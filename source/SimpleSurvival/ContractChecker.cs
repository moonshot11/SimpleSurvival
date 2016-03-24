﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ContractChecker : MonoBehaviour
    {
        public static List<string> Guids
        {
            get;
            private set;
        }

        private const string PARTID_VALNAME = "partID";

        private void Awake()
        {
            Log("Start()");

            Guids = new List<string>();
            
            GameEvents.Contract.onAccepted.Add(OnAccepted);
        }

        private void OnAccepted(Contracts.Contract c)
        {
            Log("OnAccepted(..)");

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

            #region resource mod in-place
            /* This would work if a link to vessel or part ID existed here
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (!Util.IsContractVessel(vessel))
                    continue;

                Log("Contract vessel = " + vessel.name);
                ProtoVessel pv = vessel.protoVessel;

                foreach(ProtoPartSnapshot partsnap in pv.protoPartSnapshots)
                {
                    Log("PartSnapshot = " + partsnap.partName);

                    foreach (ProtoPartResourceSnapshot resnap in partsnap.resources)
                    {
                        Log("ResourceSnapshot = " + resnap.resourceName);
                        double max = Convert.ToDouble(resnap.resourceValues.GetValue("maxAmount"));
                        resnap.resourceValues.SetValue("amount", (max/2.0).ToString());
                    }
                }
            } */
            #endregion
        }

        public static string GetPartID(string guid)
        {
            // Assuming node exists since Guids is populated
            ConfigNode contracts = HighLogic.CurrentGame.config.GetNode("SCENARIO").GetNode("CONTRACTS");

            foreach (ConfigNode contract in contracts.GetNodes("CONTRACT"))
            {
                if (contract.HasValue(PARTID_VALNAME))
                    return contract.GetValue(PARTID_VALNAME);
                else
                    Log("Contract " + guid.ToString() + " is of type RecoverAsset, but no " + PARTID_VALNAME + " found!");
            }

            return "";
        }

        public static void Save(ConfigNode scenario_node)
        {
            Log("OnSave(..)");

            foreach (string guid in Guids)
            {
                Log("Adding guid to ConfigNode (" + guid + ")");
                scenario_node.AddNode(C.NODE_RESCUE_CONTRACT_GUID).AddValue("guid", guid);
            }
        }

        public static void Load(ConfigNode scenario_node)
        {
            Log("OnLoad(..) + ");

            Guids.Clear();

            foreach (ConfigNode node in scenario_node.GetNodes(C.NODE_RESCUE_CONTRACT_GUID))
            {
                string guid = node.GetValue("guid");

                Log("Adding guid to tracking: " + guid);
                Guids.Add(node.GetValue("guid"));
            }
        }

        private static void Log(string message)
        {
            Util.Log("ContractChecker -> " + message);
        }
    }
}