using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public class EVALifeSupportModule : PartModule
    {
        /// <summary>
        /// Give the game a buffer to load everything, otherwise failed
        /// rescue contracts will not be registered
        /// </summary>
        private float kill_timer = C.KILL_BUFFER;

        public override void OnStart(StartState state)
        {
            // -- Check if resource is already added to part --
            bool found_resource = false;

            // -- Always reset grace_timer to two frames
            kill_timer = C.KILL_BUFFER;

            foreach (PartResource pr in part.Resources)
            {
                if (pr.resourceName == C.NAME_EVA_LIFESUPPORT)
                {
                    found_resource = true;
                    break;
                }
            }

            if (found_resource)
            {
                // If found, this EVA is already active - deduct LS.
                Util.StartupRequest(this, C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC);
            }
            else
            {
                Util.Log("Adding " + C.NAME_EVA_LIFESUPPORT + " resource to " + part.name);

                // Assumes EVA Kerbals will always have exactly one ProtoCrewMember
                string name = part.protoModuleCrew[0].name;

                // Kerbals assigned after this mod's installation should already be tracked,
                // but for Kerbals already in flight, add EVA LS according to current state
                // of astronaut complex
                EVALifeSupportTracker.AddKerbalToTracking(name);

                var info = EVALifeSupportTracker.GetEVALSInfo(name);

                ConfigNode resource_node = new ConfigNode("RESOURCE");
                resource_node.AddValue("name", C.NAME_EVA_LIFESUPPORT);
                resource_node.AddValue("amount", info.current.ToString());
                resource_node.AddValue("maxAmount", info.max.ToString());

                part.AddResource(resource_node);

                Util.Log("Added EVA LS to " + part.name);
            }

            base.OnStart(state);
        }

        public void FixedUpdate()
        {
            PartResource resource = part.Resources[C.NAME_EVA_LIFESUPPORT];
            double prev_amount = resource.amount;
            string kerbal_name = part.protoModuleCrew[0].name;

            EVALifeSupportTracker.SetCurrentEVAAmount(kerbal_name, prev_amount);

            // If Kerbal is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
                return;

            // -- Reduce resource --
            double retd = part.RequestResource(C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime);

            if (prev_amount > C.EVA_LS_30_SECONDS &&
                resource.amount <= C.EVA_LS_30_SECONDS)
            {
                TimeWarp.SetRate(0, true);
                string message = C.HTML_COLOR_WARNING +
                kerbal_name + " has 30 seconds to live!</color>";
                resource.amount = C.EVA_LS_30_SECONDS;

                ScreenMessage template = new ScreenMessage(message, 8f, ScreenMessageStyle.UPPER_CENTER);
                ScreenMessages.PostScreenMessage(template, true);
            }

            // Necessary to check if crew count > 0?
            if (retd == 0.0)
            {
                kill_timer -= TimeWarp.fixedDeltaTime;

                if (kill_timer <= 0)
                {
                    Util.KillKerbals(this);
                    part.explode();
                }

                #region FlightResultsDialog
                // string kerbal_name = part.protoModuleCrew[0].name; // Move to top of code block

                // These values persist if user switches to another craft
                //
                // FlightResultsDialog.showExitControls = true;
                // FlightResultsDialog.allowClosingDialog = true;

                // Prints log with this message at the top,
                // where flight status is typically displayed
                //
                // If user leaves focus immediately, Kerbal is not registered as dead
                //
                // FlightResultsDialog.Display(kerbal_name + " ran out of EVA LifeSupport!");
                #endregion
            }
        }
    }
}
