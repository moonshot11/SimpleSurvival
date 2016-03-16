﻿using System;
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
        public const double DOUBLE_ALMOST_ONE = 1.0 - DOUBLE_MARGIN;

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

        // -- Life Support Drain Rates --

        /// <summary>
        /// Life Support drain rate per day (game time)
        /// </summary>
        private const double LS_PER_DAY_PER_KERBAL = 1.0;
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
        public const string EVA_LS_LVL_2 = "10";

        /// <summary>
        /// Max amount of EVA Life Support w/ Astronaut Complex Level 3
        /// </summary>
        public const string EVA_LS_LVL_3 = "300";

        /// <summary>
        /// Amount of Life Support credited to a part when a Kerbal dies
        /// </summary>
        // Must be negative since it's a credit
        public const double LS_DEATH_CREDIT = -1.0;

        // -- Converter rates --

        /// <summary>
        /// Consumables drained per second to generate LifeSupport
        /// </summary>
        public const double CONSUMABLES_DRAINED_PER_SEC = 0.1;
        /// <summary>
        /// ElectricCharge drained per second to generate LifeSupport
        /// </summary>
        public const double ELECTRICITY_DRAINED_PER_SEC = 0.1;
        /// <summary>
        /// LifeSupport generated per second from Consumables
        /// </summary>
        public const double LIFESUPPORT_ADDED_PER_CONS = -0.1;

        /// <summary>
        /// Altitude under which LifeSupport is not needed in atmosphere with oxygen
        /// </summary>
        public const double OXYGEN_CUTOFF_ALTITUDE = 10000.0;

    }
}
