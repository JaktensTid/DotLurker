using DotLurker.UsagesResolvers;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotLurker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances();
            //var instance = instances.FirstOrDefault(x => x.Version.Major == 6);
            //MSBuildLocator.RegisterInstance(instance);
            MSBuildLocator.RegisterMSBuildPath(@"C:\Program Files\dotnet\sdk\7.0.203");
            using var workspace = MSBuildWorkspace.Create();

            Solution solution =
                await workspace.OpenSolutionAsync(
                    @"D:\VMbrowser\VMBrowser.Orca.Web\VMBrowser.Orca.Web\VMBrowser.Orca.Web.sln");
            var projects = solution.Projects;
            //var projects = new[]
            //{
            //    await workspace.OpenProjectAsync(
            //        @"D:\RiderProjects\DotLurker\DotLurker.Sut\DotLurker.Sut\DotLurker.Sut.csproj")
            //};

            var compilationsDictionary = new Dictionary<string, Compilation>();
            var compilations = new List<Compilation>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync();
                compilations.Add(compilation);
                compilationsDictionary.Add(compilation.AssemblyName, compilation);
            }

            var inheritanceManager = await InheritanceManager.Create(compilations.ToArray());

            foreach (var project in projects)
            {
                var namespaces = await project.GetAllNamespacesFromProject();
                Compilation compilation = await project.GetCompilationAsync();
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                    var classDeclarations = (await syntaxTree.GetRootAsync())
                        .DescendantNodes()
                        .OfType<ClassDeclarationSyntax>();

                    foreach (var classDeclaration in classDeclarations.Where(x =>
                                 semanticModel.GetDeclaredSymbol(x).Name == "SymLogicHandler"))
                    {
                        var method = classDeclaration.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(x => semanticModel.GetDeclaredSymbol(x)?.Name == "Creating");
                        if (method != null)
                        {
                            var methodSymbol = semanticModel.GetDeclaredSymbol(method);

                            var usagesResolver = new UsagesTreeBuilder(inheritanceManager,
                                new MethodContainingSymbolsResolver(compilationsDictionary));
                            var usages = await usagesResolver.PopulateNode(methodSymbol, new HashSet<int>());

                            Console.WriteLine("test");
                        }
                    }
                }
            }
        }
    }
}