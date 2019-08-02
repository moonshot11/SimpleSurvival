using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    /// <summary>
    /// The Consumables -> LifeSupport converter PartModule.
    /// </summary>
    [KSPModule("Converter")]
    public class Cons2LSModule : ModuleResourceConverter, IResourceConsumer
    {
        private const string CONV_SPECIALIST = "Engineer";
        private string msgMissing = "";

        public override void OnStart(StartState state)
        {
            string specialist = (Util.AdvParams.EnableKerbalExperience ? CONV_SPECIALIST : "Kerbal");
            msgMissing = "Missing " + specialist;
            base.OnStart(state);
        }

        private int FillEVAResource(EVA_Resource choice)
        {
            Vessel active = FlightGlobals.ActiveVessel;

            double conversion_rate;
            string resource_name;

            switch (choice)
            {
                case EVA_Resource.LifeSupport:
                    conversion_rate = C.CONS_TO_EVA_LS;
                    resource_name = C.NAME_EVA_LIFESUPPORT;
                    break;
                case EVA_Resource.Propellant:
                    conversion_rate = C.CONS_TO_EVA_PROP;
                    resource_name = C.NAME_EVA_PROPELLANT;
                    break;
                default:
                    throw new ArgumentException("Cons2LSModule.FillEVAResource, request enum not properly set");
            }

            Util.Log("Processing FillEVA resource request for " + resource_name);

            // Player is controlling ship
            if (vessel == active)
            {
                Util.Log("FillEVA pressed for active vessel " + vessel.name);

                double eva_request_total = 0;

                // Map of kerbals in tracking, and how much they're requesting
                Dictionary<string, double> kerbal_requests = new Dictionary<string, double>();

                foreach (ProtoCrewMember kerbal in active.GetVesselCrew())
                {
                    // Previously had a check here if Kerbal was in EVA tracking.
                    // This should now be covered by LifeSupportModule adding
                    // all missing Kerbals to tracking in OnStart.

                    var info = EVALifeSupportTracker.GetEVALSInfo(kerbal.name);
                    double request = 0;

                    switch(choice)
                    {
                        case EVA_Resource.Propellant:
                            request = info.prop_max - info.prop_current;
                            break;
                        case EVA_Resource.LifeSupport:
                            request = info.ls_max - info.ls_current;
                            break;
                    }                       

                    eva_request_total += request;
                    kerbal_requests.Add(kerbal.name, request);

                    Util.Log("    Kerbal " + kerbal.name + " has EVA need for " + request);
                }

                // If no EVA request, exit early
                if (eva_request_total < C.DOUBLE_MARGIN)
                {
                    Util.Log("All crewmembers full! Skipping EVA refill");
                    Util.PostUpperMessage("EVA resources already full!");
                    return -1;
                }

                // Deduct Consumables
                double obtained = part.RequestResource(C.NAME_CONSUMABLES, conversion_rate * eva_request_total);
                double frac = obtained / eva_request_total;

                Util.Log("    EVA request total  = " + eva_request_total);
                Util.Log("    Request * factor   = " + conversion_rate * eva_request_total);
                Util.Log("    Obtained           = " + obtained);
                Util.Log("    Fraction available = " + frac);

                // Distribute EVA LS proportionally
                foreach (string name in kerbal_requests.Keys)
                {
                    double add = kerbal_requests[name] * frac;
                    EVALifeSupportTracker.AddEVAAmount(name, add, choice);

                    Util.Log("    Adding " + add + " to " + name);
                }

                if (frac > C.DOUBLE_ALMOST_ONE)
                    return 0;
                else if (frac < C.DOUBLE_MARGIN)
                    return 2;
                else
                    return 1;
            }
            // Player is controlling EVA
            else
            {
                Util.Log("FillEVA pressed for EVA: " + active.GetVesselCrew()[0].name);

                string name = active.GetVesselCrew()[0].name;

                // This works right now because the tracker updates live.
                // May break in the future.
                var info = EVALifeSupportTracker.GetEVALSInfo(name);
                double eva_request = 0;

                switch (choice)
                {
                    case EVA_Resource.Propellant:
                        eva_request = info.prop_max - info.prop_current;
                        break;
                    case EVA_Resource.LifeSupport:
                        eva_request = info.ls_max - info.ls_current;
                        break;
                }

                double obtained = part.RequestResource(C.NAME_CONSUMABLES, conversion_rate * eva_request);
                double add = obtained / conversion_rate;
                active.rootPart.RequestResource(resource_name, -add);

                Util.Log("    EVA Request  = " + eva_request);
                Util.Log("    Amt Obtained = " + obtained);

                // If enough resources were added
                if (add > eva_request - C.DOUBLE_MARGIN)
                    return 0;
                // If Consumables are empty
                else if (add < C.DOUBLE_MARGIN)
                    return 2;
                // If Consumables are almost empty, partial refill
                else
                    return 1;
            }
        }

        /// <summary>
        /// Button in right-click interface to fill EVA
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false,
            guiName = "Refill EVA", guiActiveUncommand = true,
            guiActiveUnfocused = true, unfocusedRange = 3f, externalToEVAOnly = true)]
        public void FillEVA()
        {
            int warning_level = FillEVAResource(EVA_Resource.LifeSupport);
            warning_level = Math.Max(warning_level, FillEVAResource(EVA_Resource.Propellant));

            switch(warning_level)
            {
                case 0:
                    Util.PostUpperMessage("EVA resources refilled!");
                    break;

                case 1:
                    Util.PostUpperMessage("Partial refill - " + C.NAME_CONSUMABLES + " are empty!", 2);
                    break;

                case 2:
                    Util.PostUpperMessage(C.NAME_CONSUMABLES + " are empty - could not refill!", 2);
                    break;
            }
        }

        /// <summary>
        /// Check if the converter has the proper crew to operate
        /// </summary>
        /// <returns></returns>
        internal bool ProperlyManned()
        {
            // If game isn't in Career Mode, Kerbal specializations
            // aren't supposed to matter. Just check if converter is manned.
            if (!Util.AdvParams.EnableKerbalExperience && part.protoModuleCrew.Count > 0)
                return true;

            foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
            {
                // kerbal.experienceTrait.Title also returns "Engineer"
                // kerbal.experienceLevel [0..5] to add experience check
                if (kerbal.experienceTrait.TypeName == CONV_SPECIALIST)
                    return true;
            }

            return false;
        }

        public override void FixedUpdate()
        {
            if (!ProperlyManned())
            {
                StopResourceConverter();
                status = msgMissing;
            }
            else if (status == msgMissing)
            {
                status = "Inactive"; // Stock default status
            }
            base.FixedUpdate();
        }

        /// <summary>
        /// Return VAB info text
        /// </summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            string info = base.GetInfo();

            info += "\n\nPart must be manned by an Engineer.\n" +
                "(Can be manned by any Kerbal if \"Enable Kerbal Experience\" is unchecked.)\n\n" +
                C.HTML_VAB_GREEN + "EVA conversion rate:</color>\n" +
                "  " + Util.FormatForGetInfo(C.CONS_TO_EVA_LS) + " " + C.NAME_CONSUMABLES +
                "\n  = 1.0 " + C.NAME_EVA_LIFESUPPORT +
                "\n\nEVA refill has no crew requirement and is instantaneous. " +
                C.NAME_EVA_PROPELLANT + " is refilled for free.\n\n";

            return info;
        }

        /// <summary>
        /// Return resource definition for use in Engineer's Report
        /// </summary>
        /// <returns></returns>
        public List<PartResourceDefinition> GetConsumedResources()
        {
            PartResourceDefinition def = PartResourceLibrary.Instance.resourceDefinitions[C.NAME_CONSUMABLES];

            List<PartResourceDefinition> list = new List<PartResourceDefinition>();
            list.Add(def);

            return list;
        }

        // These two functions are needed to redefine guiActiveUncommand,
        // since these particular buttons should be available when the only
        // Kerbal in the vessel is in the crewCabin. The vessel is unmanned,
        // rendering stock ModuleResourceConverters inoperable.

        /// <summary>
        /// KSP in-flight GUI button: start converter
        /// </summary>
        [KSPEvent(guiName = "#autoLOC_6001471", guiActive = true, active = false,
            guiActiveUncommand = true)]
        public override void StartResourceConverter()
        {
            base.StartResourceConverter();
        }

        /// <summary>
        /// KSP in-flight GUI button: stop converter
        /// </summary>
        [KSPEvent(guiName = "#autoLOC_6001472", guiActive = true, active = false,
            guiActiveUncommand = true)]
        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
        }
    }
}
