using UnityEngine;

namespace Aseprite.PixelFormats
{
    public class RGBAPixelBuffer : PixelBuffer
    {
        public RGBAPixelBuffer(Frame frame, byte[] color) : base(frame, color)
        {
        }

        public override Color GetColor(int index)
        {
            int start = index * 4;
            int end = start + 4;
            if (end <= Data.Length)
            {
                float red = (float)Data[start] / 255f;
                float green = (float)Data[start + 1] / 255f;
                float blue = (float)Data[start + 2] / 255f;
                float alpha = (float)Data[start + 3] / 255f;

                return new Color(red, green, blue, alpha);
            }
            else
            {
                return UnityEngine.Color.magenta;
            }
        }
    }
}
