using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    /// <summary>
    /// LifeSupport PartModule.
    /// </summary>
    [KSPModule("Life Support")]
    public class LifeSupportModule : PartModule, IResourceConsumer
    {
        /// <summary>
        /// If true, part has already displayed warning that
        /// LifeSupport is empty, and EVA resource is being drained
        /// from global tracking.
        /// </summary>
        private bool showed_eva_warning = false;

        public override void OnStart(StartState state)
        {
            Util.Log("LifeSupportModule OnStart()");

            if (HighLogic.LoadedSceneIsFlight)
                StartupMain();

            base.OnStart(state);
        }

        /// <summary>
        /// Handles LifeSupport, Consumables, and EVA LifeSupport drain on startup
        /// </summary>
        private void StartupMain()
        {
            bool skip_startup_request = false;
            showed_eva_warning = false;

            // Check if this ship belongs to a Rescue contract
            // and has not been set up with LifeSupport
            for (int i = 0; i < ContractChecker.Guids.Count; i++)
            {
                string contract_guid = ContractChecker.Guids[i];
                if (part.flightID.ToString() == ContractChecker.GetPartID(contract_guid))
                {
                    Util.Log("For Contract GUID: " + contract_guid);
                    Util.Log("Found PartID " + part.flightID + ", skipping startup request for " + vessel.name);

                    // Remove guid from tracking, vessel will only transition to Owned once
                    ContractChecker.Guids.Remove(contract_guid);
                    part.Resources[C.NAME_LIFESUPPORT].amount = part.Resources[C.NAME_LIFESUPPORT].maxAmount / 2.0;

                    skip_startup_request = true;
                    break;
                }
            }

            double seconds_remaining = 0;

            // -- 1. Request primary LifeSupport resource and capture seconds unaccounted for --

            if (!skip_startup_request)
            {
                // Use the seconds remaining to calculate how much EVA LifeSupport needs to be deducted
                seconds_remaining = Util.StartupRequest(this, C.NAME_LIFESUPPORT, C.LS_DRAIN_PER_SEC);
            }

            // Return early to avoid scanning all the parts
            // Just make sure all Kerbals are in tracking before exit,
            // otherwise this is taken care of in section 3
            if (seconds_remaining < C.DOUBLE_MARGIN)
            {
                foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
                    EVALifeSupportTracker.AddKerbalToTracking(kerbal.name);

                return;
            }

            // -- 2. Deduct from Consumables if vessel has Converter --
            bool has_manned_converter = false;

            // Check if vessel has a Converter
            foreach (Part part in vessel.Parts)
            {
                List<Cons2LSModule> conv_list = part.FindModulesImplementing<Cons2LSModule>();

                foreach (Cons2LSModule converter in conv_list)
                {
                    if (converter.ProperlyManned())
                    {
                        has_manned_converter = true;
                        break;
                    }
                }

                if (has_manned_converter)
                    break;
            }

            // Found Converter, convert Consumables.
            // Ignore ElectricChanrge here. Too many escapes re. capacity,
            // charge rate, LOS to sun, etc. Assume Kerbals have been
            // converting slowly while player is away.
            if (has_manned_converter)
            {
                double cons_over_lifesupport = C.CONV_CONS_PER_SEC / C.CONV_LS_PER_SEC;
                double cons_per_sec = C.LS_DRAIN_PER_SEC * cons_over_lifesupport;

                // Initial Consumables request for time passed
                double request = seconds_remaining * cons_per_sec * part.protoModuleCrew.Count;
                double frac_obtained = part.RequestResource(C.NAME_CONSUMABLES, request) / request;

                seconds_remaining *= (1 - frac_obtained);

                // Now add a bit more LifeSupport
                double cons_extra_request = C.AUTO_LS_REFILL_EXTRA * cons_over_lifesupport * part.protoModuleCrew.Count;
                double frac_extra_obtained = part.RequestResource(C.NAME_CONSUMABLES, cons_extra_request) / cons_extra_request;

                part.RequestResource(C.NAME_LIFESUPPORT,
                    -C.AUTO_LS_REFILL_EXTRA * frac_extra_obtained, C.FLOWMODE_LIFESUPPORT);
            }

            // -- 3. Finally, deduct from EVA LifeSupport --

            double eva_diff = seconds_remaining * C.EVA_LS_DRAIN_PER_SEC;

            Util.Log(seconds_remaining + " seconds remaining for " + vessel.vesselName);

            if (seconds_remaining < C.DOUBLE_MARGIN)
                eva_diff = 0;

            Util.Log("Deducting " + eva_diff + " " + C.NAME_EVA_LIFESUPPORT);

            foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
            {
                // If Kerbal isn't yet in tracking (i.e. mod was just installed),
                // add EVA LifeSupport but don't deduct anything. That seems unfair.
                if (!EVALifeSupportTracker.InTracking(kerbal.name))
                {
                    EVALifeSupportTracker.AddKerbalToTracking(kerbal.name);
                    continue;
                }

                // Current EVA LifeSupport after draining from tracking
                double current = EVALifeSupportTracker.AddEVAAmount(kerbal.name, -eva_diff, EVA_Resource.LifeSupport);

                if (current < C.KILL_BUFFER)
                    EVALifeSupportTracker.SetCurrentAmount(kerbal.name, C.KILL_BUFFER, EVA_Resource.LifeSupport);
                else if (current < C.EVA_LS_30_SECONDS)
                    Util.PostUpperMessage(kerbal.name + " has " + (int)(current / C.EVA_LS_DRAIN_PER_SEC) + " seconds to live!", 1);

                // If Kerbal is about to die, messages will already be printed.
                // Don't clutter the screen with the "now on EVA LS" message too.
                if (current < C.EVA_LS_30_SECONDS)
                    showed_eva_warning = true;
            }
        }

        /// <summary>
        /// VAB info
        /// </summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            // NOTE: This method is called ONCE during initial part loading,
            // so performance is not a concern

            string info = "Active only when manned.\n\n" +
            "<b>" + C.HTML_VAB_GREEN + "Requires:</color></b>\n" +
            "- " + C.NAME_LIFESUPPORT + ": " + Util.FormatForGetInfo(C.LS_PER_DAY_PER_KERBAL) + "/Kerbal/day.\n";
            
            return info;
        }

        public void FixedUpdate()
        {
            // If part is unmanned, nothing to do
            if (part.protoModuleCrew.Count == 0)
            {
                showed_eva_warning = false;
                return;
            }

            // If vessel is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (Util.BreathableAir(vessel))
                return;

            int crew_count = part.protoModuleCrew.Count;

            // How much lifesupport to request
            double ls_request = crew_count * C.LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime;

            // Request resource based on rates defined by constants
            double ret_rs = part.RequestResource(C.NAME_LIFESUPPORT, ls_request, C.FLOWMODE_LIFESUPPORT);

            // If LifeSupport exists or is restored, reset EVA warning and return
            if (ret_rs > 0.0)
            {
                showed_eva_warning = false;
                return;
            }

            // Otherwise, begin deducting EVA LifeSupport
            if (!showed_eva_warning)
            {
                TimeWarp.SetRate(0, true);
                string vessel_name = vessel.isActiveVessel ? part.partInfo.title : vessel.vesselName;

                showed_eva_warning = true;
                Util.PostUpperMessage("Crew in " + vessel_name + " has run out of "
                    + C.NAME_LIFESUPPORT + ",\n is consuming " + C.NAME_EVA_LIFESUPPORT, 1);
            }

            // Modify crew list in place
            int i = 0;
            while (i < part.protoModuleCrew.Count)
            {
                ProtoCrewMember kerbal = part.protoModuleCrew[i];

                double request = C.EVA_LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime;

                double current_eva = EVALifeSupportTracker.AddEVAAmount(kerbal.name, -request, EVA_Resource.LifeSupport);

                if (current_eva + request > C.EVA_LS_30_SECONDS &&
                    current_eva <= C.EVA_LS_30_SECONDS)
                {
                    TimeWarp.SetRate(0, true);
                    Util.PostUpperMessage(kerbal.name + " has 30 seconds to live!", 1);
                    // Set to 30 seconds in case of large timewarp.
                    EVALifeSupportTracker.SetCurrentAmount(kerbal.name, C.EVA_LS_30_SECONDS, EVA_Resource.LifeSupport);
                }
                
                if (EVALifeSupportTracker.GetEVALSInfo(kerbal.name).ls_current < C.DOUBLE_MARGIN)
                {
                    Util.KillKerbal(this, kerbal);
                    continue;
                }

                i++;
            }
        }

        /// <summary>
        /// Return resource definition for use in Engineer's Report
        /// </summary>
        /// <returns></returns>
        public List<PartResourceDefinition> GetConsumedResources()
        {
            PartResourceDefinition def = PartResourceLibrary.Instance.resourceDefinitions[C.NAME_LIFESUPPORT];

            List<PartResourceDefinition> list = new List<PartResourceDefinition>();
            list.Add(def);

            return list;
        }
    }
}
