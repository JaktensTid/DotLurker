using System.ComponentModel;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;

namespace DotLurker;

public class TreeExtractor
{
    private readonly HashSet<string> _msPathRegistrations = new HashSet<string>();
    
    // TODO to remove
    private Dictionary<Node, HashSet<Node>>? _cache;

    public (bool InSource, string FilePath) IsInSource(SymbolInfo symbolInfo)
    {
        var locations = symbolInfo.Symbol?.Locations;

        if (locations != null && locations.HasValue)
        {
            if (!symbolInfo.Symbol.ToDisplayString().StartsWith("DotLurker")) // TODO
                return (false, null);

            // Filter for source file locations
            var sourceLocations = locations.Value.Where(loc => loc.IsInSource);

            // Get the file path from the first source location
            var filePath = sourceLocations.FirstOrDefault()?.SourceTree?.FilePath;

            if (!string.IsNullOrEmpty(filePath))
            {
                return (true, filePath);
            }
        }

        return (false, null);
    }

    private (string Name, string NameSpace, string FilePath, NodeType NodeType, bool HasNames)
        GetSymbolInformationFromSyntax(
            TypeDeclarationSyntax typeDeclarationSyntax, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetDeclaredSymbol(typeDeclarationSyntax);
        var location = symbol.Locations.FirstOrDefault(x => x.IsInSource)?.SourceTree?.FilePath;
        var @namespace = symbol.ContainingNamespace.ToString();
        var type = (symbol as ITypeSymbol).TypeKind.ToNodeType();

        return (symbol.Name, @namespace, location, type, true);
    }

