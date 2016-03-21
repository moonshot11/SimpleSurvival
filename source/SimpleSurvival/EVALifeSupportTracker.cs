using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class EVALifeSupportTracker : MonoBehaviour
    {
        /// <summary>
        /// Stores the EVA info for a Kerbal
        /// </summary>
        public class EVALS_Info
        {
            public double current;
            public double max;

            public EVALS_Info(double current, double max)
            {
                this.current = current;
                this.max = max;
            }
        }

        /// <summary>
        /// Stores the live EVA LS tracking info
        /// </summary>
        public static Dictionary<string, EVALS_Info> evals_info = null;

        private void Awake()
        {
            Log("Call -> Awake(..) " + HighLogic.LoadedScene.ToString());

            GameEvents.onGameStateSave.Add(OnSave);
            GameEvents.onGameStateLoad.Add(OnLoad);

            // Kerbals will be added to tracking
            GameEvents.OnVesselRollout.Add(OnVesselRollout);

            // Cover all the situations when a Kerbal's EVA LS will be reset.
            // Unfortunately, roster status changes can't be used exclusively,
            // since the game moves ProtoCrewMembers from Assigned to Available,
            // then back to Assigned when transferring between parts/vessels
            // (e.g. "Transfer Crew" or EVA).
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
            GameEvents.onKerbalStatusChange.Add(OnKerbalStatusChange);
            GameEvents.onKerbalRemoved.Add(OnKerbalRemoved);
        }

        /// <summary>
        /// Add a Kerbal to EVA LS tracking
        /// </summary>
        /// <param name="name">Name of the Kerbal to add to tracking</param>
        public static void AddKerbalToTracking(string name)
        {
            Log("Call -> AddKerbal(..) for " + name);

            double eva_max = Util.CurrentEVAMax();

            if (evals_info.ContainsKey(name))
            {
                Log("Uh oh! " + name + "is already being tracked. Resetting...");
                evals_info.Remove(name);
            }

            Log("Adding current/max EVA LS of " + eva_max + " for " + name);

            EVALS_Info info = new EVALS_Info(eva_max, eva_max);
            evals_info.Add(name, info);
        }

        private void OnVesselRollout(ShipConstruct ship)
        {
            Log("Call -> OnVesselRollout(..) for vessel: " + ship.shipName);

            foreach (Part part in ship.Parts)
            {
                foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
                {
                    AddKerbalToTracking(kerbal.name);
                }
            }
        }

        private void OnVesselRecovered(ProtoVessel proto)
        {
            Log("Call -> OnVesselRecovered(..) for vessel: " + proto.vesselName);

            foreach (ProtoCrewMember kerbal in proto.GetVesselCrew())
            {
                Log("Clearing EVA LS data for: " + kerbal.name);
                evals_info.Remove(kerbal.name);
                Log("    Successful!");
            }
        }

        private void OnKerbalStatusChange(ProtoCrewMember kerbal,
            ProtoCrewMember.RosterStatus old_status, ProtoCrewMember.RosterStatus new_status)
        {
            Log("Call -> OnKerbalStatusChange(..) " + kerbal.name + ": " + old_status.ToString() + " -> " + new_status.ToString());

            if (new_status == ProtoCrewMember.RosterStatus.Dead
                || new_status == ProtoCrewMember.RosterStatus.Missing)
            {
                Log("Clearing EVA LS info for " + kerbal.name);
                evals_info.Remove(kerbal.name);
            }
        }

        private void OnKerbalRemoved(ProtoCrewMember kerbal)
        {
            Log("Call -> OnKerbalRemoved(..) " + kerbal.name);
            evals_info.Remove(kerbal.name);
        }

        private void OnLoad(ConfigNode gamenode)
        {
            Log("Call -> OnLoad(..)");

            evals_info = new Dictionary<string, EVALS_Info>();

            // -- Load from ConfigNode --
            if (gamenode.HasNode("SIMPLESURVIVAL_EVALS"))
            {
                ConfigNode evals_node = gamenode.GetNode("SIMPLESURVIVAL_EVALS");

                foreach (ConfigNode node in evals_node.GetNodes("KERBAL"))
                {
                    string name = node.GetValue("name");
                    double current = Convert.ToDouble(node.GetValue("amount"));
                    double max = Convert.ToDouble(node.GetValue("maxAmount"));

                    evals_info.Add(name, new EVALS_Info(current, max));
                    Log("Adding " + name + ": [" + current + ", " + max + "]");
                }
            }

            CheckInfoAgainstRoster();
        }

        private void CheckInfoAgainstRoster()
        {
            if (HighLogic.CurrentGame == null)
                return;

            // -- Check info against roster --
            foreach (ProtoCrewMember c in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                string name = c.name;

                /*if (c.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && !evals_info.ContainsKey(name))
                {
                    //Log("Adding " + name + " and 20/20");
                    //evals_info.Add(name, new EVALS_Info(20, 20));
                }
                else */
                if (c.rosterStatus != ProtoCrewMember.RosterStatus.Assigned && evals_info.ContainsKey(name))
                {
                    Log("Removing " + name);
                    evals_info.Remove(name);
                }
            }

        }

        private void OnSave(ConfigNode gamenode)
        {
            Log("Call -> OnSave(..)");

            // Write back to confignode
            ConfigNode topnode = new ConfigNode("SIMPLESURVIVAL_EVALS");

            foreach (string name in evals_info.Keys)
            {
                Log("Adding " + name + " to ConfigNode");
                var info = evals_info[name];

                ConfigNode node = topnode.AddNode("KERBAL");
                node.AddValue("name", name);
                node.AddValue("amount", info.current);
                node.AddValue("maxAmount", info.max);
            }

            gamenode.AddNode(topnode);
        }

        // This module needs its own logger
        private static void Log(string message)
        {
            KSPLog.print("SimpleSurvival EVALifeSupportTracker: " + message);
        }
    }
}
