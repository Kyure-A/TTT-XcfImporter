using System;
using System.Collections.Generic;
using System.IO;

namespace moe.kyre.TTTXcf.parser
{
    public class Property
    {
        public Property(uint rawKind, PropertyIdentifier kind, int length, PropertyPayload payload)
        {
            RawKind = rawKind;
            Kind = kind;
            Length = length;
            Payload = payload;
        }

        public uint RawKind { get; }
        public PropertyIdentifier Kind { get; }
        public int Length { get; }
        public PropertyPayload Payload { get; }

        public static Property Parse(BinaryReader reader)
        {
            var rawKind = reader.ReadUInt32BigEndian();
            var kind = PropertyIdentifierExtensions.FromRaw(rawKind);
            var length = checked((int)reader.ReadUInt32BigEndian());
            var payload = PropertyPayload.Parse(reader, kind, length);
            return new Property(rawKind, kind, length, payload);
        }

        public static List<Property> ParseList(BinaryReader reader)
        {
            var props = new List<Property>();
            while (true)
            {
                var prop = Parse(reader);
                if (prop.Kind == PropertyIdentifier.PropEnd)
                {
                    break;
                }

                props.Add(prop);
            }

            return props;
        }
    }

    public enum PropertyIdentifier : uint
    {
        PropEnd = 0,
        PropColormap = 1,
        PropActiveLayer = 2,
        PropOpacity = 6,
        PropMode = 7,
        PropVisible = 8,
        PropLinked = 9,
        PropLockAlpha = 10,
        PropApplyMask = 11,
        PropEditMask = 12,
        PropShowMask = 13,
        PropOffsets = 15,
        PropCompression = 17,
        TypeIdentification = 18,
        PropResolution = 19,
        PropTattoo = 20,
        PropParasites = 21,
        PropUnit = 22,
        PropPaths = 23,
        PropLockContent = 28,
        PropLockPosition = 32,
        PropFloatOpacity = 33,
        PropColorTag = 34,
        PropCompositeMode = 35,
        PropCompositeSpace = 36,
        PropBlendSpace = 37,
        Unknown = uint.MaxValue,
    }

    public static class PropertyIdentifierExtensions
    {
        public static PropertyIdentifier FromRaw(uint value)
        {
            return Enum.IsDefined(typeof(PropertyIdentifier), value)
                ? (PropertyIdentifier)value
                : PropertyIdentifier.Unknown;
        }
    }

    public abstract class PropertyPayload
    {
        public static PropertyPayload Parse(BinaryReader reader, PropertyIdentifier kind, int length)
        {
            switch (kind)
            {
                case PropertyIdentifier.PropEnd:
                    return new EndPropertyPayload();
                default:
                    return new UnknownPropertyPayload(reader.ReadBytes(length));
            }
        }
    }

    public sealed class EndPropertyPayload : PropertyPayload
    {
    }

    public sealed class UnknownPropertyPayload : PropertyPayload
    {
        public UnknownPropertyPayload(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}
