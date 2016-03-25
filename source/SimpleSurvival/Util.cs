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

                ScreenMessages.PostScreenMessage(C.HTML_COLOR_ALERT + kerbal.name + " ran out of LifeSupport and died!</color>",
                    6f, ScreenMessageStyle.UPPER_CENTER);

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
        /// <returns></returns>
        public static double CurrentEVAMax()
        {
            float astro_lvl = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);

            Util.Log("Astronaut Complex Level " + astro_lvl);

            // If Astronaut Complex is fully upgraded, EVA LS gets higher value
            if (astro_lvl == 1.0f)
                return C.EVA_LS_LVL_3;
            else
                return C.EVA_LS_LVL_2;
        }

        /// <summary>
        /// Deduct the appropriate life support
        /// when first loading a vessel
        /// </summary>
        /// <param name="part">The Part with the life support PartModule</param>
        /// <param name="resource_name">The resource to drain</param>
        /// <param name="resource_rate">The resource drain rate (per second)</param>
        /// <returns>Returns false if insufficient resources. Otherwise, returns true.</returns>
        public static bool StartupRequest(PartModule module, string resource_name, double resource_rate)
        {
            if (IsContractVessel(module.vessel))
            {
                Util.Log("Vessel " + module.vessel.name + " is contract vessel. Skipping startup request.");
                return true;
            }

            if (module.vessel.mainBody.atmosphereContainsOxygen && module.vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
            {
                Util.Log("Vessel " + module.vessel.name + " is O2 atmo at " + module.vessel.altitude);
                Util.Log("Startup resource will not be drained");
                return true;
            }

            // Universal Time in seconds
            double lastUT = module.vessel.lastUT;
            double currUT = HighLogic.CurrentGame.UniversalTime;

            // Integer logic could overflow after 233 Kerbin years,
            // so maintain double values for arithmetic
            double delta = currUT - lastUT;
            double request = module.part.protoModuleCrew.Count * resource_rate * delta;

            Util.Log("LastUT = " + lastUT + " (" + KSPUtil.PrintDate((int)lastUT, true, true) + ")");
            Util.Log("CurrUT = " + currUT + " (" + KSPUtil.PrintDate((int)currUT, true, true) + ")");
            Util.Log("Time elapsed: " + delta + " (" + KSPUtil.PrintDateDelta((int)delta, true, true) + ")");
            Util.Log("Initial resource request (" + resource_name + "): " + request);

            double obtained = module.part.RequestResource(resource_name, request);

            return obtained > (request - C.STARTUP_KILL_MARGIN);
        }

        /// <summary>
        /// Formats a double for the VAB
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
    }
}
