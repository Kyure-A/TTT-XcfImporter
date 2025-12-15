namespace moe.kyre.TTTXcf.Importer
{
    [ScriptedImporter(0, new string[] { "xcf" }, new string[] { "xcf" }, AllowCaching = true)]
    public class TexTransToolXcfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var clipBytes = File.ReadAllBytes(ctx.assetPath);

        }
    }
}
