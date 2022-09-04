﻿using System;
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

            Config.EVA_LS_LVL_1 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_ls_1", Config.EVA_LS_LVL_1));
            Config.EVA_LS_LVL_2 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_ls_2", Config.EVA_LS_LVL_2));
            Config.EVA_LS_LVL_3 = Convert.ToDouble(
                Util.GetConfigNodeValue(node, "eva_ls_3", Config.EVA_LS_LVL_3));
            Config.DEBUG_SHOW_EVA = Convert.ToBoolean(
                Util.GetConfigNodeValue(node, "debug", false));

            string evaMax = Util.GetConfigNodeValue(node, "eva_max_upgrade", null);
            Config.EVA_MAX_UPDATE = (EVAUpdateMode)Enum.Parse(typeof(EVAUpdateMode), evaMax, true);

            string convReq = Util.GetConfigNodeValue(node, "converter_req", null);
            Config.CONV_REQ = (ConverterReq) Enum.Parse(typeof(ConverterReq), convReq, true);

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
