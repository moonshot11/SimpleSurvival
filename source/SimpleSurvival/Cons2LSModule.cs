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
        NO_CONSUMABLES
    }

    public class Cons2LSModule : PartModule
    {
        [KSPField(guiActive = true, guiName = "Status")]
        string str_status = "";

        ConverterStatus status = ConverterStatus.CONVERTING;

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Convert Consumables")]
        public void ToggleStatus()
        {
            status = status == ConverterStatus.NO_CONSUMABLES ? ConverterStatus.NO_ELECTRICITY : ConverterStatus.NO_CONSUMABLES;
        }

        public void FixedUpdate()
        {
            //
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
                default:
                    return "ERROR ConverterStatus";
            }
        }
    }
}
