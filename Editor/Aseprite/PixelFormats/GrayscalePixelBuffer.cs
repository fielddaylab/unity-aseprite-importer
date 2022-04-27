using UnityEngine;

namespace Aseprite.PixelFormats
{
    public class GrayscalePixelBuffer : PixelBuffer
    {
        public GrayscalePixelBuffer(Frame frame, byte[] color) : base(frame, color)
        {
        }

        public override Color GetColor(int index)
        {
            float value = (float)Data[index * 2] / 255;
            float alpha = (float)Data[index * 2 + 1] / 255;

            return new Color(value, value, value, alpha);
        }
    }
}
