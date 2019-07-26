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

                    tex = MakeWritable(tex);
                    // Change to data parameters
                    int WIDTH = 420;
                    int HEIGHT = 420;
                    int originX = 550;
                    int originY = 800;
                    overlay = MakeWritable(overlay, WIDTH, HEIGHT);
                    Util.Log($"Starting transform at {DateTime.Now}");
                    FlipVertical(overlay);
                    FlipHorizontal(overlay);
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

        /// <summary>
        /// Make a read-only texture writable. Optionally, resize it.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private Texture2D MakeWritable(Texture2D texture, int width=-1, int height=-1)
        {
            // Attribution:
            // https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-

            // Throw exception
            if (width * height < 0)
                width = height = -1;
            if (width < 0)
                width = texture.width;
            if (height < 0)
                height = texture.height;

            // Store old RenderTexture
            RenderTexture prev = RenderTexture.active;

            // Get readable texture from overlay
            RenderTexture rt = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            Texture2D newtex = new Texture2D(width, height);
            newtex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            newtex.Apply();

            // Restore previously active RenderTexture
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return newtex;
        }

        /// <summary>
        /// Rotate a texture 90 degrees clockwise.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="cycles">Number of times to rotate.</param>
        private void RotateTexture(Texture2D texture, int cycles=0)
        {
            for (int i = 0; i < cycles; i++)
            {
                Color[] pixels = texture.GetPixels();
                texture.Resize(texture.height, texture.width);

                int index = 0;
                for (int x = texture.width - 1; x >= 0; x--)
                    for (int y = 0; y < texture.height; y++)
                        texture.SetPixel(x, y, pixels[index++]);
            }

        }

        /// <summary>
        /// Flip a texture in-place horizontally.
        /// </summary>
        /// <param name="texture"></param>
        private void FlipHorizontal(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            int index = 0;

            for (int y = 0; y < texture.height; y++)
                for (int x = texture.width - 1; x >= 0; x--)
                    texture.SetPixel(x, y, pixels[index++]);
        }

        /// <summary>
        /// Flip a texture in-place vertically.
        /// </summary>
        /// <param name="texture"></param>
        private void FlipVertical(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            int index = 0;

            for (int y = texture.height - 1; y >= 0; y--)
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, pixels[index++]);
        }
    }
}
