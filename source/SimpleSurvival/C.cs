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

        // -- Resource names --
        
        /// <summary>
        /// Name of the primary life support resource
        /// </summary>
        public const string NAME_LS = "LifeSupport";
        /// <summary>
        /// Name of the EVA life Support resource
        /// </summary>
        public const string NAME_EVA_LS = "EVA LifeSupport";
        /// <summary>
        /// Name of consumables resource
        /// </summary>
        public const string NAME_CN = "Consumables";

        // -- Life Support Drain Rates --

        /// <summary>
        /// Life Support drain rate per day (game time)
        /// </summary>
        private const double LS_PER_DAY_PER_KERBAL = 1.0;
        /// <summary>
        /// Life Support drain rate per second
        /// </summary>
        public const double LS_DRAIN_PER_SEC = LS_PER_DAY_PER_KERBAL / 216000.0;

        /// <summary>
        /// EVA Life Support drain rate per day (game time)
        /// </summary>
        private const double EVA_LS_PER_DAY = 1.0;
        /// <summary>
        /// EVA Life Support drain rate per second
        /// </summary>
        public const double EVA_LS_DRAIN_PER_SEC = EVA_LS_PER_DAY / 216000.0;

        /// <summary>
        /// Max amount of EVA Life Support
        /// </summary>
        // String since it is first created via ConfigNode
        public const string EVA_LS_MAX = "5";

        /// <summary>
        /// Amount of Life Support credited to a part when a Kerbal dies
        /// </summary>
        // Must be negative since it's a credit
        public const double LS_DEATH_CREDIT = -1.0;

    }
}
