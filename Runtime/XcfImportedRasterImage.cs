using System;
using net.rs64.TexTransTool.MultiLayerImage;
using UnityEngine;

namespace moe.kyre.TTTXcf.Importer
{
    [Serializable]
    public class XcfImportedRasterImageData : net.rs64.TexTransTool.MultiLayerImage.LayerData.ImportRasterImageData
    {
        public int Width;
        public int Height;
        public int OffsetX;
        public int OffsetY;
        public byte[] RawRgba = Array.Empty<byte>();
    }

    public class XcfImportedRasterImage : TTTImportedImage
    {
        [SerializeField] public XcfImportedRasterImageData RasterImageData = null!;

        protected override void LoadImage(ITTImportedCanvasSource importSource, Span<byte> writeTarget)
        {
            if (RasterImageData == null) { return; }

            var layerWidth = RasterImageData.Width;
            var layerHeight = RasterImageData.Height;
            if (layerWidth == 0 || layerHeight == 0) { return; }

            var canvasWidth = CanvasDescription.Width;
            var canvasHeight = CanvasDescription.Height;

            var pivotX = RasterImageData.OffsetX;
            var pivotY = canvasHeight - (RasterImageData.OffsetY + layerHeight);

            var source = RasterImageData.RawRgba.AsSpan();

            for (var y = 0; y < layerHeight; y += 1)
            {
                var destY = pivotY + (layerHeight - 1 - y);
                if (destY < 0 || destY >= canvasHeight) { continue; }

                var startX = Math.Max(0, pivotX);
                var endX = Math.Min(canvasWidth, pivotX + layerWidth);
                if (endX <= startX) { continue; }

                var copyPixels = endX - startX;
                var sourceRowStart = (y * layerWidth + (startX - pivotX)) * 4;
                var destRowStart = (destY * canvasWidth + startX) * 4;

                source.Slice(sourceRowStart, copyPixels * 4).CopyTo(writeTarget.Slice(destRowStart, copyPixels * 4));
            }
        }
    }
}
