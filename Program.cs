namespace DotLurker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var lurkerCore = new LurkerCore();
            var usageNode = await lurkerCore.GetUsageTreeFromSolution(@"C:\Program Files\dotnet\sdk\7.0.203",
                @"D:\VMbrowser\VMBrowser.Orca.Web\VMBrowser.Orca.Web\VMBrowser.Orca.Web.sln",
                "VMBrowser.Orca.Web",
                "Program",
                "Main"
            );
            Console.WriteLine(usageNode);
        }
    }
}