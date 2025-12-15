using System;
using System.IO;
using System.Text;

namespace moe.kyre.TTTXcf.parser
{
    internal static class BinaryReaderExtensions
    {
        public static ushort ReadUInt16BigEndian(this BinaryReader reader)
        {
            var buffer = reader.ReadBytes(2);
            if (buffer.Length != 2)
            {
                throw new EndOfStreamException();
            }

            return (ushort)((buffer[0] << 8) | buffer[1]);
        }

        public static uint ReadUInt32BigEndian(this BinaryReader reader)
        {
            var buffer = reader.ReadBytes(4);
            if (buffer.Length != 4)
            {
                throw new EndOfStreamException();
            }

            return (uint)(buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3]);
        }

        public static ulong ReadUIntBigEndian(this BinaryReader reader, int byteCount)
        {
            switch (byteCount)
            {
                case 4:
                    return reader.ReadUInt32BigEndian();
                case 8:
                    return reader.ReadUInt64BigEndian();
                default:
                    throw new ArgumentOutOfRangeException(nameof(byteCount), "Offsets must be 4 or 8 bytes");
            }
        }

        public static ulong ReadUInt64BigEndian(this BinaryReader reader)
        {
            var buffer = reader.ReadBytes(8);
            if (buffer.Length != 8)
            {
                throw new EndOfStreamException();
            }

            return ((ulong)buffer[0] << 56) |
                   ((ulong)buffer[1] << 48) |
                   ((ulong)buffer[2] << 40) |
                   ((ulong)buffer[3] << 32) |
                   ((ulong)buffer[4] << 24) |
                   ((ulong)buffer[5] << 16) |
                   ((ulong)buffer[6] << 8) |
                   buffer[7];
        }
    }

    internal static class GimpStringReader
    {
        public static string Read(BinaryReader reader)
        {
            var length = reader.ReadUInt32BigEndian();
            if (length == 0)
            {
                return string.Empty;
            }

            var buffer = reader.ReadBytes(checked((int)length - 1));
            reader.ReadByte();
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
