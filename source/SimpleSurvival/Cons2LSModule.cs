using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    public enum ConverterReq
    {
        /// <summary>
        /// Converter can be used, even if unmanned.
        /// </summary>
        AlwaysReady,
        /// <summary>
        /// Converter must be manned. Any Kerbal will do.
        /// </summary>
        RequiresAnyKerbal,
        /// <summary>
        /// Converter must be manned by an Engineer.
        /// </summary>
        RequiresEngineer,
        /// <summary>
        /// Converter must be manned by an Engineer if exp enabled in-game,
        /// otherwise any Kerbal will do.
        /// </summary>
        IfKerbalExpEnabled
    }

    /// <summary>
    /// The Consumables -> LifeSupport converter PartModule.
    /// </summary>
    [KSPModule("Converter")]
    public class Cons2LSModule : ModuleResourceConverter, IResourceConsumer
    {
        private string msgMissing = "";

        public override void OnStart(StartState state)
        {
            string specialist = (Util.AdvParams.EnableKerbalExperience ? C.CONV_SPECIALIST : "Kerbal");
            msgMissing = "Missing " + specialist;
            base.OnStart(state);
        }

        /// <summary>
        /// Check if the converter has the proper crew to operate
        /// </summary>
        /// <returns></returns>
        internal bool ProperlyManned()
        {
            if (Config.CONV_REQ == ConverterReq.AlwaysReady)
                return true;

            if (Config.CONV_REQ == ConverterReq.RequiresAnyKerbal ||
                Config.CONV_REQ == ConverterReq.IfKerbalExpEnabled &&
                    !Util.AdvParams.EnableKerbalExperience)
                return part.protoModuleCrew.Count > 0;

            if (Config.CONV_REQ == ConverterReq.RequiresEngineer ||
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
                "  " + Util.FormatForGetInfo(C.CONS_PER_EVA_LS) + " " + C.NAME_CONSUMABLES +
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
