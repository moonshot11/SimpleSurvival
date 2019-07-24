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
                Util.Log("info name = " + info.name);

                if (info.name.EndsWith("/125Tanks_BW"))
                {
                    Util.Log("Changing texture!");
                    Texture2D tex = info.texture;
                    Texture2D overlay = textures.Find(a => a.name.EndsWith("/125Tanks_OVERLAY")).texture;

                    tex = getRT(tex);
                    overlay = getRT(overlay);
                    Util.Log($"Starting transform at {DateTime.Now}");
                    for (int x = 0; x < tex.width; x++)
                    {
                        for (int y = 0; y < tex.height; y++)
                        {
                            Color overcol = overlay.GetPixel(x, y);
                            // TODO: Even commented out, overlay is not appearing on part in VAB
                            if (overcol.a < 0.1f)
                                continue;
                            tex.SetPixel(x, y, overcol);
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

        private Texture2D getRT(Texture2D tex)
        {
            // Get readable texture from overlay
            RenderTexture rt = RenderTexture.GetTemporary(
                tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var newtex = new Texture2D(tex.width, tex.height);
            newtex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            newtex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return newtex;
        }
    }
}
