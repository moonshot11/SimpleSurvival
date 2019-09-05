using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SimpleSurvival
{
    /// <summary>
    /// Add the tech icon to the R&D tech tree.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TechLoader : MonoBehaviour
    {
        public static Dictionary<AvailablePart, Texture2D> NewTextures =
            new Dictionary<AvailablePart, Texture2D>();

        public void Awake()
        {
            RDController.OnRDTreeSpawn.Add(OnRDTreeSpawn);
            RDNode.OnNodeSelected.Add(OnNodeSelected);
        }

        private void OnNodeSelected(RDNode node)
        {
            //if (!node.name.Contains("survival"))
            //    return;

            foreach (AvailablePart part in NewTextures.Keys)
            {
                Texture2D tex = NewTextures[part];
                Util.Log("ONS a");
                MeshRenderer mesh = part.iconPrefab.GetComponentInChildren<MeshRenderer>();
                Util.Log("ONS b");
                try
                {
                    Util.Log("ONS Getting mateiral 1");
                    var aa = mesh.material;
                    Util.Log("ONS Setting material 1");
                    aa.mainTexture = tex;
                }
                catch (NullReferenceException e)
                {
                    Util.Warn("ONS NRE on material");
                }
                Util.Log("ONS c");
                try
                {
                    Util.Log("ONS Getting sharedMaterial 1");
                    var aa = mesh.sharedMaterial;
                    Util.Log("ONS Setting sharedMaterial 1");
                    aa.mainTexture = tex;
                }
                catch (NullReferenceException e)
                {
                    Util.Warn("ONS NRE on sharedMaterial");
                }
                Util.Log("ONS d");

                /*var arr = part.iconPrefab.GetComponentsInChildren<Transform>();
                Util.Log($"ONS e length = {arr.Length}");
                for (int i = 0; i < arr.Length; i++)
                {
                    Util.PrintComponents(arr[i], $"Transform[{i}]", false);
                    Util.PrintComponents(arr[i], $"Transform[{i}] children", true);
                }*/
            }
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
