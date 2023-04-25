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

//Solution solution = await workspace.OpenSolutionAsync(solutionPath);
//var projects = solution.Projects;
            var projects = new[]
            {
                await workspace.OpenProjectAsync(
                    @"D:\RiderProjects\DotLurker\DotLurker.Sut\DotLurker.Sut\DotLurker.Sut.csproj")
            };

            var compilations = new List<Compilation>();
            foreach (var project in projects)
            {
                compilations.Add(await project.GetCompilationAsync());
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
                                 semanticModel.GetDeclaredSymbol(x).Name == "Program"))
                    {
                        var method = classDeclaration.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(x => semanticModel.GetDeclaredSymbol(x)?.Name == "Main");
                        if (method != null)
                        {
                            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                            bool NamespacePredicate(ISymbol s) => namespaces.Contains(s.ContainingNamespace.ToDisplayString());

                            bool LambdaPredicate(ISymbol s)
                            {
                                if (s.Kind == SymbolKind.Method)
                                {
                                    var sMethod = s as IMethodSymbol;
                                    if (sMethod.MethodKind is MethodKind.AnonymousFunction or MethodKind.LambdaMethod
                                        or MethodKind.DelegateInvoke) return true;
                                }

                                return false;
                            }

                            try
                            {
                                var usagesResolver = new UsagesResolver(inheritanceManager);
                                var usages = await usagesResolver.PopulateNode(methodSymbol,
                                    s => NamespacePredicate(s) || LambdaPredicate(s),
                                    compilation, new HashSet<int>());

                                Console.WriteLine("test");
                            }
                            catch (Exception e)
                            {
                                Console.Write(e);
                            }
                        }
                    }
                }
            }
        }
    }
}