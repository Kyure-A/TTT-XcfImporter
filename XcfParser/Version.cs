using System;
using System.IO;
using System.Text;

namespace moe.kyre.TTTXcf.parser
{
    public readonly struct Version
    {
        public Version(ushort num)
        {
            Num = num;
        }

        public ushort Num { get; }

        public static Version Parse(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (bytes.Length != 4)
            {
                throw new EndOfStreamException();
            }

            var tag = Encoding.ASCII.GetString(bytes);
            if (tag == "file")
            {
                return new Version(0);
            }

            if (bytes[0] == (byte)'v')
            {
                var numberString = Encoding.ASCII.GetString(bytes, 1, 3).TrimEnd('\0');
                if (ushort.TryParse(numberString, out var num))
                {
                    return new Version(num);
                }
            }

            throw new XcfParseException("Unknown XCF version tag");
        }

        public int BytesPerOffset => Num >= 11 ? 8 : 4;
    }

    public enum ColorType : uint
    {
        Rgb = 0,
        Grayscale = 1,
        Indexed = 2,
    }

    public static class ColorTypeExtensions
    {
        public static ColorType Parse(uint value)
        {
            switch (value)
            {
                case 0:
                    return ColorType.Rgb;
                case 1:
                    return ColorType.Grayscale;
                case 2:
                    return ColorType.Indexed;
                default:
                    throw new XcfParseException("Invalid color type");
            }
        }
    }

    public enum Precision
    {
        LinearU8,
        NonLinearU8,
        PerceptualU8,
        LinearU16,
        NonLinearU16,
        PerceptualU16,
        LinearU32,
        NonLinearU32,
        PerceptualU32,
        LinearF16,
        NonLinearF16,
        PerceptualF16,
        LinearF32,
        NonLinearF32,
        PerceptualF32,
        LinearF64,
        NonLinearF64,
        PerceptualF64,
    }

    public static class PrecisionExtensions
    {
        public static Precision Parse(BinaryReader reader, Version version)
        {
            var value = reader.ReadUInt32BigEndian();
            if (version.Num == 4)
            {
                switch (value)
                {
                    case 0:
                        return Precision.NonLinearU8;
                    case 1:
                        return Precision.NonLinearU16;
                    case 2:
                        return Precision.LinearU32;
                    case 3:
                        return Precision.LinearF16;
                    case 4:
                        return Precision.LinearF32;
                    default:
                        throw new XcfParseException("Invalid precision");
                }
            }

            if (version.Num >= 5 && version.Num <= 6)
            {
                switch (value)
                {
                    case 100:
                        return Precision.LinearU8;
                    case 150:
                        return Precision.NonLinearU8;
                    case 200:
                        return Precision.LinearU16;
                    case 250:
                        return Precision.NonLinearU16;
                    case 300:
                        return Precision.LinearU32;
                    case 350:
                        return Precision.NonLinearU32;
                    case 400:
                        return Precision.LinearF16;
                    case 450:
                        return Precision.NonLinearF16;
                    case 500:
                        return Precision.LinearF32;
                    case 550:
                        return Precision.NonLinearF32;
                    default:
                        throw new XcfParseException("Invalid precision");
                }
            }

            if (version.Num >= 7)
            {
                switch (value)
                {
                    case 100:
                        return Precision.LinearU8;
                    case 150:
                        return Precision.NonLinearU8;
                    case 175:
                        return Precision.PerceptualU8;
                    case 200:
                        return Precision.LinearU16;
                    case 250:
                        return Precision.NonLinearU16;
                    case 275:
                        return Precision.PerceptualU16;
                    case 300:
                        return Precision.LinearU32;
                    case 350:
                        return Precision.NonLinearU32;
                    case 375:
                        return Precision.PerceptualU32;
                    case 500:
                        return Precision.LinearF16;
                    case 550:
                        return Precision.NonLinearF16;
                    case 575:
                        return Precision.PerceptualF16;
                    case 600:
                        return Precision.LinearF32;
                    case 650:
                        return Precision.NonLinearF32;
                    case 675:
                        return Precision.PerceptualF32;
                    case 700:
                        return Precision.LinearF64;
                    case 750:
                        return Precision.NonLinearF64;
                    case 775:
                        return Precision.PerceptualF64;
                    default:
                        throw new XcfParseException("Invalid precision");
                }
            }

            throw new XcfParseException("Invalid precision for this version");
        }
    }
}
