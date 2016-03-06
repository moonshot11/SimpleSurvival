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
        CONVERTING,
        NO_ELECTRICITY,
        NO_CONSUMABLES,
        LS_FULL
    }

    public class Cons2LSModule : PartModule
    {
        const double test_value = 0.1f;

        // -- Minimum values for Consumable->LifeSupport conversion
        const double minElectric = test_value;
        const double minConsum = test_value;
        const double minLS = test_value;

        [KSPField(guiActive = true, guiName = "Converter Status")]
        string str_status = "";

        ConverterStatus status = ConverterStatus.READY;

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Convert Consumables")]
        public void ToggleStatus()
        {
            switch (status)
            {
                case ConverterStatus.CONVERTING:
                    status = ConverterStatus.READY;
                    break;

                case ConverterStatus.READY:
                    status = ConverterStatus.CONVERTING;
                    break;
            }
        }

        public void FixedUpdate()
        {
            // First, check resources
            CheckConverterResources();

            if (status == ConverterStatus.CONVERTING)
            {
                part.RequestResource("ElectricCharge", minElectric * TimeWarp.fixedDeltaTime);
                part.RequestResource("Consumables", minConsum * TimeWarp.fixedDeltaTime);
                part.RequestResource("LifeSupport", -minLS * TimeWarp.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Check whether converter has enough resources to run
        /// </summary>
        public void CheckConverterResources()
        {
            bool deficient = true;

            if (!Util.ResourceAvailable(part, "LifeSupport", -minLS, ResourceFlowMode.ALL_VESSEL))
                status = ConverterStatus.LS_FULL;
            else if (!Util.ResourceAvailable(part, "Consumables", minConsum))
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

        public string StatusToString(ConverterStatus st)
        {
            switch(st)
            {
                case ConverterStatus.CONVERTING:
                    return "Converting";
                case ConverterStatus.NO_CONSUMABLES:
                    return "Insufficient Consumables";
                case ConverterStatus.NO_ELECTRICITY:
                    return "Insufficient Electricity";
                case ConverterStatus.READY:
                    return "Ready";
                case ConverterStatus.LS_FULL:
                    return "LifeSupport full!";
                default:
                    return "ERROR ConverterStatus";
            }
        }
    }
}
