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

            public EVALS_Info Copy()
            {
                return new EVALS_Info(current, max);
            }
        }

        /// <summary>
        /// Header for the ConfigNode section that will contain EVA LS tracking
        /// </summary>
        private static string NODE_HEADER = "SIMPLESURVIVAL_MOD";
        /// <summary>
        /// Title of each individual node holding one Kerbal's info
        /// </summary>
        private static string NODE_INNER_TITLE = "KERBAL_EVA_LS";

        /// <summary>
        /// Stores the live EVA LS tracking info
        /// </summary>
        private static Dictionary<string, EVALS_Info> evals_info = null;

        /// <summary>
        /// Run once on startup, set up hooks to rest of game
        /// </summary>
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

            // Assume that this Kerbal's info should be reset,
            // but warn in the log file just in case.
            if (evals_info.ContainsKey(name))
            {
                Log("Uh oh! " + name + "is already being tracked. Resetting...");
                evals_info.Remove(name);
            }

            Log("Adding current/max EVA LS of " + eva_max + " for " + name);

            EVALS_Info info = new EVALS_Info(eva_max, eva_max);
            evals_info.Add(name, info);
        }

        /// <summary>
        /// Returns true if the Kerbal's EVA LS is currently being tracked
        /// </summary>
        /// <param name="name">Name of Kerbal as defined by game</param>
        /// <returns>True if tracked, false otherwise.</returns>
        public static bool InTracking(string name)
        {
            return evals_info.ContainsKey(name);
        }

        /// <summary>
        /// Returns a COPY to this Kerbal's EVA LS info.
        /// Cannot modify in place.
        /// </summary>
        /// <param name="name">Name of Kerbal</param>
        /// <returns></returns>
        public static EVALS_Info GetEVALSInfo(string name)
        {
            if (!evals_info.ContainsKey(name))
                return null;

            return evals_info[name].Copy();
        }

        /// <summary>
        /// When the ship is "rolled out" from the VAB to the LaunchPad,
        /// via the VAB or the Launch GUI
        /// </summary>
        /// <param name="ship"></param>
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

        /// <summary>
        /// Update the amount of EVA LS a Kerbal currently has
        /// </summary>
        /// <param name="name">The Kerbal's name</param>
        /// <param name="amount">The current amount</param>
        public static void SetCurrentEVAAmount(string name, double amount)
        {
            try
            {
                evals_info[name].current = amount;
            }
            catch (KeyNotFoundException e)
            {
                Log("SetCurrentEVAAmount Exception thrown: Kerbal " + name + " not found to update tracking!");
                Log(e.ToString());
            }
        }

        /// <summary>
        /// Add to the current EVA amount
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        public static void AddEVAAmount(string name, double amount)
        {
            try
            {
                evals_info[name].current += amount;
            }
            catch (KeyNotFoundException e)
            {
                Log("AddEVAAmount Exception thrown: Kerbal " + name + " not found to update tracking!");
                Log(e.ToString());
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
            // Kerbals regularly change to Available, then back to Assigned
            // when in space, so can't use this method to track Kerbals in service
            if (new_status == ProtoCrewMember.RosterStatus.Dead
                || new_status == ProtoCrewMember.RosterStatus.Missing)
            {
                Log("Call -> OnKerbalStatusChange(..) " + kerbal.name + ": " + old_status.ToString() + " -> " + new_status.ToString());
                Log("Clearing EVA LS info for " + kerbal.name);
                evals_info.Remove(kerbal.name);
            }
        }

        // Currently only seems to track Kerbals removed entirely from roster,
        // but keep in for the sake of completeness for now
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
            if (gamenode.HasNode(NODE_HEADER))
            {
                ConfigNode evals_node = gamenode.GetNode(NODE_HEADER);

                foreach (ConfigNode node in evals_node.GetNodes(NODE_INNER_TITLE))
                {
                    string name = node.GetValue("name");
                    double current = Convert.ToDouble(node.GetValue("amount"));
                    double max = Convert.ToDouble(node.GetValue("maxAmount"));

                    evals_info.Add(name, new EVALS_Info(current, max));
                    Log("Adding " + name + ": [" + current + ", " + max + "]");
                }
            }
        }

        private void OnSave(ConfigNode gamenode)
        {
            Log("Call -> OnSave(..)");

            if (gamenode == null)
            {
                Log("gamenode is null, aborting OnSave");
                return;
            }

            if (evals_info == null)
            {
                Log("evals_info is null, aborting OnSave");
                return;
            }

            // Write back to confignode
            ConfigNode topnode = null;

            if (gamenode.HasNode(NODE_HEADER))
                topnode = gamenode.GetNode(NODE_HEADER);
            else
                topnode = new ConfigNode(NODE_HEADER);

            foreach (string name in evals_info.Keys)
            {
                var info = evals_info[name];

                Log("Adding " + name + " to ConfigNode");
                Log("  name      = " + name);
                Log("  amount    = " + info.current);
                Log("  maxAmount = " + info.max);

                ConfigNode node = topnode.AddNode(NODE_INNER_TITLE);
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
