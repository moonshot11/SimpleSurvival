using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class EVALifeSupportTracker : MonoBehaviour
    {
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

        public static Dictionary<string, EVALS_Info> evals_info = null;

        public void Awake()
        {
            ConfigNode gamenode = HighLogic.CurrentGame.config;

            if (evals_info == null)
            {
                Log("Initializing evals_info");
                evals_info = new Dictionary<string, EVALS_Info>();
                
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
            }

            foreach (ProtoCrewMember c in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                string name = c.name;

                if (c.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && !evals_info.ContainsKey(name))
                {
                    Log("Adding " + name + " and 20/20");
                    evals_info.Add(name, new EVALS_Info(20, 20));
                }
                else if (c.rosterStatus != ProtoCrewMember.RosterStatus.Assigned && evals_info.ContainsKey(name))
                {
                    Log("Removing " + name);
                    evals_info.Remove(name);
                }
            }

            GameEvents.onGameStateSave.Add(OnSave);
        }

        public void OnSave(ConfigNode gamenode)
        {
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

        public void Update()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            // If EVA
            if (vessel.isEVA)
            {
                string name = vessel.GetVesselCrew()[0].name;

                evals_info[name].current = vessel.rootPart.Resources[C.NAME_EVA_LIFESUPPORT].amount;

            }
        }

        private void Log(string message)
        {
            KSPLog.print("SimpleSurvival EVALifeSupportTracker: " + message);
        }
    }
}
