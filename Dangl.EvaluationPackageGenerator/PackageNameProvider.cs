namespace Dangl.EvaluationPackageGenerator
{
    public static class PackageNameProvider
    {
        public static string[] AvaPackageNames =>
            [
                "Dangl.AVA",
                "Dangl.AVA.Converter",
                "Dangl.AVA.Converter.Excel",
                "Dangl.AVA.IO",
                "Dangl.AVACloud.Client",
                "Dangl.AVACloud.Client.Shared",
                "Dangl.GAEB",
                "Dangl.Oenorm",
                "Dangl.REB",
                "Dangl.REB.Formulas",
                "Dangl.SIA",
                "Dangl.ProductData",
                "Dangl.XRechnung"
            ];

        public static string[] XRechnungPackageNames =>
            [
                "Dangl.AVA",
                "Dangl.AVA.IO",
                "Dangl.XRechnung",
                "Dangl.XRechnung.Rendering"
            ];
    }
}
