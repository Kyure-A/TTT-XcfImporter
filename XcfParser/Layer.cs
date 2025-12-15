using System.Collections.Generic;
using System.IO;

namespace moe.kyre.TTTXcf.parser
{
    public class Layer
    {
        public Layer(uint width, uint height, LayerColorType kind, string name, List<Property> properties, PixelData pixels)
        {
            Width = width;
            Height = height;
            Kind = kind;
            Name = name;
            Properties = properties;
            Pixels = pixels;
        }

        public uint Width { get; }
        public uint Height { get; }
        public LayerColorType Kind { get; }
        public string Name { get; }
        public IReadOnlyList<Property> Properties { get; }
        public PixelData Pixels { get; }

        public static Layer Parse(BinaryReader reader, Version version)
        {
            var width = reader.ReadUInt32BigEndian();
            var height = reader.ReadUInt32BigEndian();
            var kind = LayerColorType.Parse(reader.ReadUInt32BigEndian());
            var name = GimpStringReader.Read(reader);
            var properties = Property.ParseList(reader);
            var hierarchyPointer = reader.ReadUIntBigEndian(version.BytesPerOffset);

            var currentPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(checked((long)hierarchyPointer), SeekOrigin.Begin);
            var pixels = PixelData.ParseHierarchy(reader, version);
            reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);

            return new Layer(width, height, kind, name, properties, pixels);
        }

        public RgbaPixel? Pixel(uint x, uint y) => Pixels.Pixel(x, y);

        public (uint Width, uint Height) Dimensions => (Width, Height);

        public RgbaPixel[] RawRgbaBuffer() => Pixels.Pixels;

        public byte[] RawSubRgbaBuffer(uint x, uint y, uint width, uint height) => Pixels.RawSubRgbaBuffer(x, y, width, height);
    }

    public readonly struct LayerColorType
    {
        public LayerColorType(ColorType kind, bool alpha)
        {
            Kind = kind;
            Alpha = alpha;
        }

        public ColorType Kind { get; }
        public bool Alpha { get; }

        public static LayerColorType Parse(uint identifier)
        {
            var kind = ColorTypeExtensions.Parse(identifier / 2);
            var alpha = identifier % 2 == 1;
            return new LayerColorType(kind, alpha);
        }
    }
}
