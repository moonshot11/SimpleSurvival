using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    #region Delete
    /*[KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class SimpleSurvivalLoader_Initializer : MonoBehaviour
    {
        private void Awake()
        {
            Game game = HighLogic.CurrentGame;

            if (game == null)
                return;

            bool installed = false;

            foreach (ProtoScenarioModule module in game.scenarios)
            {
                if (module.moduleName == typeof(SimpleSurvivalLoader).Name)
                {
                    Util.Log("Found SimpleSurvivalLoader already installed");
                    installed = true;
                    break;
                }
            }

            if (!installed)
            {
                Util.Log("Installing SimpleSurvivalLoader");
                var p = game.AddProtoScenarioModule(typeof(SimpleSurvivalLoader),
                    GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER,
                    GameScenes.TRACKSTATION);
            }
        }
    }*/
    #endregion

    [KSPScenario(ScenarioCreationOptions.AddToAllGames,
        GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class SimpleSurvivalLoader : ScenarioModule
    {
        public override void OnSave(ConfigNode scenario_node)
        {
            Util.Log("Loader OnSave(..)");

            EVALifeSupportTracker.Save(scenario_node);
            ContractChecker.Save(scenario_node);
        }

        public override void OnLoad(ConfigNode scenario_node)
        {
            Util.Log("Loader OnLoad(..)");

            EVALifeSupportTracker.Load(scenario_node);
            ContractChecker.Load(scenario_node);
        }
    }
}
