using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class ClassDependencyWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;

    public ClassDependencyWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public IDictionary<string, HashSet<string>> Dependencies { get; } =
        new Dictionary<string, HashSet<string>>();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var className = node.Identifier.ValueText;
        if (!Dependencies.ContainsKey(className))
        {
            Dependencies[className] = new HashSet<string>();
        }

        base.VisitClassDeclaration(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node);
        var type = typeInfo.Type;
        if (type != null && !type.ContainingNamespace.ToDisplayString().StartsWith("System"))
        {
            var className = node.Identifier.ValueText;
            var parentClass = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (parentClass != null)
            {
                var parentClassName = parentClass.Identifier.ValueText;
                Dependencies[parentClassName].Add(className);
            }
        }

        base.VisitIdentifierName(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var interfaceName = node.Identifier.ValueText;
        if (!Dependencies.ContainsKey(interfaceName))
        {
            Dependencies[interfaceName] = new HashSet<string>();
        }

        foreach (var method in node.Members
                     .Where(x => x is MethodDeclarationSyntax)
                     .Cast<MethodDeclarationSyntax>())
        {
            // Check for default interface implementations
            if (method.Body != null || method.ExpressionBody != null)
            {
                var dependencies = ExtractDependenciesFromNode(method);
                foreach (var dependency in dependencies)
                {
                    Dependencies[interfaceName].Add(dependency);
                }
            }
        }

        base.VisitInterfaceDeclaration(node);
    }

    private IEnumerable<string> ExtractDependenciesFromNode(SyntaxNode node)
    {
        var identifiers = node.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>();

        foreach (var identifier in identifiers)
        {
            var typeInfo = _semanticModel.GetTypeInfo(identifier);
            var type = typeInfo.Type;
            if (type != null && !type.ContainingNamespace.ToDisplayString().StartsWith("System"))
            {
                yield return identifier.Identifier.ValueText;
            }
        }
    }
}

// class Program
// {
//     static void Main(string[] args)
//     {
//         MSBuildLocator.RegisterMSBuildPath(@"C:\Program Files\dotnet\sdk\7.0.203");
//         var solutionPath = @"D:\RiderProjects\DotLurker\DotLurker.Sut\DotLurker.Sut"; // Path to your solution file or project folder
//
//         var workspace = new AdhocWorkspace();
//         var solution = workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
//         var project = solution.AddProject("DotLurker.Sut", "DotLurker.Sut", LanguageNames.CSharp);
//
//         var filePaths = Directory.GetFiles(solutionPath, "*.cs", SearchOption.AllDirectories);
//         foreach (var filePath in filePaths)
//         {
//             var document = project.AddDocument(Path.GetFileName(filePath), File.ReadAllText(filePath));
//             project = document.Project;
//         }
//
//         solution = project.Solution;
//         var compilation = solution.GetProject(project.Id).GetCompilationAsync().Result;
//
//         var walker = new ClassDependencyWalker(compilation.GetSemanticModel(compilation.SyntaxTrees.First()));
//         foreach (var syntaxTree in compilation.SyntaxTrees)
//         {
//             walker.Visit(syntaxTree.GetRoot());
//         }
//
//         var dependencies = walker.Dependencies;
//         foreach (var classDependency in dependencies)
//         {
//             Console.WriteLine($"Class: {classDependency.Key}");
//             Console.WriteLine("Dependencies:");
//             foreach (var dependency in classDependency.Value)
//             {
//                 Console.WriteLine(dependency);
//             }
//             Console.WriteLine();
//         }
//     }
// }


