using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace moe.kyre.TTTXcf.parser
{
    public class Xcf
    {
        public XcfHeader Header { get; }
        private readonly List<Layer> layers;
        public IReadOnlyList<Layer> Layers => layers;

        private Xcf(XcfHeader header, List<Layer> layers)
        {
            Header = header;
            this.layers = layers;
        }

        public static Xcf Open(string path)
        {
            using var stream = File.OpenRead(path);
            return Load(stream);
        }

        public static Xcf Load(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var header = XcfHeader.Parse(reader);

            var layers = new List<Layer>();
            while (true)
            {
                var layerPointer = reader.ReadUIntBigEndian(header.Version.BytesPerOffset);
                if (layerPointer == 0)
                {
                    break;
                }

                var currentPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(checked((long)layerPointer), SeekOrigin.Begin);
                layers.Add(Layer.Parse(reader, header.Version));
                reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);
            }

            return new Xcf(header, layers);
        }

        public uint Width => Header.Width;
        public uint Height => Header.Height;
        public (uint Width, uint Height) Dimensions => (Width, Height);

        public Layer? Layer(string name)
        {
            foreach (var layer in layers)
            {
                if (layer.Name == name)
                {
                    return layer;
                }
            }

            return null;
        }
    }

    public class XcfHeader
    {
        public Version Version { get; }
        public uint Width { get; }
        public uint Height { get; }
        public ColorType ColorType { get; }
        public Precision Precision { get; }
        public IReadOnlyList<Property> Properties { get; }

        private XcfHeader(Version version, uint width, uint height, ColorType colorType, Precision precision, List<Property> properties)
        {
            Version = version;
            Width = width;
            Height = height;
            ColorType = colorType;
            Precision = precision;
            Properties = properties;
        }

        public static XcfHeader Parse(BinaryReader reader)
        {
            var magic = reader.ReadBytes(9);
            if (magic.Length != 9 || Encoding.ASCII.GetString(magic) != "gimp xcf ")
            {
                throw new XcfParseException("Invalid XCF header magic");
            }

            var version = Version.Parse(reader);
            if (version.Num > 11)
            {
                throw new XcfParseException("Unknown XCF version");
            }

            reader.ReadByte();

            var width = reader.ReadUInt32BigEndian();
            var height = reader.ReadUInt32BigEndian();

            var colorType = ColorTypeExtensions.Parse(reader.ReadUInt32BigEndian());
            if (colorType != ColorType.Rgb)
            {
                throw new NotSupportedException("Only RGB/RGBA color images supported");
            }

            var precision = version.Num >= 4
                ? PrecisionExtensions.Parse(reader, version)
                : Precision.NonLinearU8;

            var properties = Property.ParseList(reader);

            return new XcfHeader(version, width, height, colorType, precision, properties);
        }
    }

    public class XcfParseException : Exception
    {
        public XcfParseException(string message) : base(message)
        {
        }
    }
}
