using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Manages rescue contracts so that Kerbals in orbit don't die
    /// as soon as they come into focus.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ContractChecker : MonoBehaviour
    {
        /// <summary>
        /// Guid of a contract yet unaddressed by LifeSupportModule OnStart
        /// </summary>
        private const string NODE_RESCUE_CONTRACT_GUID = "CONTRACT_RECOVERASSET_UNHANDLED_GUID";

        /// <summary>
        /// List of GUIDs of yet-unaddressed rescue contracts
        /// </summary>
        public static List<string> Guids
        {
            get;
            private set;
        }

        /// <summary>
        /// The key in the ConfigNode pointing to the rescue contract's part
        /// housing the stranded Kerbal
        /// </summary>
        private const string PARTID_VALNAME = "partID";

        private void Awake()
        {
            Log("Awake()");

            Guids = new List<string>();
            
            GameEvents.Contract.onAccepted.Add(OnAccepted);
        }

        private void OnAccepted(Contracts.Contract c)
        {
            Log("OnAccepted(..)");
            PruneOldGuids();

            if (c is Contracts.Templates.RecoverAsset)
            {
                Contracts.Templates.RecoverAsset ra = (Contracts.Templates.RecoverAsset)c;

                // Isn't there a better way to access this than manually trawling
                // through ConfigNodes after the fact?
                string guid = ra.ContractGuid.ToString();

                // Ideally I would save the partID and apply the LifeSupport here,
                // but the partID is 0 and is not initialized until a scene change
                Log("Adding contract guid " + guid);
                Guids.Add(guid);
            }
        }

        public static string GetPartID(string guid)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return "";

            // Assuming node exists since Guids is populated
            ConfigNode contracts = HighLogic.CurrentGame.config.GetNode("SCENARIO").GetNode("CONTRACTS");
            
            // There has to be a better way to get the partID...
            foreach (ConfigNode contract in contracts.GetNodes("CONTRACT"))
            {
                if (contract.GetValue("guid") == guid && contract.HasValue(PARTID_VALNAME))
                    return contract.GetValue(PARTID_VALNAME);
                    
            }

            Log("Contract " + guid.ToString() + " is of type RecoverAsset, but no " + PARTID_VALNAME + " found!");

            return "";
        }

        /// <summary>
        /// Save the current GUIDs to a ConfigNode.
        /// </summary>
        /// <param name="scenario_node"></param>
        public static void Save(ConfigNode scenario_node)
        {
            Log("OnSave(..)");

            foreach (string guid in Guids)
            {
                Log("Adding guid to ConfigNode (" + guid + ")");
                scenario_node.AddNode(NODE_RESCUE_CONTRACT_GUID).AddValue("guid", guid);
            }
        }

        /// <summary>
        /// Load the current GUIDs from a ConfigNode.
        /// </summary>
        /// <param name="scenario_node"></param>
        public static void Load(ConfigNode scenario_node)
        {
            Log("OnLoad(..)");

            Guids.Clear();

            foreach (ConfigNode node in scenario_node.GetNodes(NODE_RESCUE_CONTRACT_GUID))
            {
                string guid = node.GetValue("guid");

                Log("Adding guid to tracking: " + guid);
                Guids.Add(node.GetValue("guid"));
            }
        }

        /// <summary>
        /// Remove stale Guids from the tracking list.
        /// </summary>
        private static void PruneOldGuids()
        {
            Log("PruneOldGuids(..)");

            List<string> newlist = new List<string>();

            var current_contracts = Contracts.ContractSystem.Instance.Contracts;

            // Move Guids that are found to new list...
            foreach (string guid in Guids)
            {
                Log("Searching for guid in contracts (" + current_contracts.Count +
                    " found): " + guid);

                foreach (Contracts.Contract contract in current_contracts)
                {
                    if (contract.ContractGuid.ToString() == guid)
                    {
                        Log("Found in contract: " + contract.Title);
                        newlist.Add(guid);
                        break;
                    }
                }

                if (!newlist.Contains(guid))
                    Log("Guid " + guid + " not found in current contracts, removing from tracking");
            }

            //...then point to new list once it contains up-to-date Guids
            Guids = newlist;
        }

        /// <summary>
        /// Print a log message unique to ContractChecker.
        /// </summary>
        /// <param name="message"></param>
        private static void Log(string message)
        {
            Util.Log("ContractChecker -> " + message);
        }
    }
}
