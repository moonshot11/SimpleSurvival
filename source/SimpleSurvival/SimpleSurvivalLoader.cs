using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class SimpleSurvivalLoader : MonoBehaviour
    {
        private const string TOPNAME = "SIMPLESURVIVAL_MOD";

        public void Awake()
        {
            Util.Log("Loader Awake(..)");
            GameEvents.onGameStateLoad.Add(OnLoad);
            GameEvents.onGameStateSave.Add(OnSave);
        }

        public void OnSave(ConfigNode topnode)
        {
            Util.Log("Loader OnSave(..)");

            ConfigNode scenario_node = topnode.AddNode(TOPNAME);

            EVALifeSupportTracker.Save(scenario_node);
            ContractChecker.Save(scenario_node);
        }

        public void OnLoad(ConfigNode topnode)
        {
            Util.Log("Loader OnLoad(..)");

            ConfigNode scenario_node = topnode.GetNode(TOPNAME);

            EVALifeSupportTracker.Load(scenario_node);
            ContractChecker.Load(scenario_node);
        }
    }
}
