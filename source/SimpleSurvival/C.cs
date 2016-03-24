using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    /// <summary>
    /// Constants
    /// </summary>
    public static class C
    {
        /// <summary>
        /// The margin for double equality
        /// </summary>
        public const double DOUBLE_MARGIN = 0.000001;
        /// <summary>
        /// The margin for equality compared to 1
        /// </summary>
        public const double DOUBLE_ALMOST_ONE = 1.0 - DOUBLE_MARGIN;
        /// <summary>
        /// If part would have less than ~1/5 seconds of LS upon startup,
        /// kill Kerbals.
        /// </summary>
        public const double STARTUP_KILL_MARGIN = 0.00001;

        // -- Resource names --
        
        /// <summary>
        /// Name of the primary life support resource
        /// </summary>
        public const string NAME_LIFESUPPORT = "LifeSupport";
        /// <summary>
        /// Name of the EVA life Support resource
        /// </summary>
        public const string NAME_EVA_LIFESUPPORT = "EVA LifeSupport";
        /// <summary>
        /// Name of consumables resource
        /// </summary>
        public const string NAME_CONSUMABLES = "Consumables";
        /// <summary>
        /// Name of stock electricity resource
        /// </summary>
        public const string NAME_ELECTRICITY = "ElectricCharge";
        /// <summary>
        /// Name of stock EVA propellant resource
        /// </summary>
        public const string NAME_EVA_PROPELLANT = "EVA Propellant";

        // -- Life Support Drain Rates --

        /// <summary>
        /// Life Support drain rate per day (game time)
        /// </summary>
        public const double LS_PER_DAY_PER_KERBAL = 1.0;
        /// <summary>
        /// Life Support drain rate per second
        /// </summary>
        public const double LS_DRAIN_PER_SEC = LS_PER_DAY_PER_KERBAL / 21600.0;

        /// <summary>
        /// EVA Life Support drain rate per day (game time)
        /// </summary>
        private const double EVA_LS_PER_MINUTE = 1.0;
        /// <summary>
        /// EVA Life Support drain rate per second
        /// </summary>
        public const double EVA_LS_DRAIN_PER_SEC = EVA_LS_PER_MINUTE / 60.0;

        /// <summary>
        /// Max amount of EVA Life Support w/ Astronaut Complex Level 2
        /// </summary>
        // String since it is first created via ConfigNode
        public const double EVA_LS_LVL_2 = 10;
        /// <summary>
        /// Max amount of EVA Life Support w/ Astronaut Complex Level 3
        /// </summary>
        public const double EVA_LS_LVL_3 = 300;

        /// <summary>
        /// Seconds of grace period before empty LS module kills Kerbals
        /// </summary>
        public const float GRACE_PERIOD = 30.0f;
        /// <summary>
        /// Give the game a buffer to finish loading everything before killing Kerbals,
        /// otherwise Rescue contracts won't register as failures.
        /// This number can be increased if large vessels trigger this erroneous behavior.
        /// </summary>
        // Tested at 0.05 seconds on a 270 part behemoth, with an i5-4690 @ 3.5 GHz.
        // This should be okay, but not sure how ultra low budget PCs would perform.
        public const float KILL_BUFFER = 1f;

        // -- Converter rates --

        /// <summary>
        /// Consumables drained per second to generate LifeSupport
        /// </summary>
        public const double CONV_CONS_PER_SEC = 0.1;
        /// <summary>
        /// ElectricCharge drained per second to generate LifeSupport
        /// </summary>
        public const double CONV_ELEC_PER_SEC = 0.1;
        /// <summary>
        /// LifeSupport generated per second from Consumables
        /// </summary>
        public const double CONV_LS_PER_SEC = -0.1;

        /// <summary>
        /// Conversation rate of Consumables to EVA
        /// </summary>
        public const double CONS_TO_EVA = 1.0;

        /// <summary>
        /// Altitude under which LifeSupport is not needed in atmosphere with oxygen
        /// </summary>
        public const double OXYGEN_CUTOFF_ALTITUDE = 10000.0;

        /// <summary>
        /// Medium priority - Orange
        /// </summary>
        public const string HTML_COLOR_WARNING = "<color=#ff8800>";
        /// <summary>
        /// High priority - Red
        /// </summary>
        public const string HTML_COLOR_ALERT = "<color=#ff1100>";
        /// <summary>
        /// "VAB green"
        /// </summary>
        public const string HTML_VAB_GREEN = "<color=#99ff00>";

        /// <summary>
        /// Header for the ConfigNode section that will contain EVA LS tracking
        /// </summary>
        public static string NODE_SIMPLESURVIVAL = "SIMPLESURVIVAL_MOD";
        /// <summary>
        /// Title of each individual node holding one Kerbal's info
        /// </summary>
        public static string NODE_EVA_TRACK = "KERBAL_EVA_LS";
        /// <summary>
        /// Guid of a contract unaddressed by LifeSupportModule OnStart
        /// </summary>
        public static string NODE_RESCUE_CONTRACT_GUID = "CONTRACT_RECOVERASSET_UNHANDLED_GUID";
    }
}
