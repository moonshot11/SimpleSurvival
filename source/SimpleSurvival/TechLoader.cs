using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Add the tech icon to the R&D tech tree.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TechLoader : MonoBehaviour
    {
        /// <summary>
        /// Part name -> Texture
        /// </summary>
        public static Dictionary<AvailablePart, Texture2D> PartIcons =
            new Dictionary<AvailablePart, Texture2D>();

        public void Awake()
        {
            KSP.UI.Screens.RDController.OnRDTreeSpawn.Add(OnRDTreeSpawn);
            KSP.UI.Screens.RDNode.OnNodeSelected.Add(OnNodeSelected);
            KSP.UI.Screens.RDTechTree.OnTechTreeSpawn.Add(OnTechTreeSpawn);
        }

        private void OnTechTreeSpawn(KSP.UI.Screens.RDTechTree tree)
        {
            Util.Log("Call OnTechTreeSpawn");

            // icon start
            string refname = "RD_node_icon_simplesurvivalbasic";
            Texture2D texture = GameDatabase.Instance.GetTexture("SimpleSurvival/Tech/RD_node_icon_simplesurvivalbasic", false);
            RUI.Icons.Simple.Icon icon = new RUI.Icons.Simple.Icon(refname, texture);

            tree.controller.iconLoader.iconDictionary[refname] = icon;
            // icon end

            ProtoTechNode node = tree.FindTech("simplesurvivalBasic");
            var parts = node.partsPurchased;
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].variant = parts[i].Variants[0];
                parts[i].partPrefab.variants.SetVariant(parts[i].Variants[0].Name);

                parts[i].partPrefab.baseVariant = parts[i].Variants[0];
                foreach (var variant in parts[i].partPrefab.variants.variantList)
                    Util.Log($"Var name = {variant.Name}, {variant.DisplayName}");
            }
        }

        private void OnNodeSelected(KSP.UI.Screens.RDNode node)
        {
            Util.Log("Call OnNodeSelected -> " + node.name);
            if (node == null)
            {
                Util.Warn("node is null");
                return;
            }
            if (node.tech == null)
            {
                Util.Warn("tech is null");
                return;
            }

            Util.Log("Before parts");
            if (node.tech.partsAssigned != null)
                foreach (AvailablePart part in node.tech.partsAssigned)
                    if (part.variant != null && part.Variants != null && part.Variants.Count >= 1)
                        part.variant = part.Variants[0];
            Util.Log("Middle parts");
            if (node.tech.partsPurchased != null)
                foreach (AvailablePart part in node.tech.partsPurchased)
                    if (part.variant != null && part.Variants != null && part.Variants.Count >= 1)
                        part.variant = part.Variants[0];
            Util.Log("After parts");

            var rd = node.controller;
            foreach (var pair in PartIcons)
                rd.partList.partIcons[pair.Key] = pair.Value;
            node.UpdateGraphics();

            var co = node.controller.partList.partIcons;
            Util.Log($"button image: {node.graphics.button.Image.name}");
            Util.Log($"Selection image: {node.graphics.selection.name}");
            var tool = node.graphics.tooltip;

            Util.PrintComponents(node, "node");
            Util.PrintComponents(node.tech, "node.tech");
            Util.PrintComponents(node.graphics, "node.graphics");
            Util.PrintComponents(tool, "tool");
            Util.Log("----");
            Util.PrintComponents(tool.prefab, "tool.prefab");
            Util.PrintComponents(node.graphics.GetComponent<UnityEngine.CanvasRenderer>(), "graphics CanvasRenderer");
            Util.Log("----");
            Util.PrintComponents(
                node.graphics.GetComponent<UnityEngine.CanvasRenderer>().GetComponent<UnityEngine.UI.VerticalLayoutGroup>(), "VerticalLayoutGroup");

            Util.PrintComponents(node.graphics.GetComponent<UnityEngine.CanvasRenderer>().GetComponent<UnityEngine.UI.ContentSizeFitter>(), "ContentSizeFitter");


            if (node.name.Contains("Habitation"))
            {
                Util.Log("Internal scanning node");
                foreach (var pair in co)
                {
                    AvailablePart part = pair.Key;

                    Util.Log($"Internal: {part.name}, {part.name}");
                    if (part.iconPrefab == null)
                        Util.Log("iconPrefab is null");
                    else if (part.iconPrefab.GetComponent<UnityEngine.Renderer>() == null)
                        Util.Log("renderer is null");
                    else if (part.iconPrefab.GetComponent<UnityEngine.Renderer>().material == null)
                        Util.Log("material is null");

                    Util.PrintComponents(part.iconPrefab, "Look iconPrefab");
                    Util.Log("Name = " + part.iconPrefab.transform.name);
                    var transform = part.iconPrefab.GetComponent<UnityEngine.Transform>();
                    transform.Rotate(90f, 45f, 0f);

                    //part.iconPrefab.GetComponent<UnityEngine.Renderer>().material.mainTexture = pair.Value;
                    Util.Log("Internal AA");
                    //part.iconPrefab.GetComponent<UnityEngine.Renderer>().sharedMaterial.mainTexture = pair.Value;
                    Util.Log("Internal BB");
                    part.iconScale /= 2f;
                    //part.partConfig.GetNode("MODULE").GetNode("VARIANT", "name", "BlackAndWhite").GetNode("TEXTURE").SetValue("mainTextureURL", "Squad/Parts/FuelTank/Size1_Tanks/125Tanks_O");
                    var nodes = part.partConfig.GetNode("MODULE").GetNodes("VARIANT");
                    foreach (var vnode in nodes)
                    {
                        vnode.GetNode("TEXTURE").SetValue("mainTextureURL", "Squad/Parts/FuelTank/Size1_Tanks/125Tanks_O");
                    }
                    // -> ModulePartVariants: simplesurvivalT400
                    Util.PrintComponents(part.partPrefab, "Just print");
                    Util.Log("Setting MPV to " + part.Variants[0].Name);
                    foreach (var v in part.Variants)
                        Util.Log("  name: " + v.Name);
                    part.partPrefab.variants.SetVariant(part.Variants[1].Name);
                    //part.iconPrefab = null;
                }
                
            }


        }

        private void OnRDTreeSpawn(KSP.UI.Screens.RDController rd)
        {
            Util.Log("Call OnRDTreeSpawn");
            string refname = "RD_node_icon_simplesurvivalbasic";
            string url = "SimpleSurvival/Tech/RD_node_icon_simplesurvivalbasic";
            Texture2D texture = GameDatabase.Instance.GetTexture(url, false);
            RUI.Icons.Simple.Icon icon = new RUI.Icons.Simple.Icon(refname, texture);
            Util.Log("RefreshVariants()");
            // KSP.UI.Screens.EditorPartList.Instance.RefreshVariants();

            rd.iconLoader.iconDictionary[refname] = icon;
            Util.PrintComponents(rd.partList, "rd.partList");
            Util.PrintComponents(ResearchAndDevelopment.Instance, "RnD.Instance");

            // Reapply custom textures
            foreach (var pair in PartIcons)
            {
                Util.Log($"Apply RDPart texture to {pair.Key.title}");
                rd.partList.partIcons[pair.Key] = pair.Value;
                Util.PrintComponents(pair.Key.iconPrefab, "pair.iconPrefab");
            }

            /*foreach (var node in AssetBase.RnDTechTree.GetTreeNodes())
            {
                if (node.tech.partsPurchased == null)
                    Util.Log("tech.partsPurchased is null");
                else
                    Util.Log("Count = " + node.tech.partsPurchased.Count);
            }*/

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

                    var assigned = node.tech.partsAssigned;
                    var purchased = node.tech.partsPurchased;
                    Util.Log($"assigned count = {assigned.Count}");
                    Util.Log($"purchased count = ${purchased.Count}");

                    foreach (var part in assigned)
                    {
                        Util.Log("Modifying part " + part.title);
                        //part.variant = part.Variants[0];
                        //Util.Log("aa");
                        //part.partPrefab.baseVariant = part.Variants[0];
                        //Util.Log("bb");
                        //part.partPrefab.variants.SetVariant(part.Variants[0].Name);
                        //Util.Log("cc");
                        if (part.iconPrefab == null)
                            Util.Warn("  iconPrefab is null!");
                        else
                        {
                            foreach (var comp in part.iconPrefab.GetComponents(typeof(UnityEngine.Component)))
                                if (comp != null)
                                    Util.Log($"  -> {comp.GetType()}: {comp.name}");
                            //part.iconPrefab.transform.Rotate(90f, 60f, 100f);
                            //part.partPrefab.transform.Rotate(90f, 60f, 100f);
                            /*try
                            {
                                Util.Log("Try rechange 11");
                                part.iconPrefab.GetComponent<UnityEngine.Renderer>().material.mainTexture = part.Variants[0].Materials[0].mainTexture;
                            }
                            catch (System.NullReferenceException e)
                            {
                                Util.Log("Caught NULL 11");
                            }

                            try
                            {
                                Util.Log("Try rechange 22");
                                part.iconPrefab.GetComponent<UnityEngine.Renderer>().sharedMaterial.mainTexture = part.Variants[0].Materials[0].mainTexture;
                            }
                            catch (System.NullReferenceException e)
                            {
                                Util.Log("Caught NULL 22");
                            }

                            try
                            {
                                Util.Log("Try rechange 33");
                                part.partPrefab.GetComponent<UnityEngine.Renderer>().material.mainTexture = part.Variants[0].Materials[0].mainTexture;
                            }
                            catch (System.NullReferenceException e)
                            {
                                Util.Log("Caught NULL 33");
                            }

                            try
                            {
                                Util.Log("Try rechange 44");
                            part.partPrefab.GetComponent<UnityEngine.Renderer>().sharedMaterial.mainTexture = part.Variants[0].Materials[0].mainTexture;

                            }
                            catch (System.NullReferenceException e)
                            {
                                Util.Log("Caught NULL 44");
                            }

                            try
                            {
                                Util.Log("Try rechange 55");
                            part.partPrefab.GetComponent<KSP.UI.Screens.EditorPartIcon>().materials[0].mainTexture = part.Variants[0].Materials[0].mainTexture;
                            }
                            catch (System.NullReferenceException e)
                            {
                                Util.Log("Caught NULL 55");
                            }

                            try
                            {
                                Util.Log("Try rechange 66");
                            part.iconPrefab.GetComponent<KSP.UI.Screens.EditorPartIcon>().materials[0].mainTexture = part.Variants[0].Materials[0].mainTexture;
                            }
                            catch (System.NullReferenceException e)
                            {
                                Util.Log("Caught NULL 66");
                            }

                            Util.Log("Try rechange 77");*/
                        }
                    }

                    node.UpdateGraphics();
                    //rd.partList.SetupParts(node);
                    break;
                }
            }
        }
    }
}
