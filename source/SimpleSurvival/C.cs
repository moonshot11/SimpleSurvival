using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// LS required to support one Kerbal for 30 seconds.
        /// </summary>
        public const double LS_30_SECONDS = LS_PER_DAY_PER_KERBAL / 6.0 / 60.0 / 2.0;
        /// <summary>
        /// Life Support drain rate per second. Multiply for seconds -> days.
        /// </summary>
        public const double LS_DRAIN_PER_SEC = LS_PER_DAY_PER_KERBAL / 21600.0;

        // -- EVA Life Support rates --

        /// <summary>
        /// EVA Life Support drain rate per day (game time)
        /// </summary>
        private const double EVA_LS_PER_MINUTE = 1.0;
        /// <summary>
        /// EVA Life Support drain rate per second. Multiply for seconds -> minutes.
        /// </summary>
        public const double EVA_LS_DRAIN_PER_SEC = EVA_LS_PER_MINUTE / 60.0;
        /// <summary>
        /// EVA Life Support drain rate per day.
        /// </summary>
        public const double EVA_LS_DRAIN_PER_DAY = EVA_LS_PER_MINUTE * 60.0 * 6.0;
        /// <summary>
        /// 30 seconds' worth of EVA LifeSupport, for warning messages
        /// </summary>
        public const double EVA_LS_30_SECONDS = EVA_LS_DRAIN_PER_SEC * 30;

        // -- EVA Propellant values --

        /// <summary>
        /// If EVA Propellant is less than this, set to this to avoid cases
        /// where player EVAs and consequently "bricks" a Kerbal
        /// </summary>
        public const double EVA_PROP_SAFE_MIN = 0.1;

        // -- Almost-out tweaking values --

        /// <summary>
        /// Seconds of grace period before empty LS module kills Kerbals
        /// </summary>
        public const float GRACE_PERIOD = 30.0f;
        /// <summary>
        /// Give the game a buffer to finish loading everything before killing Kerbals,
        /// otherwise Rescue contracts won't register as failures.
        /// This number can be increased if large vessels trigger this erroneous behavior.
        /// Units == minutes
        /// </summary>
        // Tested at 0.05 seconds on a 270 part behemoth, with an i5-4690 @ 3.5 GHz.
        // This should be okay, but not sure how ultra low budget PCs would perform.
        //
        // Units of EVA LS are minutes, so this value should be calculated as
        // (x seconds) * (EVA_LS_DRAIN_PER_SEC) ==> (x in minutes)
        public const double KILL_BUFFER = EVA_LS_DRAIN_PER_SEC;
        /// <summary>
        /// How much to "top off" a LifeSupport module during vessel load
        /// after Consumables have been drained to keep Kerbals alive
        /// </summary>
        public const double AUTO_LS_REFILL_EXTRA = 0.25;

        // -- Converter rates --

        /// <summary>
        /// Consumables drained per second to generate LifeSupport
        /// </summary>
        public const double CONV_CONS_PER_SEC = 4;
        /// <summary>
        /// ElectricCharge drained per second to generate LifeSupport
        /// </summary>
        public const double CONV_ELEC_PER_SEC = 2;
        /// <summary>
        /// LifeSupport generated per second from Consumables
        /// </summary>
        public const double CONV_LS_PER_SEC = CONV_CONS_PER_SEC;
        /// <summary>
        /// Conversation rate: Consumables per one LifeSupport
        /// </summary>
        public const double CONS_PER_LS = CONV_CONS_PER_SEC / CONV_LS_PER_SEC;

        /// <summary>
        /// Conversation rate of Consumables to EVA LifeSupport
        /// </summary>
        public const double CONS_PER_EVA_LS = 1.0 / 360.0;

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
        /// Program name to apply decal
        /// </summary>
        public const string PROG_APPLY_DECAL = "apply";

        public const string PROG_COLORMULT = "colormult";
        public const string PROG_GRAYSCALE = "grayscale";

        /// <summary>
        /// The flow mode when requesting LifeSupport in-game
        /// </summary>
        public const ResourceFlowMode FLOWMODE_LIFESUPPORT = ResourceFlowMode.NO_FLOW;

        /// <summary>
        /// When to warn player (yellow), in days.
        /// </summary>
        public const double GUI_LITEWARN_LIMIT = 1.0 / 6.0;
        /// <summary>
        /// When to warn player (red), in days.
        /// </summary>
        public const double GUI_HARDWARN_LIMIT = 1.0 / 6.0 / 12.0;

        public const string GUI_HARDWARN_COLOR = "<color=#ff3322>";
        public const string GUI_LITEWARN_COLOR = "<color=#ffee11>";

        public const string CONV_SPECIALIST = "Engineer";
    }
}
