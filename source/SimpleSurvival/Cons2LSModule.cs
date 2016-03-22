﻿using System;
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

    [KSPModule("Converter")]
    public class Cons2LSModule : PartModule
    {
        const double test_value = C.DOUBLE_MARGIN;

        // -- Minimum values for Consumable->LifeSupport conversion
        const double minElectric = test_value;
        const double minConsum = test_value;
        const double minLS = test_value;

        [KSPField(guiActive = true, guiName = "Converter")]
        string str_status = "";

        ConverterStatus status = ConverterStatus.READY;

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

        [KSPEvent(guiActive = true, guiActiveEditor = false,
            guiName = "Refill " + C.NAME_EVA_LIFESUPPORT, guiActiveUncommand = true,
            guiActiveUnfocused = true, unfocusedRange = 3f, externalToEVAOnly = true)]
        public void FillEVA()
        {
            Vessel active = FlightGlobals.ActiveVessel;

            // Player is controlling ship
            if (vessel == active)
            {
                Util.Log("FillEVA pressed for active vessel " + vessel.name);

                double eva_request_total = 0;

                // Map of kerbals in tracking, and how much they're requesting
                Dictionary<string, double> kerbal_requests = new Dictionary<string, double>();

                foreach (ProtoCrewMember kerbal in active.GetVesselCrew())
                {
                    if (!EVALifeSupportTracker.InTracking(kerbal.name))
                        continue;

                    var info = EVALifeSupportTracker.GetEVALSInfo(kerbal.name);
                    double request = info.max - info.current;

                    eva_request_total += request;
                    kerbal_requests.Add(kerbal.name, request);

                    Util.Log("    Kerbal " + name + " has EVA need for " + request);
                }

                // Deduct Consumables
                double obtained = part.RequestResource(C.NAME_CONSUMABLES, C.CONS_TO_EVA * eva_request_total);
                double frac = obtained / eva_request_total;

                Util.Log("    EVA request total  = " + eva_request_total);
                Util.Log("    Request * factor   = " + C.CONS_TO_EVA * eva_request_total);
                Util.Log("    Obtained           = " + obtained);
                Util.Log("    Fraction available = " + frac);

                // Distribute EVA LS proportionally
                foreach (string name in kerbal_requests.Keys)
                {
                    double add = kerbal_requests[name] * frac;
                    EVALifeSupportTracker.AddEVAAmount(name, add);

                    Util.Log("    Adding " + add + " to " + name);
                }
            }
            // Player is controlling EVA
            else
            {
                Util.Log("FillEVA pressed for EVA: " + active.GetVesselCrew()[0].name);

                string name = active.GetVesselCrew()[0].name;

                // This works right now because the tracker updates live.
                // May break in the future.
                var info = EVALifeSupportTracker.GetEVALSInfo(name);
                double eva_request = info.max - info.current;

                double obtained = part.RequestResource(C.NAME_CONSUMABLES, C.CONS_TO_EVA * eva_request);
                active.rootPart.RequestResource(C.NAME_EVA_LIFESUPPORT, -obtained / C.CONS_TO_EVA);

                Util.Log("    EVA Request  = " + eva_request);
                Util.Log("    Amt Obtained = " + obtained);

                // Fill EVA Propellant while we're at it
                active.rootPart.RequestResource(C.NAME_EVA_PROPELLANT, -double.MaxValue);
            }
        }

        public void FixedUpdate()
        {
            if (status == ConverterStatus.CONVERTING)
            {
                if (!ProperlyManned())
                {
                    ScreenMessages.PostScreenMessage("<color=#ff8800>Converter requires an Engineer to operate</color>",
                        5f, ScreenMessageStyle.UPPER_CENTER);
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

            double frac = Math.Abs(obtained / req);

            if (frac < C.DOUBLE_ALMOST_ONE)
            {
                string message;

                if (req >= 0)
                    message = "Not enough " + resource + " to use Converter!";
                else
                    message = "Cannot proceed, " + resource + " is full!";

                ScreenMessages.PostScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_CENTER);
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
            foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
            {
                // kerbal.experienceTrait.Title also returns "Engineer"
                // kerbal.experienceLevel [0..5] to add experience check
                if (kerbal.experienceTrait.TypeName == "Engineer")
                    return true;
            }

            return false;
        }

        public override string GetInfo()
        {
            string info = "Converts " + C.NAME_CONSUMABLES + " to " + C.NAME_LIFESUPPORT + ". Part must be manned by an Engineer to operate.\n\n" +
            "<b><color=#99ff00>Requires:</color></b>\n" +
            "- " + C.NAME_CONSUMABLES + ": " + Util.FormatForGetInfo(C.CONV_CONS_PER_SEC) + "/sec.\n" +
            "- " + C.NAME_ELECTRICITY + ": " + Util.FormatForGetInfo(C.CONV_ELEC_PER_SEC) + "/sec.\n\n" +
            "<b><color=#99ff00>Outputs:</color></b>\n" +
            "- " + C.NAME_LIFESUPPORT + ": " + Util.FormatForGetInfo(-C.CONV_LS_PER_SEC) + "/sec.";

            return info;
        }
    }
}