class Program
{
    public static Dictionary<string, List<string>> GenerateDependencyGraph2(string projectPath)
    {
        // Load the project solution
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        Solution solution = workspace.OpenSolutionAsync(projectPath).Result;

        // Create a dictionary to store the dependency graph
        Dictionary<string, List<string>> dependencyGraph = new Dictionary<string, List<string>>();

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
                    string typeFullName = GetFullTypeName(typeDeclaration);

                    // Add the type to the dependency graph
                    if (!dependencyGraph.ContainsKey(typeFullName))
                        dependencyGraph[typeFullName] = new List<string>();

                    // Find and add dependencies from the type's members
                    IEnumerable<MemberDeclarationSyntax> memberDeclarations = typeDeclaration.Members;

                    foreach (MemberDeclarationSyntax memberDeclaration in memberDeclarations)
                    {
                        IEnumerable<string> memberDependencies = GetMemberDependencies(memberDeclaration);
                        dependencyGraph[typeFullName].AddRange(memberDependencies);
                    }
                }
            }
        }

        return dependencyGraph;
    }

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
                typeNameParts.Insert(0, "<FileScopedNamespace>");
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

    private static IEnumerable<string> GetMemberDependencies(MemberDeclarationSyntax memberDeclaration)
    {
        List<string> dependencies = new List<string>();

        // Collect dependencies from fields
        if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
        {
            foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
            {
                foreach (SyntaxNode usage in variable.DescendantNodes())
                {
                    if (usage is IdentifierNameSyntax identifierName)
                    {
                        string dependencyName = identifierName.Identifier.Text;
                        dependencies.Add(dependencyName);
                    }
                }
            }
        }

        // Collect dependencies from properties
        if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
        {
            foreach (SyntaxNode usage in propertyDeclaration.DescendantNodes())
            {
                if (usage is IdentifierNameSyntax identifierName)
                {
                    string dependencyName = identifierName.Identifier.Text;
                    dependencies.Add(dependencyName);
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
                    string dependencyName = identifierName.Identifier.Text;
                    dependencies.Add(dependencyName);
                }
            }
        }

        // Add more cases for other member types as needed

        return dependencies;
    }


    // DO NOT CHANGE!!
    public static Dictionary<string, List<string>> GenerateDependencyGraph(string projectPath)
    {
        // Load the project solution
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        Solution solution = workspace.OpenSolutionAsync(projectPath).Result;

        // Create a dictionary to store the dependency graph
        Dictionary<string, List<string>> dependencyGraph = new Dictionary<string, List<string>>();

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

                    // Add the type to the dependency graph
                    if (!dependencyGraph.ContainsKey(typeName))
                        dependencyGraph[typeName] = new List<string>();

                    // Find and add dependencies
                    IEnumerable<IdentifierNameSyntax> dependencies = typeDeclaration.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(identifier => identifier.Parent is BaseTypeSyntax);

                    foreach (IdentifierNameSyntax dependency in dependencies)
                    {
                        string dependencyName = dependency.Identifier.Text;
                        if (!dependencyGraph[typeName].Contains(dependencyName))
                            dependencyGraph[typeName].Add(dependencyName);
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
        var dependencyGraph = GenerateDependencyGraph2(projectPath);
        //var dependencyGraph = GenerateDependencyGraph(projectPath);
        foreach (KeyValuePair<string, List<string>> entry in dependencyGraph)
        {
            string typeName = entry.Key;
            List<string> dependencies = entry.Value;

            Console.WriteLine($"Type: {typeName}");
            Console.WriteLine("Dependencies:");
            foreach (string dependency in dependencies)
            {
                Console.WriteLine(dependency);
            }

            Console.WriteLine();
        }
    }
}


// class Program
// {
//     static void Main(string[] args)
//     {
//         MSBuildLocator.RegisterMSBuildPath(@"C:\Program Files\dotnet\sdk\7.0.203");
//         var workspace = MSBuildWorkspace.Create();
//         var project = workspace
//             .OpenProjectAsync(@"D:\RiderProjects\DotLurker\DotLurker.Sut\DotLurker.Sut\DotLurker.Sut.csproj").Result;
//         var dependencies = new Dictionary<string, HashSet<string>>();
//
//         foreach (var document in project.Documents)
//         {
//             var model = document.GetSemanticModelAsync().Result;
//             var tree = document.GetSyntaxTreeAsync().Result;
//             var root = tree.GetRoot();
//             var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
//
//             foreach (var @class in classes)
//             {
//                 var className = @class.Identifier.Text;
//                 if (!dependencies.ContainsKey(className))
//                     dependencies[className] = new HashSet<string>();
//
//                 var memberAccessExpressions = @class.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
//                 foreach (var memberAccessExpression in memberAccessExpressions)
//                 {
//                     var symbolInfo = model.GetSymbolInfo(memberAccessExpression);
//                     if (symbolInfo.Symbol?.ContainingType?.Name != null)
//                     {
//                         var accessedClass = symbolInfo.Symbol.ContainingType.Name;
//                         if (accessedClass != className)
//                             dependencies[className].Add(accessedClass);
//                     }
//                 }
//             }
//         }
//
//         foreach (var pair in dependencies)
//         {
//             Console.WriteLine($"{pair.Key} depends on: {string.Join(", ", pair.Value)}");
//         }
//     }
// }

// using DotLurker.Managers;
//
// namespace DotLurker
// {
//     public class Program
//     {
//         public static async Task Main()
//         {
//             var gitManager = new GitManager(@"D:\RiderProjects\DotLurker\DotLurker\.git");
//             var diff = gitManager.GetChanges();
//             
//             var lurkerCore = new LurkerCore();
//             var usageNode = await lurkerCore.GetUsageTreeFromSolution(@"C:\Program Files\dotnet\sdk\7.0.203",
//                 @"D:\VMbrowser\VMBrowser.Orca.Web\VMBrowser.Orca.Web\VMBrowser.Orca.Web.sln",
//                 "VMBrowser.Orca.Web",
//                 "Program",
//                 "Main"
//             );
//             Console.WriteLine(usageNode);
//         }
//     }
// }