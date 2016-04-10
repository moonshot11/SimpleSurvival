using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public enum ConverterStatus
    {
        READY,
        CONVERTING
    }

    /// <summary>
    /// The Consumables -> LifeSupport converter PartModule.
    /// </summary>
    [KSPModule("Converter")]
    public class Cons2LSModule : PartModule, IResourceConsumer
    {
        const double test_value = C.DOUBLE_MARGIN;

        // -- Minimum values for Consumable->LifeSupport conversion
        const double minElectric = test_value;
        const double minConsum = test_value;
        const double minLS = test_value;

        [KSPField(guiActive = true, guiName = "Converter")]
        string str_status = "";

        ConverterStatus status = ConverterStatus.READY;

        /// <summary>
        /// Button in KSP interface to toggle converter
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false,
            guiName = "Convert " + C.NAME_CONSUMABLES, guiActiveUncommand = true)]
        public void ToggleStatus()
        {
            Util.Log("Toggling Converter status from " + status);
            switch (status)
            {
                case ConverterStatus.CONVERTING:
                    status = ConverterStatus.READY;
                    break;

                case ConverterStatus.READY:
                    status = ConverterStatus.CONVERTING;
                    break;
            }

            Util.Log(" to " + status);
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

        public void FixedUpdate()
        {
            if (status != ConverterStatus.CONVERTING)
                return;

            if (!ProperlyManned())
            {
                Util.PostUpperMessage("Converter requires an Engineer to operate!");
                status = ConverterStatus.READY;
                return;
            }

            double frac_elec = PullResource(C.NAME_ELECTRICITY, C.CONV_ELEC_PER_SEC);
            double frac_cons = PullResource(C.NAME_CONSUMABLES, C.CONV_CONS_PER_SEC);
            double frac_ls = PullResource(C.NAME_LIFESUPPORT, C.CONV_LS_PER_SEC,
                ResourceFlowMode.ALL_VESSEL);

            double min_frac = Math.Min(Math.Min(frac_elec, frac_cons), frac_ls);

            // If not all resources could be obtained,
            // proportionally return the excess resources
            if (min_frac < C.DOUBLE_ALMOST_ONE)
            {
                // Factor (min_frac - frac_*) will be <= 0,
                // negating the sign of the original request in PullResource
                part.RequestResource(C.NAME_ELECTRICITY,
                    (min_frac - frac_elec) * C.CONV_ELEC_PER_SEC * TimeWarp.fixedDeltaTime);
                part.RequestResource(C.NAME_CONSUMABLES,
                    (min_frac - frac_cons) * C.CONV_CONS_PER_SEC * TimeWarp.fixedDeltaTime);
                part.RequestResource(C.NAME_LIFESUPPORT,
                    (min_frac - frac_ls) * C.CONV_LS_PER_SEC * TimeWarp.fixedDeltaTime,
                    ResourceFlowMode.ALL_VESSEL);

                status = ConverterStatus.READY;
            }

        }

        /// <summary>
        /// Request resource for Converter, print message
        /// </summary>
        /// <param name="resource">Name of the resource to request</param>
        /// <param name="amount">Amount of the resource to request</param>
        /// <param name="flowmode">Flowmode. Defaults to resource default</param>
        /// <returns>Returns the fraction of resource obtained to resource requested</returns>
        public double PullResource(string resource, double amount, ResourceFlowMode flowmode = ResourceFlowMode.NULL)
        {
            double req = amount * TimeWarp.fixedDeltaTime;
            double obtained;

            if (flowmode == ResourceFlowMode.NULL)
                obtained = part.RequestResource(resource, amount);
            else
                obtained = part.RequestResource(resource, amount, flowmode);

            // Percentage of resource request that was obtained
            double frac = Math.Abs(obtained / req);

            if (frac < C.DOUBLE_ALMOST_ONE)
            {
                string message;

                if (req >= 0)
                    message = "Not enough " + resource + " to use Converter!";
                else
                    message = "Cannot proceed, " + resource + " is full!";

                Util.PostUpperMessage(message);
            }

            return frac;
        }

        /// <summary>
        /// Generic part update. Handle part status.
        /// </summary>
        public override void OnUpdate()
        {
            str_status = StatusToString(status);
            base.OnUpdate();
        }

        /// <summary>
        /// Convert converter status to its string representation
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public string StatusToString(ConverterStatus status)
        {
            switch(status)
            {
                case ConverterStatus.CONVERTING:
                    return "Converting";
                case ConverterStatus.READY:
                    return "Ready";
                default:
                    return "ERROR ConverterStatus";
            }
        }

        /// <summary>
        /// Check if the converter has the proper crew to operate
        /// </summary>
        /// <returns></returns>
        private bool ProperlyManned()
        {
            // If game isn't in Career Mode, Kerbal specializations
            // aren't supposed to matter. Just check if converter is manned.
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER &&
                part.protoModuleCrew.Count > 0)
                return true;

            foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
            {
                // kerbal.experienceTrait.Title also returns "Engineer"
                // kerbal.experienceLevel [0..5] to add experience check
                if (kerbal.experienceTrait.TypeName == "Engineer")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Return VAB info text
        /// </summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            string info = "Converts " + C.NAME_CONSUMABLES + " to " + C.NAME_LIFESUPPORT +
                ". Part must be manned by an Engineer.\n\n" +

            "<b>" + C.HTML_VAB_GREEN + "Requires:</color></b>\n" +
            "- " + C.NAME_CONSUMABLES + ": " + Util.FormatForGetInfo(C.CONV_CONS_PER_SEC) + "/sec.\n" +
            "- " + C.NAME_ELECTRICITY + ": " + Util.FormatForGetInfo(C.CONV_ELEC_PER_SEC) + "/sec.\n" +
            "<b>" + C.HTML_VAB_GREEN + "Outputs:</color></b>\n" +
            "- " + C.NAME_LIFESUPPORT + ": " + Util.FormatForGetInfo(-C.CONV_LS_PER_SEC) + "/sec.\n\n" +

            "EVA refill has no crew requirement and is instantaneous. " + C.NAME_EVA_PROPELLANT + " is refilled for free.\n\n" +
            "<b>" + C.HTML_VAB_GREEN + "Conversion rate:</color></b>\n" +
            "  " + Util.FormatForGetInfo(C.CONS_TO_EVA_LS) + " " + C.NAME_CONSUMABLES +
            "\n  = 1.0 " + C.NAME_EVA_LIFESUPPORT;

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
    }
}
