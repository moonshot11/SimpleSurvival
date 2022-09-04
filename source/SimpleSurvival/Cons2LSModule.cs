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
        private string msgMissing = "";
        private const string STATUS_RUNNING = "Running";
        private const string STATUS_STANDBY = "Ready";

        public override void OnStart(StartState state)
        {
            // Give player context-sensitive feedback on what is required for
            // converter to operate
            string reqdKerbal;

            if (Config.CONV_REQ == ConverterReq.IfKerbalExpEnabled && Util.AdvParams.EnableKerbalExperience ||
                Config.CONV_REQ == ConverterReq.Engineer)
                reqdKerbal = "Engineer";
            else
                reqdKerbal = "Kerbal";
            msgMissing = "Missing " + reqdKerbal;
            base.OnStart(state);
        }

        /// <summary>
        /// Check if the converter has the proper crew to operate
        /// </summary>
        /// <returns></returns>
        internal bool IsOperational()
        {
            if (Config.CONV_REQ == ConverterReq.None)
                return true;

            if (Config.CONV_REQ == ConverterReq.AnyKerbal ||
                Config.CONV_REQ == ConverterReq.IfKerbalExpEnabled &&
                    !Util.AdvParams.EnableKerbalExperience)
                return part.protoModuleCrew.Count > 0;

            if (Config.CONV_REQ == ConverterReq.Engineer ||
                Config.CONV_REQ == ConverterReq.IfKerbalExpEnabled &&
                    Util.AdvParams.EnableKerbalExperience)
            {
                for (int i = 0; i < part.protoModuleCrew.Count; i++)
                {
                    // kerbal.experienceTrait.Title also returns "Engineer"
                    // kerbal.experienceLevel [0..5] to add experience check
                    if (part.protoModuleCrew[i].experienceTrait.TypeName == C.CONV_SPECIALIST)
                        return true;
                }
            }

            return false;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!IsOperational())
            {
                StopResourceConverter();
                status = msgMissing;
            }
            else if (IsActivated)
            {
                // statusPercent is load, out of 100 (i.e. % times 100)
                if (statusPercent < 1.0)
                {
                    StopResourceConverter();
                    status = STATUS_STANDBY;
                }
                else
                    status = STATUS_RUNNING;
            }
            else if (status == msgMissing) // && ProperlyManned()
            {
                // By this point, converter must be manned,
                // so no need to check again
                status = STATUS_STANDBY;
            }

            UpdateConverterStatus();
        }

        /// <summary>
        /// Return VAB info text
        /// </summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            string info = base.GetInfo();

            info += "\n\nConverter may require Kerbal, see mod settings.\n\n" +
                $"Can refill {C.NAME_EVA_LIFESUPPORT} from toolbar.\n\n" +
                C.HTML_VAB_GREEN + "Consumables conversion:</color>\n" +
                $"One unit of {C.NAME_CONSUMABLES} is equal to one day of {C.NAME_LIFESUPPORT}, " +
                $"or one day of {C.NAME_EVA_LIFESUPPORT}.";

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