    private (string Name, string Namespace, string FilePath, NodeType NodeType, bool HasNames)
        GetSymbolInformationFromSyntax(
            SyntaxNode usage, IEnumerable<SemanticModel> semanticModels)
    {
        var symbols = semanticModels.Select<SemanticModel, SymbolInfo?>(x =>
        {
            try
            {
                return x.GetSymbolInfo(usage);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }).Where(x => x.HasValue).Select(x => x.Value);
        foreach (var symbolInfo in symbols)
        {
            var (inSource, filePath) = IsInSource(symbolInfo);
            if (inSource && symbolInfo.Symbol is INamedTypeSymbol
                {
                    TypeKind: TypeKind.Interface or TypeKind.Class or TypeKind.Struct or TypeKind.Enum
                })
            {
                var type = (symbolInfo.Symbol as INamedTypeSymbol).TypeKind.ToNodeType();
                return (symbolInfo.Symbol.Name, symbolInfo.Symbol.ContainingNamespace.ToString(), filePath, type, true);
            }
        }

        return (null, null, null, NodeType.Class, false);
    }

    private IEnumerable<Node> GetMemberDependencies(MemberDeclarationSyntax memberDeclaration,
        IEnumerable<SemanticModel> semanticModels)
    {
        var dependencies = new List<Node>();

        // Collect dependencies from fields
        if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
        {
            foreach (SyntaxNode usage in fieldDeclaration.DescendantNodes())
            {
                if (usage is IdentifierNameSyntax)
                {
                    var (name, @namespace, filePath, type, hasNames) =
                        GetSymbolInformationFromSyntax(usage, semanticModels);
                    if (hasNames)
                    {
                        dependencies.Add(new Node(type, name, @namespace, filePath, DependencyType.Default));
                    }
                }
            }
        }

        // Collect dependencies from properties
        if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
        {
            foreach (SyntaxNode usage in propertyDeclaration.DescendantNodes())
            {
                if (usage is IdentifierNameSyntax)
                {
                    var (name, @namespace, filePath, type, hasNames) =
                        GetSymbolInformationFromSyntax(usage, semanticModels);
                    if (hasNames)
                    {
                        dependencies.Add(new Node(type, name, @namespace, filePath, DependencyType.Default));
                    }
                }
            }
        }


        // Collect dependencies from methods
        if (memberDeclaration is MethodDeclarationSyntax methodDeclaration)
        {
            foreach (SyntaxNode usage in methodDeclaration.DescendantNodes())
            {
                if (usage is IdentifierNameSyntax identifierName)
                {
                    var (name, @namespace, filePath, type, hasNames) =
                        GetSymbolInformationFromSyntax(usage, semanticModels);
                    if (hasNames)
                    {
                        dependencies.Add(new Node(type, name, @namespace, filePath, DependencyType.Default));
                    }
                }
            }
        }

        // Collect dependencies from methods
        if (memberDeclaration is ConstructorDeclarationSyntax constructorDeclarationSyntax)
        {
            foreach (SyntaxNode usage in constructorDeclarationSyntax.DescendantNodes())
            {
                if (usage is IdentifierNameSyntax identifierName)
                {
                    var (name, @namespace, filePath, type, hasNames) =
                        GetSymbolInformationFromSyntax(usage, semanticModels);
                    if (hasNames)
                    {
                        dependencies.Add(new Node(type, name, @namespace, filePath, DependencyType.Default));
                    }
                }
            }
        }

        // Add more cases for other member types as needed

        return dependencies;
    }


    public async Task<Dictionary<Node, HashSet<Node>>> GenerateDependencyGraph(string msBuildPath, string projectPath)
    {
        if (_cache != null)
            return _cache;
        
        if (!_msPathRegistrations.Contains(msBuildPath))
        {
            MSBuildLocator.RegisterMSBuildPath(msBuildPath);
            _msPathRegistrations.Add(msBuildPath);
        }
        
        // Load the project solution
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        Solution solution = await workspace.OpenSolutionAsync(projectPath);

        // Create a dictionary to store the dependency graph
        Dictionary<Node, HashSet<Node>> dependencyGraph = new Dictionary<Node, HashSet<Node>>();

        var documents = solution.Projects.SelectMany(p => p.Documents).ToList();
        var semanticModels = new List<SemanticModel?>();
        foreach (var document in documents)
        {
            semanticModels.Add(await document.GetSemanticModelAsync());
        }

        var assemblies = AssembliesExtractor.ExtractAssemblies(solution.Projects);

        // Traverse each project in the solution
        foreach (var project in solution.Projects)
        {
            // Traverse each document in the project
            foreach (var document in project.Documents)
            {
                // Parse the syntax tree of the document
                SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
                SyntaxNode root = await syntaxTree.GetRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();

                // Traverse the syntax tree and find class, struct, record, and interface declarations
                IEnumerable<TypeDeclarationSyntax> typeDeclarations =
                    root.DescendantNodes().OfType<TypeDeclarationSyntax>();

                // Usages
                foreach (TypeDeclarationSyntax typeDeclaration in typeDeclarations)
                {
                    var (typeName, @namespace, filePath, type, hasNames) =
                        GetSymbolInformationFromSyntax(typeDeclaration, semanticModel);

                    var node = new Node(type, typeName, @namespace, filePath, DependencyType.Root);
                    // Add the type to the dependency graph
                    if (!dependencyGraph.ContainsKey(node))
                        dependencyGraph[node] = new HashSet<Node>();

                    // Find and add base types
                    IEnumerable<IdentifierNameSyntax> dependencies = typeDeclaration.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(identifier => identifier.Parent is BaseTypeSyntax);

                    foreach (IdentifierNameSyntax dependency in dependencies)
                    {
                        var dependencyData =
                            GetSymbolInformationFromSyntax(dependency, semanticModels);
                        if (dependencyData.HasNames)
                        {
                            var dependencyNode = new Node(
                                dependencyData.NodeType,
                                dependencyData.Name,
                                dependencyData.Namespace,
                                dependencyData.FilePath,
                                DependencyType.Base);

                            if (!dependencyGraph[node].Contains(dependencyNode))
                            {
                                dependencyGraph[node].Add(dependencyNode);
                            }

                            if (!dependencyGraph.ContainsKey(dependencyNode))
                                dependencyGraph[dependencyNode] = new HashSet<Node>();
                        }
                    }
                }

                // Inheritance
                foreach (TypeDeclarationSyntax typeDeclaration in typeDeclarations)
                {
                    var (typeName, @namespace, filePath, type, hasNames) =
                        GetSymbolInformationFromSyntax(typeDeclaration, semanticModel);

                    var node = new Node(type, typeName, @namespace, filePath, DependencyType.Root);
                    // Add the type to the dependency graph
                    if (!dependencyGraph.ContainsKey(node))
                        dependencyGraph[node] = new HashSet<Node>();

                    // Find and add dependencies from the type's members
                    IEnumerable<MemberDeclarationSyntax> memberDeclarations = typeDeclaration.Members;

                    foreach (MemberDeclarationSyntax memberDeclaration in memberDeclarations)
                    {
                        IEnumerable<Node> memberDependencies =
                            GetMemberDependencies(memberDeclaration, semanticModels);
                        foreach (var baseNode in memberDependencies)
                        {
                            dependencyGraph[node].Add(baseNode);
                            if (!dependencyGraph.ContainsKey(baseNode))
                                dependencyGraph[baseNode] = new HashSet<Node>();
                        }
                    }
                }
            }
        }

        _cache = dependencyGraph;
        return dependencyGraph;
    }
}