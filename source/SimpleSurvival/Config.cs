using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    /// <summary>
    /// Variable config values
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Show EVA stats in GUI
        /// </summary>
        public static bool DEBUG_SHOW_EVA = false;

        // -- EVA Propellant career values --

        /// <summary>
        /// Max amount of EVA Propellant w/ Astronaut Complex Level 2
        /// </summary>
        public static double EVA_PROP_LVL_2 = 5;
        /// <summary>
        /// Max amount of EVA Propellant w/ Astronaut Complex Level 3
        /// </summary>
        public static double EVA_PROP_LVL_3 = 5;


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
