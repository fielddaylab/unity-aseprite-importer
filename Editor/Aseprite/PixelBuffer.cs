using UnityEngine;

namespace Aseprite
{
    public abstract class PixelBuffer
    {
        protected Frame Frame = null;
        protected byte[] Data = null;
        public abstract Color GetColor(int index);

        public Color this[int index] {
            get { return GetColor(index); }
        }

        public PixelBuffer(Frame frame, byte[] data)
        {
            Frame = frame;
            Data = data;
        }
    }
}

