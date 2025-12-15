using System;
using System.IO;

namespace moe.kyre.TTTXcf.parser
{
    public class PixelData
    {
        public PixelData(uint width, uint height, RgbaPixel[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public uint Width { get; }
        public uint Height { get; }
        public RgbaPixel[] Pixels { get; }

        public static PixelData ParseHierarchy(BinaryReader reader, Version version)
        {
            var width = reader.ReadUInt32BigEndian();
            var height = reader.ReadUInt32BigEndian();
            var bpp = reader.ReadUInt32BigEndian();
            if (bpp != 3 && bpp != 4)
            {
                throw new NotSupportedException("Only RGB/RGBA supported");
            }

            var levelPointer = reader.ReadUIntBigEndian(version.BytesPerOffset);
            reader.BaseStream.Seek(checked((long)levelPointer), SeekOrigin.Begin);

            var levelWidth = reader.ReadUInt32BigEndian();
            var levelHeight = reader.ReadUInt32BigEndian();
            if (levelWidth != width || levelHeight != height)
            {
                throw new XcfParseException("Invalid level dimensions");
            }

            var pixels = new RgbaPixel[checked((int)(width * height))];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new RgbaPixel(0, 0, 0, 255);
            }

            var tilesX = (uint)Math.Ceiling(width / 64.0);
            var tilesY = (uint)Math.Ceiling(height / 64.0);
            for (uint ty = 0; ty < tilesY; ty++)
            {
                for (uint tx = 0; tx < tilesX; tx++)
                {
                    var tilePointer = reader.ReadUIntBigEndian(version.BytesPerOffset);
                    var nextPointerPos = reader.BaseStream.Position;
                    reader.BaseStream.Seek(checked((long)tilePointer), SeekOrigin.Begin);

                    var cursor = new TileCursor(width, height, bpp, tx, ty);
                    cursor.Feed(reader, pixels);

                    reader.BaseStream.Seek(nextPointerPos, SeekOrigin.Begin);
                }
            }

            return new PixelData(width, height, pixels);
        }

        public RgbaPixel? Pixel(uint x, uint y)
        {
            if (x >= Width || y >= Height)
            {
                return null;
            }

            return Pixels[(int)(y * Width + x)];
        }

        public byte[] RawSubRgbaBuffer(uint x, uint y, uint width, uint height)
        {
            var sub = new byte[checked((int)(width * height * 4))];
            var pos = 0;
            for (var yy = y; yy < y + height; yy++)
            {
                for (var xx = x; xx < x + width; xx++)
                {
                    if (yy >= Height || xx >= Width)
                    {
                        throw new IndexOutOfRangeException("Pixel access is out of bounds");
                    }

                    var pixel = Pixels[(int)(yy * Width + xx)];
                    sub[pos++] = pixel.R;
                    sub[pos++] = pixel.G;
                    sub[pos++] = pixel.B;
                    sub[pos++] = pixel.A;
                }
            }

            return sub;
        }
    }

    public class TileCursor
    {
        private readonly uint width;
        private readonly uint height;
        private readonly uint channels;
        private readonly uint x;
        private readonly uint y;
        private uint i;

        public TileCursor(uint width, uint height, uint channels, uint tx, uint ty)
        {
            this.width = width;
            this.height = height;
            this.channels = channels;
            x = tx * 64;
            y = ty * 64;
            i = 0;
        }

        public void Feed(BinaryReader reader, RgbaPixel[] pixels)
        {
            var tileWidth = Math.Min(x + 64, width) - x;
            var tileHeight = Math.Min(y + 64, height) - y;
            var baseOffset = y * width + x;
            var channel = 0u;

            while (channel < channels)
            {
                while (i < tileWidth * tileHeight)
                {
                    var determinant = reader.ReadByte();
                    if (determinant < 127)
                    {
                        var run = (uint)(determinant + 1);
                        var v = reader.ReadByte();
                        for (var idx = i; idx < i + run; idx++)
                        {
                            var index = (int)(baseOffset + (idx / tileWidth) * width + idx % tileWidth);
                            var pixel = pixels[index];
                            pixel.SetChannel((int)channel, v);
                            pixels[index] = pixel;
                        }
                        i += run;
                    }
                    else if (determinant == 127)
                    {
                        var run = reader.ReadUInt16BigEndian();
                        var v = reader.ReadByte();
                        for (var idx = i; idx < i + run; idx++)
                        {
                            var index = (int)(baseOffset + (idx / tileWidth) * width + idx % tileWidth);
                            var pixel = pixels[index];
                            pixel.SetChannel((int)channel, v);
                            pixels[index] = pixel;
                        }
                        i += run;
                    }
                    else if (determinant == 128)
                    {
                        var streamRun = reader.ReadUInt16BigEndian();
                        for (var idx = i; idx < i + streamRun; idx++)
                        {
                            var index = (int)(baseOffset + (idx / tileWidth) * width + idx % tileWidth);
                            var v = reader.ReadByte();
                            var pixel = pixels[index];
                            pixel.SetChannel((int)channel, v);
                            pixels[index] = pixel;
                        }
                        i += streamRun;
                    }
                    else
                    {
                        var streamRun = 256u - determinant;
                        for (var idx = i; idx < i + streamRun; idx++)
                        {
                            var index = (int)(baseOffset + (idx / tileWidth) * width + idx % tileWidth);
                            var v = reader.ReadByte();
                            var pixel = pixels[index];
                            pixel.SetChannel((int)channel, v);
                            pixels[index] = pixel;
                        }
                        i += streamRun;
                    }
                }

                i = 0;
                channel += 1;
            }
        }
    }
}
