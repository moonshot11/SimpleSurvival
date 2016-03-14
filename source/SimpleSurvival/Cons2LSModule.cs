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
        CONVERTING,
        NO_ELECTRICITY,
        NO_CONSUMABLES,
        LS_FULL,
        UNMANNED
    }

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
            guiName = "Convert " + C.NAME_CN, guiActiveUncommand = true)]
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

        public void FixedUpdate()
        {
            // Consider keeping this to check if vehicle is unmanned
            // Or, add another warning message
            // CheckConverterResources();
            if (status == ConverterStatus.CONVERTING)
            {
                double frac_elec = PullResource("ElectricCharge", C.ELECTRICITY_DRAINED_PER_SEC);
                double frac_cons = PullResource(C.NAME_CN, C.CONSUMABLES_DRAINED_PER_SEC);
                double frac_ls = PullResource(C.NAME_LS, C.LIFESUPPORT_ADDED_PER_CONS,
                    ResourceFlowMode.ALL_VESSEL);

                double min_frac = Math.Min(Math.Min(frac_elec, frac_cons), frac_ls);

                // If not all resources could be obtained,
                // proportionally return the excess resources
                if (min_frac < C.DOUBLE_ALMOST_ONE)
                {
                    // Factor (min_frac - frac_*) will be <= 0,
                    // negating the sign of the original request in PullResource
                    part.RequestResource("ElectricCharge",
                        (min_frac - frac_elec) * C.ELECTRICITY_DRAINED_PER_SEC * TimeWarp.fixedDeltaTime);
                    part.RequestResource(C.NAME_CN,
                        (min_frac - frac_cons) * C.CONSUMABLES_DRAINED_PER_SEC * TimeWarp.fixedDeltaTime);
                    part.RequestResource(C.NAME_LS,
                        (min_frac - frac_ls) * C.LIFESUPPORT_ADDED_PER_CONS * TimeWarp.fixedDeltaTime,
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
        /// Check whether converter has enough resources to run
        /// </summary>
        public void CheckConverterResources()
        {
            bool deficient = true;
            
            if (vessel.GetCrewCount() == 0)
                status = ConverterStatus.UNMANNED;
            else if (!Util.ResourceAvailable(part, C.NAME_LS, -minLS, ResourceFlowMode.ALL_VESSEL))
                status = ConverterStatus.LS_FULL;
            else if (!Util.ResourceAvailable(part, C.NAME_CN, minConsum))
                status = ConverterStatus.NO_CONSUMABLES;
            else if (!Util.ResourceAvailable(part, "ElectricCharge", minElectric))
                status = ConverterStatus.NO_ELECTRICITY;
            else
                deficient = false;

            if (!deficient && status != ConverterStatus.READY && status != ConverterStatus.CONVERTING)
            {
                status = ConverterStatus.READY;
            }
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
                case ConverterStatus.NO_CONSUMABLES:
                    return "Insufficient " + C.NAME_CN;
                case ConverterStatus.NO_ELECTRICITY:
                    return "Insufficient Electricity";
                case ConverterStatus.READY:
                    return "Ready";
                case ConverterStatus.LS_FULL:
                    return C.NAME_LS + " Full";
                case ConverterStatus.UNMANNED:
                    return "Ship Unmanned";
                default:
                    return "ERROR ConverterStatus";
            }
        }
    }
}
