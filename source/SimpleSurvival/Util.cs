using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public static class Util
    {
        /// <summary>
        /// Print a line to the KSP log with a "SimpleSurvival:" prefix.
        /// </summary>
        /// <param name="message">Contents of log message</param>
        public static void Log(string message)
        {
            KSPLog.print("SimpleSurvival: " + message);
        }

        /// <summary>
        /// Print an empty log line.
        /// </summary>
        public static void Log()
        {
            KSPLog.print("");
        }

        /// <summary>
        /// This kills the Kerbal.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="kerbal"></param>
        public static void KillKerbal(PartModule module, ProtoCrewMember kerbal)
        {
            List<ProtoCrewMember> part_crew = module.part.protoModuleCrew;

            Util.PostUpperMessage(kerbal.name + " ran out of LifeSupport and died!", 2);

            // Kerbal must be removed from part BEFORE calling Die()
            module.part.RemoveCrewmember(kerbal);
            // Necessary for "Valentina Kermal was killed" message in log.
            // Doesn't seem to have any other effect.
            kerbal.Die();
            // Remove dead Kerbal's portrait - if not done, player will still be able
            // to control a ship with zero live Kerbals.
            // First two lines don't seem necessary, but may be more complete
            // if they update information used by other mods.
            module.vessel.CrewListSetDirty();
            Vessel.CrewWasModified(module.vessel);
            KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.StartRefresh(module.vessel);

            // Put Kerbal in Missing queue
            if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn)
                kerbal.StartRespawnPeriod();
        }

        /// <summary>
        /// Kill all Kerbals attached to this specific PartModule
        /// </summary>
        /// <param name="module"></param>
        public static void KillKerbals(PartModule module)
        {
            List<ProtoCrewMember> part_crew = module.part.protoModuleCrew;

            while (part_crew.Count > 0)
            {
                ProtoCrewMember kerbal = part_crew[0];
                bool respawn_flag = HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn;

                Util.PostUpperMessage(kerbal.name + " ran out of LifeSupport and died!", 2);

                // Kerbal must be removed from part BEFORE calling Die()
                module.part.RemoveCrewmember(kerbal);

                // ...for some reason
                kerbal.Die();

                // Put Kerbal in Missing queue
                if (respawn_flag)
                    kerbal.StartRespawnPeriod();
            }
        }

        /// <summary>
        /// Returns the current maximum EVA LifeSupport given the state
        /// of the astronaut complex
        /// </summary>
        /// <param name="choice">Which resource to return</param>
        /// <returns></returns>
        public static double CurrentEVAMax(EVA_Resource choice, string astro_level = "")
        {
            // Default astro_level value is "" instead of null
            // to capture the case where the input to this method
            // is a missing ConfigNode value.

            float lvl;

            if (astro_level == "")
                lvl = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
            else
                lvl = Convert.ToSingle(astro_level);

            Util.Log("Astronaut Complex Level " + lvl);

            // If Astronaut Complex is fully upgraded, EVA LS gets higher value

            if (choice == EVA_Resource.Propellant)
            {
                if (lvl == 1.0f)
                    return C.EVA_PROP_LVL_3;
                else
                    return C.EVA_PROP_LVL_2;
            }
            else if (choice == EVA_Resource.LifeSupport)
            {
                if (lvl == 1.0f)
                    return C.EVA_LS_LVL_3;
                else
                    return C.EVA_LS_LVL_2;
            }

            Util.Log("CheckThis -> Incorrect index, throwing exception");
            throw new ArgumentException("Index for SimpleSurvival Util.CurrentEVAMax must be [0,1].");
        }

        /// <summary>
        /// Deduct the appropriate life support
        /// when first loading a vessel
        /// </summary>
        /// <param name="part">The Part with the life support PartModule</param>
        /// <param name="resource_name">The resource to drain</param>
        /// <param name="resource_rate">The resource drain rate (per second)</param>
        /// <returns>Returns the number of seconds remaining after LifeSupport is deducted</returns>
        public static double StartupRequest(PartModule module, string resource_name, double resource_rate)
        {
            if (module.part.protoModuleCrew.Count == 0)
            {
                Util.Log("Part " + module.part.name + " has no crew - skipping LSM startup");
                return 0.0;
            }

            if (module.vessel.mainBody.atmosphereContainsOxygen && module.vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
            {
                Util.Log("Vessel " + module.vessel.name + " is O2 atmo at " + module.vessel.altitude);
                Util.Log("Startup resource will not be drained");
                return 0.0;
            }

            // Universal Time in seconds
            double lastUT = module.vessel.lastUT;
            double currUT = HighLogic.CurrentGame.UniversalTime;

            // Integer logic could overflow after 233 Kerbin years,
            // so maintain double values for arithmetic
            double delta = currUT - lastUT;
            double request = module.part.protoModuleCrew.Count * resource_rate * delta;

            // Startup should not be zero unless user is REALLY quick with the mouse
            // (i.e. never).
            if (request < C.DOUBLE_MARGIN)
            {
                Util.Log("CheckThis -> Startup request is zero. This is unexpected. Factors:");
                Util.Log("    Crew count    = " + module.part.protoModuleCrew.Count);
                Util.Log("    Resource rate = " + resource_rate);
                Util.Log("    Time delta    = " + delta);
                return 0.0;
            }

            Util.Log("LastUT = " + lastUT + " (" + KSPUtil.PrintDate((int)lastUT, true, true) + ")");
            Util.Log("CurrUT = " + currUT + " (" + KSPUtil.PrintDate((int)currUT, true, true) + ")");
            Util.Log("Time elapsed: " + delta + " (" + KSPUtil.PrintDateDelta((int)delta, true, true) + ")");
            Util.Log("Initial resource request (" + resource_name + "): " + request);

            // If user has disabled flow of LifeSupport to crewed part, assume this was in error
            // Re-enable so Kerbals don't immediately die upon vessel load
            module.part.Resources[resource_name].flowState = true;

            double obtained = module.part.RequestResource(resource_name, request, C.FLOWMODE_LIFESUPPORT);

            // Calculate remaining time that needs to be deducted from EVA LifeSupport (if applicable)
            return ((request - obtained) / request) * delta;
        }

        /// <summary>
        /// Formats a double for the VAB. Returns double truncated to one digit
        /// after the decimal.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FormatForGetInfo(double value)
        {
            string s = Math.Round(value, 1).ToString();

            if (!s.Contains('.'))
                s += ".0";

            return s;
        }

        /// <summary>
        /// Check if this is a contract-generated vessel that has not yet
        /// been activated by close approach.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns>True is vessel was generated by rescue contract, false otherwise</returns>
        public static bool IsContractVessel(Vessel vessel)
        {
            // IsControllable does not seem to be a reliable check,
            // since for controllable (non-contract) vessels,
            // it returns True during Contract acceptance,
            // but False when LifeSupportModule's Onstart() is called
            // during vessel loading.
            //
            // (vessel.lastUT == -1) && (vessel.launchTime == vessel.missionTime)
            // This was also used as criteria, but may not be reliable.

            return vessel.vesselType != VesselType.SpaceObject &&
                vessel.DiscoveryInfo.Level == DiscoveryLevels.Unowned;
        }

        /// <summary>
        /// Log a VERY verbose printout of a contract.
        /// </summary>
        /// <param name="contract">Reference to the Contract object</param>
        public static void PrintContractDetails(Contracts.Contract contract)
        {
            Util.Log("---------------------Contract Info---------------------------");
            Util.Log("ContractGuid = " + contract.ContractGuid);
            Util.Log("ContractID = " + contract.ContractID);
            Util.Log("ContractState = " + contract.ContractState);
            Util.Log("DateAccepted = " + contract.DateAccepted);
            Util.Log("DateDeadline = " + contract.DateDeadline);
            Util.Log("DateExpire = " + contract.DateExpire);
            Util.Log("DateFinished = " + contract.DateFinished);
            Util.Log("Description = " + contract.Description);
            Util.Log("Notes = " + contract.Notes);
            Util.Log("Prestige = " + contract.Prestige);
            Util.Log("Synopsys = " + contract.Synopsys);
            Util.Log("Title = " + contract.Title);
            Util.Log("-------------------------------------------------------------");
        }

        /// <summary>
        /// Post a message to UPPER_CENTER.
        /// </summary>
        /// <param name="message">The text to print.</param>
        /// <param name="level">0=info, 1=warning, 2=alert. Decides message color.</param>
        public static void PostUpperMessage(string message, int level = 0)
        {
            const float message_duration = 8f;

            string prefix;

            switch(level)
            {
                case 1:
                    prefix = C.HTML_COLOR_WARNING;
                    break;

                case 2:
                    prefix = C.HTML_COLOR_ALERT;
                    break;

                default:
                    prefix = "";
                    break;
            }

            message = prefix + message;

            if (level > 0)
                message += "</color>";

            ScreenMessage sm = new ScreenMessage(message, message_duration, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(sm);
        }

        /// <summary>
        /// Safely get a ConfigNode value if it exists
        /// </summary>
        /// <param name="node">Ref to the ConfigNode</param>
        /// <param name="key">Key.</param>
        /// <param name="default_value">Value to return if node does not exist.</param>
        /// <returns></returns>
        public static string GetConfigNodeValue(ConfigNode node, string key, object default_value)
        {
            if (node.HasValue(key))
                return node.GetValue(key);

            return default_value.ToString();
        }

        /// <summary>
        /// Return CurrentGame's AdvancedParams
        /// </summary>
        public static GameParameters.AdvancedParams AdvParams =>
            HighLogic.CurrentGame.Parameters.
            CustomParams<GameParameters.AdvancedParams>();
    }
}
