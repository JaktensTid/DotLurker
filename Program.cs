using System.Security.Cryptography;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;


enum DependencyType
{
    Default,
    Base
}


class Node
{
    public DependencyType DependencyType { get; set; }
    public string TypeName { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is Node node && node.DependencyType == DependencyType && node.TypeName == TypeName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DependencyType.GetHashCode(), TypeName.GetHashCode());
    }
}


class Program
{
    private static string GetFullTypeName(TypeDeclarationSyntax typeDeclaration)
    {
        SyntaxNode currentNode = typeDeclaration;
        List<string> typeNameParts = new List<string>();

        while (currentNode != null)
        {
            if (currentNode is TypeDeclarationSyntax typeNode)
            {
                typeNameParts.Insert(0, typeNode.Identifier.Text);
                currentNode = typeNode.Parent;
            }
            else if (currentNode is NamespaceDeclarationSyntax namespaceNode)
            {
                typeNameParts.Insert(0, namespaceNode.Name.ToString());
                currentNode = namespaceNode.Parent;
            }
            else if (currentNode is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceNode)
            {
                // File-scoped namespace does not have a name, so we can use a placeholder
                //typeNameParts.Insert(0, "<FileScopedNamespace>");
                currentNode = fileScopedNamespaceNode.Parent;
            }
            else
            {
                break;
            }
        }

        if (typeNameParts.Count > 0)
        {
            return string.Join(".", typeNameParts);
        }
        else
        {
            return string.Empty;
        }
    }

    public static string GetFilePathFromSymbolInfo(SymbolInfo symbolInfo)
    {
        var locations = symbolInfo.Symbol?.Locations;

        if (locations != null && locations.HasValue)
        {
            // Filter for source file locations
            var sourceLocations = locations.Value.Where(loc => loc.IsInSource);

            // Get the file path from the first source location
            var filePath = sourceLocations.FirstOrDefault()?.SourceTree?.FilePath;

            if (!string.IsNullOrEmpty(filePath))
            {
                return filePath;
            }
        }

        return string.Empty;
    }

