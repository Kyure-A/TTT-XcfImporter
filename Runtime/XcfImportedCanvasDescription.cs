using moe.kyre.TTTXcf.parser;
using net.rs64.TexTransCore;
using net.rs64.TexTransTool.MultiLayerImage;

namespace moe.kyre.TTTXcf.Importer
{
    public class XcfImportedCanvasDescription : TTTImportedCanvasDescription
    {
        public override TexTransCoreTextureFormat ImportedImageFormat => TexTransCoreTextureFormat.Byte;

        public override ITTImportedCanvasSource LoadCanvasSource(string path)
        {
            return new XcfCanvasSource(Xcf.Open(path));
        }
    }

    public class XcfCanvasSource : ITTImportedCanvasSource
    {
        public Xcf Xcf;

        public XcfCanvasSource(Xcf xcf)
        {
            Xcf = xcf;
        }
    }
}
