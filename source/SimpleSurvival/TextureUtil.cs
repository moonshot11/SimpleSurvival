using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSurvival
{
    public struct DecalMap
    {
        public string Part;
        public string Decal;
        public int OriginX;
        public int OriginY;
        public int Width;
        public int Height;
        public int Rotate;
        public bool FlipHorizontal;
        public bool FlipVertical;
    }

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
        public static List<DecalMap> ReadDecalCfg(string filename)
        {
            List<DecalMap> result = new List<DecalMap>();

            using(StreamReader sr = new StreamReader(filename)) while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                line = Regex.Replace(line, @"#.*", "");
                if (Regex.IsMatch(line, @"^\s*$"))
                    continue;

                line = Regex.Replace(line, @"\s", "");
                var match = Regex.Match(line, @"^(.+):(\w+)\((\d+),(\d+)\)\((\d+),(\d+)\)(\d)(\d)");
                if (!match.Success)
                {
                    Util.Log($"WARN line in decal config not understood: {line}");
                    continue;
                }

                DecalMap decalmap;
                int i = 1;

                decalmap.Part = match.Groups[i++].Value;
                decalmap.Decal = match.Groups[i++].Value;
                decalmap.OriginX = int.Parse(match.Groups[i++].Value);
                decalmap.OriginY = int.Parse(match.Groups[i++].Value);
                decalmap.Width = int.Parse(match.Groups[i++].Value);
                decalmap.Height = int.Parse(match.Groups[i++].Value);
                decalmap.Rotate = int.Parse(match.Groups[i++].Value);
                int flip = int.Parse(match.Groups[i++].Value);

                decalmap.FlipHorizontal = flip % 2 == 1;
                decalmap.FlipVertical = flip >= 2;
                result.Add(decalmap);
            }

            return result;
        }
    }
}
