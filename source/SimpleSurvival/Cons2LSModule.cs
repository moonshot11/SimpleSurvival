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

        [KSPField(guiActive = true, guiName = "Status")]
        string str_status = "";

        ConverterStatus status = ConverterStatus.CONVERTING;

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
            double obt_elec = part.RequestResource("ElectricCharge", minElectric);
            bool deficient = false;

            if (obt_elec < minElectric)
            {
                status = ConverterStatus.NO_ELECTRICITY;
                deficient = true;
            }

            double obt_consum = part.RequestResource("Consumables", minConsum);

            if (obt_consum < minConsum)
            {
                status = ConverterStatus.NO_CONSUMABLES;
                deficient = true;
            }

            // This value is negative!
            double obt_ls = part.RequestResource("LifeSupport", -minLS, ResourceFlowMode.ALL_VESSEL);

            if (-obt_ls < minLS)
            {
                status = ConverterStatus.LS_FULL;
                deficient = true;
            }

            // Restore resources - this is only a check
            // Better way to check resources than requesting negative amounts?
            part.RequestResource("Consumables", -obt_consum);
            part.RequestResource("ElectricCharge", -obt_elec);
            part.RequestResource("LifeSupport", -obt_ls, ResourceFlowMode.ALL_VESSEL);

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
