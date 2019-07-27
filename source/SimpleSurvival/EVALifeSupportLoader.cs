using System;
using System.IO;
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

            // New ?
            var textures = GameDatabase.Instance.databaseTexture;

            List<DecalMap> decals = TextureUtil.ReadDecalCfg(
                Util.Combine("GameData", "SimpleSurvival", "Decals", "decals.txt"));

            foreach (DecalMap decal in decals)
            {
                Util.Log("Modifying part " + decal.Part);
                AvailablePart part = PartLoader.getPartInfoByName(decal.Part);
                Texture2D overlay_orig = textures.Find(a => a.name.EndsWith('/' + decal.Decal)).texture;

                // Get original texture
                if (part.Variants == null)
                {
                    Util.Warn("  Variants is null. Skipping " + decal.Part);
                    continue;
                }
                string texname = part.Variants[0].Materials[0].mainTexture.name;
                Util.Log("  Found texture: " + texname);
                Texture2D tex = textures.Find(a => a.name == texname).texture;
                Texture2D result = TextureUtil.MakeWritable(tex);

                // Get resized decal
                Texture2D overlay = TextureUtil.MakeWritable(overlay_orig, decal.Width, decal.Height);

                // Transform decal
                Util.Log($"  Rotating CW {decal.Rotate} times");
                TextureUtil.Rotate(overlay, cycles: decal.Rotate);
                if (decal.FlipHorizontal)
                {
                    Util.Log("  Flipping horizontally");
                    TextureUtil.FlipHorizontal(overlay);
                }
                if (decal.FlipVertical)
                {
                    Util.Log("  Flipping vertically");
                    TextureUtil.FlipVertical(overlay);
                }

                // Place decal on original texture
                for (int x = 0; x < Math.Min(result.width - decal.OriginX, overlay.width); x++)
                {
                    for (int y = 0; y < Math.Min(result.height - decal.OriginY, overlay.height); y++)
                    {
                        Color overcol = overlay.GetPixel(x, y);
                        if (overcol.a < 0.1f)
                            continue;
                        result.SetPixel(decal.OriginX + x, decal.OriginY + y, overcol);
                    }
                }

                result.Apply(true);
                part.Variants[0].Materials[0].mainTexture = result;
            }

            Util.Log("Completed setup of EVA LifeSupport");
        }
    }
}
