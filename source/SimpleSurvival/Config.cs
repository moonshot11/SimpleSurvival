using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    /// <summary>
    /// Selects the behavior by which to update EVA stats
    /// </summary>
    public enum EVAUpdateMode
    {
        /// <summary>
        /// Never update in-flight (Kerbal must be recovered)
        /// </summary>
        Recovered,
        /// <summary>
        /// Update as long as vessel has Hitchhiker module
        /// </summary>
        IfHitchhiker,
        /// <summary>
        /// Update as long as Kerbal is aboard any vessel
        /// </summary>
        Aboard
    }

    public enum ConverterReq
    {
        /// <summary>
        /// Converter can be used, even if unmanned.
        /// </summary>
        None,
        /// <summary>
        /// Converter must be manned. Any Kerbal will do.
        /// </summary>
        AnyKerbal,
        /// <summary>
        /// Converter must be manned by an Engineer.
        /// </summary>
        Engineer,
        /// <summary>
        /// Converter must be manned by an Engineer if exp enabled in-game,
        /// otherwise any Kerbal will do.
        /// </summary>
        IfKerbalExpEnabled
    }

    /// <summary>
    /// Variable config values
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Show EVA stats in GUI
        /// </summary>
        public static bool DEBUG_SHOW_EVA = false;
        /// <summary>
        /// Update EVA max values without returning to KSC?
        /// </summary>
        public static EVAUpdateMode EVA_MAX_UPDATE = EVAUpdateMode.IfHitchhiker;
        /// <summary>
        /// Requirement for enabling Converter.
        /// </summary>
        public static ConverterReq CONV_REQ = ConverterReq.IfKerbalExpEnabled;


        // -- EVA LifeSupport career values --

        /// <summary>
        /// Max amount of EVA Life Support w/ Astronaut Complex Level 1
        /// </summary>
        public static double EVA_LS_LVL_1 = 1;
        /// <summary>
        /// Max amount of EVA Life Support w/ Astronaut Complex Level 2
        /// </summary>
        public static double EVA_LS_LVL_2 = 10;
        /// <summary>
        /// Max amount of EVA Life Support w/ Astronaut Complex Level 3
        /// </summary>
        public static double EVA_LS_LVL_3 = 300;
    }
}
