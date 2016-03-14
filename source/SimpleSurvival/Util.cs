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
        /// Checks whether resource is available for request from given part
        /// </summary>
        /// <param name="part">The part making this request</param>
        /// <param name="resource">The name of the resource</param>
        /// <param name="request">The amount of resource requested</param>
        /// <param name="flowmode">The FlowMode</param>
        /// <returns></returns>
        public static bool ResourceAvailable(Part part, string resource, double request,
            ResourceFlowMode flowmode = ResourceFlowMode.NULL)
        {
            // Will hold the value of the resource obtained
            double obtained = 0.0;
            
            // Check resource availability by 
            if (flowmode == ResourceFlowMode.NULL)
                obtained = part.RequestResource(resource, request);
            else
                obtained = part.RequestResource(resource, request, flowmode);

            // Store result if amount obtained is "close enough" to original request
            bool result = (Math.Abs(request - obtained) < C.DOUBLE_MARGIN);

            // Restore resource that was obtained
            if (flowmode == ResourceFlowMode.NULL)
                part.RequestResource(resource, -obtained);
            else
                part.RequestResource(resource, -obtained, flowmode);

            return result;
        }

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

                // Kerbal must be removed from part BEFORE calling Die()
                module.part.RemoveCrewmember(kerbal);

                // ...for some reason
                kerbal.Die();

                // Put Kerbal in Missing queue
                if (respawn_flag)
                    kerbal.StartRespawnPeriod();
            }
        }
    }
}
