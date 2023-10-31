using Microsoft.Build.Locator;

namespace DotLurker;

class Program
{
    public static async Task Main(string[] args)
    {
        //string solutionPath = @"D:\VMbrowser\VMBrowser.Orca.Web\VMBrowser.Orca.Web\VMBrowser.Orca.Web.sln";
        var solutionPath = @"D:\RiderProjects\DotLurker\DotLurker\DotLurker.sln";
        var treeExtractor = new TreeExtractor();
        var dependencyGraph =
            await treeExtractor.GenerateDependencyGraph(@"C:\Program Files\dotnet\sdk\7.0.203", solutionPath);
    }
}