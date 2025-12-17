using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using moe.kyre.TTTXcf.parser;
using net.rs64.TexTransTool.MultiLayerImage;
using net.rs64.TexTransTool.MultiLayerImage.Importer;
using net.rs64.TexTransTool.MultiLayerImage.LayerData;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace moe.kyre.TTTXcf.Importer
{
    [ScriptedImporter(0, new string[] { "xcf" }, new string[] { "xcf" }, AllowCaching = true)]
    public class TexTransToolXcfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var xcf = Xcf.Open(ctx.assetPath);
            var prefabName = Path.GetFileName(ctx.assetPath) + "-XCF";
            var rootXcf = new GameObject(prefabName);
            var multiLayerImageCanvas = rootXcf.AddComponent<MultiLayerImageCanvas>();

            ctx.AddObjectToAsset("RootXCF", rootXcf);
            ctx.SetMainObject(rootXcf);

            var xcfCanvasDescription = ScriptableObject.CreateInstance<XcfImportedCanvasDescription>();
            xcfCanvasDescription.Width = checked((int)xcf.Width);
            xcfCanvasDescription.Height = checked((int)xcf.Height);
            xcfCanvasDescription.name = "XCFCanvasDescription";
            ctx.AddObjectToAsset(xcfCanvasDescription.name, xcfCanvasDescription);
            multiLayerImageCanvas.tttImportedCanvasDescription = xcfCanvasDescription;

            var mliImporter = new MultiLayerImageImporter(multiLayerImageCanvas, xcfCanvasDescription, ctx, CreateXcfImportedImage);
            var layers = ConvertToLayerData(xcf);
            mliImporter.AddLayers(layers);
            mliImporter.SaveSubAsset();
        }

        private List<AbstractLayerData> ConvertToLayerData(Xcf xcf)
        {
            var layers = new List<AbstractLayerData>();
            foreach (var layer in xcf.Layers)
            {
                var rasterData = new RasterLayerData
                {
                    LayerName = layer.Name,
                    TransparencyProtected = ReadFlag(layer, PropertyIdentifier.PropLockAlpha),
                    Visible = ReadVisible(layer),
                    Opacity = ReadOpacity(layer),
                    Clipping = false,
                    BlendTypeKey = ReadBlendType(layer),
                    RasterTexture = CreateRasterImageData(layer),
                };

                layers.Add(rasterData);
            }

            return layers;
        }

        private static XcfImportedRasterImageData CreateRasterImageData(Layer layer)
        {
            var (offsetX, offsetY) = ReadOffsets(layer);
            return new XcfImportedRasterImageData
            {
                Width = checked((int)layer.Width),
                Height = checked((int)layer.Height),
                OffsetX = offsetX,
                OffsetY = offsetY,
                RawRgba = layer.Pixels.RawSubRgbaBuffer(0, 0, layer.Width, layer.Height),
            };
        }

        private static bool ReadFlag(Layer layer, PropertyIdentifier identifier)
        {
            var value = ReadUIntProperty(layer, identifier);
            return value.HasValue && value.Value != 0;
        }

        private static bool ReadVisible(Layer layer)
        {
            var visible = ReadUIntProperty(layer, PropertyIdentifier.PropVisible);
            return visible.HasValue ? visible.Value != 0 : true;
        }

        private static float ReadOpacity(Layer layer)
        {
            var opacity = ReadUIntProperty(layer, PropertyIdentifier.PropOpacity);
            if (opacity.HasValue)
            {
                return Mathf.Clamp01(opacity.Value / 255f);
            }

            var floatOpacity = ReadFloatProperty(layer, PropertyIdentifier.PropFloatOpacity);
            return floatOpacity.HasValue ? Mathf.Clamp01(floatOpacity.Value) : 1f;
        }

        private static (int X, int Y) ReadOffsets(Layer layer)
        {
            var payload = GetPayload(layer, PropertyIdentifier.PropOffsets);
            if (payload != null && payload.Data.Length >= 8)
            {
                var span = payload.Data.AsSpan();
                var x = BinaryPrimitives.ReadInt32BigEndian(span);
                var y = BinaryPrimitives.ReadInt32BigEndian(span.Slice(4));
                return (x, y);
            }

            return (0, 0);
        }

        private static string ReadBlendType(Layer layer)
        {
            var modeValue = ReadUIntProperty(layer, PropertyIdentifier.PropMode);
            if (modeValue.HasValue)
            {
                return modeValue.Value switch
                {
                    0 => "Normal",
                    1 => "Dissolve",
                    3 => "Mul",
                    4 => "Screen",
                    5 => "Overlay",
                    6 => "Difference",
                    7 => "Addition",
                    8 => "Subtract",
                    9 => "DarkenOnly",
                    10 => "LightenOnly",
                    11 => "Hue",
                    12 => "Saturation",
                    13 => "Color",
                    14 => "Luminosity",
                    15 => "Divide",
                    16 => "ColorDodge",
                    17 => "ColorBurn",
                    18 => "HardLight",
                    19 => "SoftLight",
                    _ => "Normal",
                };
            }

            return "Normal";
        }

        private static UnknownPropertyPayload? GetPayload(Layer layer, PropertyIdentifier identifier)
        {
            return layer.Properties.FirstOrDefault(prop => prop.Kind == identifier)?.Payload as UnknownPropertyPayload;
        }

        private static uint? ReadUIntProperty(Layer layer, PropertyIdentifier identifier)
        {
            var payload = GetPayload(layer, identifier);
            if (payload == null || payload.Data.Length < 4)
            {
                return null;
            }

            return BinaryPrimitives.ReadUInt32BigEndian(payload.Data.AsSpan());
        }

        private static float? ReadFloatProperty(Layer layer, PropertyIdentifier identifier)
        {
            var payload = GetPayload(layer, identifier);
            if (payload == null || payload.Data.Length < 4)
            {
                return null;
            }

            var raw = BinaryPrimitives.ReadUInt32BigEndian(payload.Data.AsSpan());
            return BitConverter.Int32BitsToSingle((int)raw);
        }

        private TTTImportedImage? CreateXcfImportedImage(ImportRasterImageData importRasterImage)
        {
            switch (importRasterImage)
            {
                case XcfImportedRasterImageData rasterImageData:
                    {
                        var importedImage = ScriptableObject.CreateInstance<XcfImportedRasterImage>();
                        importedImage.RasterImageData = rasterImageData;
                        return importedImage;
                    }
                default:
                    return null;
            }
        }
    }
}
