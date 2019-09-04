using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    public static class TextureUtil
    {
        /// <summary>
        /// Make a read-only texture writable. Optionally, resize it.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D MakeWritable(Texture2D texture, int width = -1, int height = -1)
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
            newtex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
        public static void Rotate(Texture2D texture, int cycles = 1)
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
        public static void FlipHorizontal(Texture2D texture)
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
        public static void FlipVertical(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            int index = 0;

            for (int y = texture.height - 1; y >= 0; y--)
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, pixels[index++]);
        }

        /// <summary>
        /// Translate decal config to a list of DecalMaps.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static List<string[]> ReadDecalCfg(string filename)
        {
            List<string[]> result = new List<string[]>();

            using(StreamReader sr = new StreamReader(filename)) while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();

                line = Regex.Replace(line, @"#.*", "");
                if (Regex.IsMatch(line, @"^\s*$"))
                    continue;

                string[] tokens = Regex.Split(line, @"[ \(\)\[\]:,]+");
                result.Add(tokens);
            }

            return result;
        }

        public static void ApplyDecals()
        {
            var textures = GameDatabase.Instance.databaseTexture;

            List<string[]> progs = ReadDecalCfg(
                Util.Combine("GameData", "SimpleSurvival", "Decals", "decals.txt"));

            string prevPartname = null;
            int prevVariant = -1;
            AvailablePart part = null;
            Texture2D result = null;

            foreach (string[] prog in progs)
            {
                if (prog.Length < 3)
                {
                    Util.Warn($"Prog error: length {prog.Length}");
                    continue;
                }

                string partname = prog[0];
                int variant = int.Parse(prog[1]);
                string progname = prog[2];
                int tokenIndex = 3;

                bool setIcon = partname.Last() == '*';
                if (setIcon)
                    partname = partname.Remove(partname.Length - 1);

                Util.Log("Modifying part " + partname);
                Util.Log("  Prog: " + progname);

                if (partname != prevPartname || variant != prevVariant)
                {
                    part = PartLoader.getPartInfoByName(partname);
                    if (variant >= part.Variants.Count)
                    {
                        Util.Warn($"Variant #{variant} out of range; part has {part.Variants.Count} variants");
                        prevPartname = null;
                        prevVariant = -1;
                        continue;
                    }
                    string texname = part.Variants[variant].Materials[0].mainTexture.name;
                    Util.Log("  Found texture: " + texname);
                    Texture2D tex = textures.Find(a => a.name == texname).texture;
                    result = MakeWritable(tex);
                    prevPartname = partname;
                    prevVariant = variant;
                }

                if (progname == C.PROG_APPLY_DECAL)
                {
                    if (prog.Length < 9)
                    {
                        Util.Warn("  Fewer than 9 req'd tokens found; skipping");
                        continue;
                    }

                    string decal = prog[tokenIndex++];
                    int originX = int.Parse(prog[tokenIndex++]);
                    int originY = int.Parse(prog[tokenIndex++]);
                    int width = int.Parse(prog[tokenIndex++]);
                    int height = int.Parse(prog[tokenIndex++]);
                    int rotate = int.Parse(prog[tokenIndex++]);
                    int flip = int.Parse(prog[tokenIndex++]);

                    Texture2D overlay_orig = textures.Find(a => a.name.EndsWith('/' + decal)).texture;

                    // Get original texture
                    if (part.Variants == null)
                    {
                        Util.Warn("  Variants is null. Skipping " + partname);
                        continue;
                    }

                    // Get resized decal
                    Texture2D overlay = MakeWritable(overlay_orig, width, height);

                    // Transform decal
                    Util.Log($"  Rotating CW {rotate} times");
                    Rotate(overlay, cycles: rotate);
                    if (flip % 2 == 1)
                    {
                        Util.Log("  Flipping horizontally");
                        FlipHorizontal(overlay);
                    }
                    if (flip >= 2)
                    {
                        Util.Log("  Flipping vertically");
                        FlipVertical(overlay);
                    }

                    // Place decal on original texture
                    for (int x = 0; x < Math.Min(result.width - originX, overlay.width); x++)
                    {
                        for (int y = 0; y < Math.Min(result.height - originY, overlay.height); y++)
                        {
                            Color overcol = overlay.GetPixel(x, y);
                            if (overcol.a < 0.1f)
                                continue;
                            result.SetPixel(originX + x, originY + y, overcol);
                        }
                    }

                }
                else if (progname == C.PROG_COLORMULT)
                {
                    float rfact = float.Parse(prog[tokenIndex++]);
                    float gfact = float.Parse(prog[tokenIndex++]);
                    float bfact = float.Parse(prog[tokenIndex++]);
                    int originX = int.Parse(prog[tokenIndex++]);
                    int originY = int.Parse(prog[tokenIndex++]);
                    int width = int.Parse(prog[tokenIndex++]);
                    int height = int.Parse(prog[tokenIndex++]);

                    for (int x = originX; x < Math.Min(result.width, originX + width); x++)
                    {
                        for (int y = originY; y < Math.Min(result.height, originY + height); y++)
                        {
                            Color col = result.GetPixel(x, y);
                            col.r *= rfact;
                            col.g *= gfact;
                            col.b *= bfact;
                            result.SetPixel(x, y, col);
                        }
                    }
                }
                else if (progname == C.PROG_GRAYSCALE)
                {
                    Color[] pixels = result.GetPixels();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        float gs = pixels[i].grayscale;
                        pixels[i].r = gs;
                        pixels[i].g = gs;
                        pixels[i].b = gs;
                    }
                    result.SetPixels(pixels);
                }
                else
                {
                    Util.Warn($"  Program {progname} not recognized");
                    continue;
                }

                System.IO.File.WriteAllBytes("asdf", result.EncodeToPNG());
                GameDatabase.Instance.databaseTexture.Add(
                    new GameDatabase.TextureInfo(new UrlDir.UrlFile(null, new FileInfo("asdf")), result, false, true, false));

                result.Apply(true);
                //part.Variants[variant].Materials[0].mainTexture = result;
                part.Variants[variant].Materials[0].SetTexture("asdf", result);
                if (variant == 0)
                {
                    part.partPrefab.baseVariant.Materials[0].mainTexture = result;
                    part.variant.Materials[0].mainTexture = result;
                }

                for (int i = 0; i < part.Variants.Count; i++)
                    for (int j = 0; j < part.Variants[i].Materials.Count; j++)
                        Util.Log($"i = {i}, j = {j}");

                if (setIcon)
                {
                    Util.Log($"Setting RDicon for {part.title}");

                    // Util.Log("RelinkPrefab()");
                    // part.partPrefab.RelinkPrefab();

                    TechLoader.PartIcons.Add(part, result);
                    Util.Log("aa");
                    //part.variant = part.Variants[variant];
                    Util.Log("bb");
                    //part.partPrefab.baseVariant = part.variant;
                    Util.Log("cc");
                    //part.partPrefab.variants.SetVariant(part.Variants[variant].Name);
                    Util.Log("dd");
                    //VariantSetter.NewVariants[part] = part.Variants[3];

                }
            }

            Util.Log("Completed setup of EVA LifeSupport");
        }
    }
}
