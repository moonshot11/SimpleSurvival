using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Loads the EVALifeSupportModule into the EVA parts on startup.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class EVALifeSupportLoader : MonoBehaviour
    {
        public void Awake()
        {
            Util.Log("Beginning setup of EVA LifeSupport");

            // -- Attach PartModule to EVA part(s) --

            Util.Log("Scanning loaded parts for EVA");

            List<AvailablePart> part_list = PartLoader.LoadedPartsList;
            part_list.Sort(
                delegate (AvailablePart p1, AvailablePart p2)
                {
                    return p1.name.CompareTo(p2.name);
                }
            );

            ConfigNode mod_node = new ConfigNode("MODULE");
            mod_node.AddValue("name", "EVALifeSupportModule");

            // Search each loaded part. If EVA part is found,
            // add EVALifeSupportModule.
            foreach (AvailablePart part in part_list)
            {
                string name = part.name;

                if (name.StartsWith("kerbalEVA"))
                {
                    Util.Log("Adding EVALifeSupportModule to " + name);

                    // Necessary to guarantee that this code block operates
                    // on kerbalEVA and kerbalEVAfemale
                    try
                    {
                        part.partPrefab.AddModule(mod_node);
                    }
                    catch (NullReferenceException e)
                    {
                        Util.Log("Catching exception, doesn't seem to affect game:");
                        Util.Log("  " + e.ToString());
                    }
                }
                else if (name == "fuelTank")
                {
                    Util.Log("Adding texture to, NOT " + name);
                }
            }

            // New?
            Texture2D result = null;
            var textures = GameDatabase.Instance.databaseTexture;
            foreach (GameDatabase.TextureInfo info in textures)
            {
                if (info.name.EndsWith("/125Tanks_BW"))
                {
                    Util.Log("Changing texture!");
                    Texture2D tex = info.texture;
                    Texture2D overlay = textures.Find(a => a.name.EndsWith("/125Tanks_OVERLAY")).texture;

                    tex = TextureUtil.MakeWritable(tex);
                    // Change to data parameters
                    int WIDTH = 420;
                    int HEIGHT = 420;
                    int originX = 550;
                    int originY = 800;
                    overlay = TextureUtil.MakeWritable(overlay, WIDTH, HEIGHT);
                    Util.Log($"Starting transform at {DateTime.Now}");
                    TextureUtil.FlipVertical(overlay);
                    TextureUtil.FlipHorizontal(overlay);
                    for (int x = 0; x < Math.Min(tex.width - originX, overlay.width); x++)
                    {
                        for (int y = 0; y < Math.Min(tex.height - originY, overlay.height); y++)
                        {
                            Color overcol = overlay.GetPixel(x, y);
                            if (overcol.a < 0.1f)
                                continue;
                            tex.SetPixel(originX + x, originY + y, overcol);
                        }
                    }
                    tex.Apply(true);
                    info.texture = tex;
                    result = tex;
                    Util.Log($"Completed transform at {DateTime.Now}");
                }
            }

            AvailablePart ap = PartLoader.getPartInfoByName("fuelTank");
            ap.Variants[0].Materials[0].mainTexture = result;

            Util.Log("Completed setup of EVA LifeSupport");
        }
    }
}