    public static bool IsInterfaceOrClassOrRecordOrStructOrDelegate(ISymbol symbol)
    {
        return symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface or TypeKind.Class or TypeKind.Struct };
    }

    private static IEnumerable<SymbolInfo> GetSymbols(SyntaxNode usage, IEnumerable<SemanticModel> semanticModels)
    {
        return semanticModels.Select<SemanticModel, SymbolInfo?>(x =>
        {
            try
            {
                return x.GetSymbolInfo(usage);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }).Where(x => x != null).Select(x => x.Value);
    }

    private static IEnumerable<string> GetMemberDependencies(MemberDeclarationSyntax memberDeclaration,
        IEnumerable<SemanticModel> semanticModels)
    {
        List<string> dependencies = new List<string>();

        // Collect dependencies from fields
        if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
        {
            foreach (SyntaxNode usage in fieldDeclaration.DescendantNodes())
            {
                if (usage is IdentifierNameSyntax identifierName)
                {
                    var symbols = GetSymbols(usage, semanticModels);
                    foreach (var symbolInfo in symbols)
                    {
                        var path = GetFilePathFromSymbolInfo(symbolInfo);
                        if (!string.IsNullOrWhiteSpace(path) &&
                            IsInterfaceOrClassOrRecordOrStructOrDelegate(symbolInfo.Symbol))
                        {
                            string dependencyName = identifierName.Identifier.Text;
                            dependencies.Add(symbolInfo.Symbol.ToDisplayString());
                        }
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
                    var symbols = GetSymbols(usage, semanticModels);
                    foreach (var symbolInfo in symbols)
                    {
                        var path = GetFilePathFromSymbolInfo(symbolInfo);
                        if (!string.IsNullOrWhiteSpace(path) &&
                            IsInterfaceOrClassOrRecordOrStructOrDelegate(symbolInfo.Symbol))
                        {
                            dependencies.Add(symbolInfo.Symbol.ToDisplayString());
                        }
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
                    var symbols = GetSymbols(usage, semanticModels);
                    foreach (var symbolInfo in symbols)
                    {
                        var path = GetFilePathFromSymbolInfo(symbolInfo);
                        if (!string.IsNullOrWhiteSpace(path) &&
                            IsInterfaceOrClassOrRecordOrStructOrDelegate(symbolInfo.Symbol))
                        {
                            string dependencyName = identifierName.Identifier.Text;
                            dependencies.Add(symbolInfo.Symbol.ToDisplayString());
                        }
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
                    var symbols = GetSymbols(usage, semanticModels);
                    foreach (var symbolInfo in symbols)
                    {
                        var path = GetFilePathFromSymbolInfo(symbolInfo);
                        if (!string.IsNullOrWhiteSpace(path) &&
                            IsInterfaceOrClassOrRecordOrStructOrDelegate(symbolInfo.Symbol))
                        {
                            string dependencyName = identifierName.Identifier.Text;
                            dependencies.Add(symbolInfo.Symbol.ToDisplayString());
                        }
                    }
                }
            }
        }

        // Add more cases for other member types as needed

        return dependencies;
    }


    // DO NOT CHANGE!!
    public static Dictionary<string, HashSet<Node>> GenerateDependencyGraph(string projectPath)
    {
        // Load the project solution
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        Solution solution = workspace.OpenSolutionAsync(projectPath).Result;

        // Create a dictionary to store the dependency graph
        Dictionary<string, HashSet<Node>> dependencyGraph = new Dictionary<string, HashSet<Node>>();

        var documents = solution.Projects.SelectMany(p => p.Documents).ToList();
        var semanticModels = documents.Select(x => x.GetSemanticModelAsync().Result).ToList();

        // Traverse each project in the solution
        foreach (Project project in solution.Projects)
        {
            // Traverse each document in the project
            foreach (Document document in project.Documents)
            {
                // Parse the syntax tree of the document
                SyntaxTree syntaxTree = document.GetSyntaxTreeAsync().Result;
                SyntaxNode root = syntaxTree.GetRoot();

                // Traverse the syntax tree and find class, struct, record, and interface declarations
                IEnumerable<TypeDeclarationSyntax> typeDeclarations =
                    root.DescendantNodes().OfType<TypeDeclarationSyntax>();

                foreach (TypeDeclarationSyntax typeDeclaration in typeDeclarations)
                {
                    string typeName = typeDeclaration.Identifier.Text;
                    string typeKind = typeDeclaration.Kind().ToString();
                    string typeFullName = GetFullTypeName(typeDeclaration);

                    // Add the type to the dependency graph
                    if (!dependencyGraph.ContainsKey(typeName))
                        dependencyGraph[typeName] = new HashSet<Node>();

                    // Find and add base types
                    IEnumerable<IdentifierNameSyntax> dependencies = typeDeclaration.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(identifier => identifier.Parent is BaseTypeSyntax);

                    foreach (IdentifierNameSyntax dependency in dependencies)
                    {
                        var symbols = GetSymbols(dependency, semanticModels);
                        string dependencyName = dependency.Identifier.Text;
                        var node = new Node
                        {
                            DependencyType = DependencyType.Base,
                            TypeName = dependencyName
                        };

                        if (!dependencyGraph[typeName].Contains(node))
                            dependencyGraph[typeName].Add(node);
                    }
                }

                // bases
                foreach (TypeDeclarationSyntax typeDeclaration in typeDeclarations)
                {
                    string typeName = typeDeclaration.Identifier.Text;
                    string typeFullName = GetFullTypeName(typeDeclaration);

                    //if (!typeFullName.Contains("StaticClass")) 
                    //    continue; // TODO rude

                    // Add the type to the dependency graph
                    if (!dependencyGraph.ContainsKey(typeFullName))
                        dependencyGraph[typeFullName] = new HashSet<Node>();

                    // Find and add dependencies from the type's members
                    IEnumerable<MemberDeclarationSyntax> memberDeclarations = typeDeclaration.Members;

                    foreach (MemberDeclarationSyntax memberDeclaration in memberDeclarations)
                    {
                        IEnumerable<string> memberDependencies =
                            GetMemberDependencies(memberDeclaration, semanticModels);
                        foreach (var member in memberDependencies.Select(x => new Node
                                 {
                                     DependencyType = DependencyType.Default,
                                     TypeName = x
                                 }))
                        {
                            dependencyGraph[typeFullName].Add(member);
                        }
                    }
                }
            }
        }

        return dependencyGraph;
    }


    public static void Main(string[] args)
    {
        MSBuildLocator.RegisterMSBuildPath(@"C:\Program Files\dotnet\sdk\7.0.203");
        string projectPath = @"D:\RiderProjects\DotLurker\DotLurker\DotLurker.sln";
        var dependencyGraph = GenerateDependencyGraph(projectPath);
        //var dependencyGraph = GenerateDependencyGraph(projectPath);
        foreach (KeyValuePair<string, HashSet<Node>> entry in dependencyGraph)
        {
            string typeName = entry.Key;
            HashSet<Node> dependencies = entry.Value;

            Console.WriteLine($"Type: {typeName}");
            Console.WriteLine("Dependencies:");
            foreach (var dependency in dependencies)
            {
                Console.WriteLine(
                    $"Dep: {dependency.TypeName}, Type: {(dependency.DependencyType == DependencyType.Base ? "Base" : "Dependency")}");
            }

            Console.WriteLine();
        }
    }
}