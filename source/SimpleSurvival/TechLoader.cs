using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Add the tech icon to the R&D tech tree.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TechLoader : MonoBehaviour
    {
        public void Awake()
        {
            KSP.UI.Screens.RDController.OnRDTreeSpawn.Add(OnRDTreeSpawn);
        }

        private void OnRDTreeSpawn(KSP.UI.Screens.RDController rd)
        {
            string refname = "RD_node_icon_simplesurvivalbasic";
            Texture2D texture = GameDatabase.Instance.GetTexture("SimpleSurvival/Tech/RD_node_icon_simplesurvivalbasic", false);
            RUI.Icons.Simple.Icon icon = new RUI.Icons.Simple.Icon(refname, texture);

            rd.iconLoader.iconDictionary.Add(refname, icon);

            // Custom nodes will be last on the list.
            // Speed up load time by beginning iteration backwards.
            for (int i = rd.nodes.Count-1; i >= 0; i--)
            {
                KSP.UI.Screens.RDNode node = rd.nodes[i];

                // This is the "id" field in the tech tree config
                if (node.tech.techID == "simplesurvivalBasic")
                {
                    Util.Log("Found SimpleSurvival Basic tech node");

                    // Sets the large icon in the righthand info panel
                    node.icon = icon;
                    node.iconRef = refname;

                    // Sets the tree icon
                    node.graphics.SetIcon(icon);

                    node.UpdateGraphics();
                    break;
                }
                
            }
        }
    }
}
