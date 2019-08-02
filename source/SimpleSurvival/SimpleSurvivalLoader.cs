using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Manages loading/saving of global tracking data.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class SimpleSurvivalLoader : MonoBehaviour
    {
        /// <summary>
        /// Top-level node that will appear in persistence file.
        /// </summary>
        private const string TOPNAME = "SIMPLESURVIVAL_MOD";

        public void Awake()
        {
            Util.Log("Loader Awake(..)");

            string url = "SimpleSurvival / settings / SIMPLESURVIVAL_SETTINGS";
            ConfigNode node = GameDatabase.Instance.GetConfigNode(url);

            C.EVA_LS_LVL_2 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_ls_2", C.EVA_LS_LVL_2));
            C.EVA_LS_LVL_3 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_ls_3", C.EVA_LS_LVL_3));
            C.EVA_PROP_LVL_2 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_prop_2", C.EVA_PROP_LVL_2));
            C.EVA_PROP_LVL_3 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_prop_3", C.EVA_PROP_LVL_3));

            GameEvents.onGameStateCreated.Add(OnLoad);
            GameEvents.onGameStateSave.Add(OnSave);
        }

        public void OnSave(ConfigNode topnode)
        {
            Util.Log("Loader OnSave(..) in " + HighLogic.LoadedScene.ToString());

            if (topnode.HasNode(TOPNAME))
                Util.Log("CheckThis -> Node " + TOPNAME + " already exists!");

            ConfigNode scenario_node = topnode.AddNode(TOPNAME);

            EVALifeSupportTracker.Save(scenario_node);
            ContractChecker.Save(scenario_node);
        }

        public void OnLoad(Game game)
        {
            Util.Log("Loader OnLoad(..) in " + HighLogic.LoadedScene.ToString());
            
            ConfigNode scenario_node;

            if (game.config.HasNode(TOPNAME))
                scenario_node = game.config.GetNode(TOPNAME);
            else
                scenario_node = new ConfigNode(TOPNAME);

            EVALifeSupportTracker.Load(scenario_node);
            ContractChecker.Load(scenario_node);
        }
    }
}
