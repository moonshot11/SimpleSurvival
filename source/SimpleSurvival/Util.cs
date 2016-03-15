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

                ScreenMessages.PostScreenMessage(kerbal.name + " ran out of LifeSupport and died!",
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
        /// Deduct the appropriate life support
        /// when first loading a vessel
        /// </summary>
        /// <param name="part">The Part with the life support PartModule</param>
        /// <param name="resource_name">The resource to drain</param>
        /// <param name="resource_rate">The resource drain rate (per second)</param>
        public static void StartupRequest(PartModule module, string resource_name, double resource_rate)
        {
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

            module.part.RequestResource(resource_name, request);
        }
    }
}
