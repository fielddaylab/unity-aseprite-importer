using Aseprite.Chunks;
using UnityEngine;

namespace Aseprite.PixelFormats
{
    public class IndexedPixelBuffer : PixelBuffer
    {
        public IndexedPixelBuffer(Frame frame, byte[] indices) : base(frame, indices)
        {
        }

        public override Color GetColor(int index)
        {
            PaletteChunk palette = Frame.File.GetChunk<PaletteChunk>();

            if (palette != null)
                return palette.GetColor(Data[index]);
            else
                return Color.magenta;
        }
    }
}
