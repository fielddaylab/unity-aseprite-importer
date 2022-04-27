using UnityEngine;

namespace Aseprite.Utils
{
    public class Texture2DUtil
    {
        public static Texture2D CreateTransparentTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new UnityEngine.Color32[width * height];

            // for (int i = 0; i < pixels.Length; i++) pixels[i] = UnityEngine.Color.clear;

            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }

        public static Texture2D CreateTransparentTexture(int width, int height, out Color32[] pixels)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels = new UnityEngine.Color32[width * height];

            // for (int i = 0; i < pixels.Length; i++) pixels[i] = UnityEngine.Color.clear;

            // texture.SetPixels32(pixels);
            // texture.Apply();

            return texture;
        }
    }
}