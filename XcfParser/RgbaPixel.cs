using System;

namespace moe.kyre.TTTXcf.parser
{
    public struct RgbaPixel
    {
        public RgbaPixel(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public void SetChannel(int channel, byte value)
        {
            switch (channel)
            {
                case 0:
                    R = value;
                    break;
                case 1:
                    G = value;
                    break;
                case 2:
                    B = value;
                    break;
                case 3:
                    A = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel));
            }
        }
    }
}
