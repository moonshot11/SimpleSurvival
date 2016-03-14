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
        // The margin for double equality
        public const double DOUBLE_MARGIN = 0.000001;

        // Resource names
        public const string NAME_LS = "LifeSupport";
        public const string NAME_EVA_LS = "EVA LifeSupport";

        // Life Support Drain Rates
        private const double LS_PER_DAY_PER_KERBAL = 1.0;
        public const double LS_DRAIN_PER_SEC = LS_PER_DAY_PER_KERBAL / 216000.0;

        private const double EVA_LS_PER_DAY = 1.0;
        public const double EVA_LS_DRAIN_PER_SEC = EVA_LS_PER_DAY / 216000.0;

        // String since it is first created via ConfigNode
        public const string EVA_LS_MAX = "5";

        // Must be negative since it's a credit
        public const double LS_DEATH_CREDIT = -1.0;

    }
}
