using DotLurker.Managers;
using DotLurker.Models;
using DotLurker.UsagesResolvers;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotLurker;

public class LurkerCore
{
    public async Task<SymbolDetail> GetUsageTreeFromSolution(
        string msBuildPath,
        string solutionPath,
        string fromAssemblyName,
        string fromClass,
        string fromMember
    )
    {
        MSBuildLocator.RegisterMSBuildPath(msBuildPath);
        using var workspace = MSBuildWorkspace.Create();

        Solution solution =
            await workspace.OpenSolutionAsync(
                solutionPath);
        var projects = solution.Projects;
        return await GetUsageTreeFromProjects(projects.ToList(), fromAssemblyName, fromClass, fromMember);
    }

    public async Task<SymbolDetail> GetUsageTreeFromProject(
        string msBuildPath,
        string projectPath,
        string fromAssemblyName,
        string fromClass,
        string fromMember
    )
    {
        MSBuildLocator.RegisterMSBuildPath(msBuildPath);
        using var workspace = MSBuildWorkspace.Create();

        var projects = new[]
        {
            await workspace.OpenProjectAsync(projectPath)
        };

        return await GetUsageTreeFromProjects(projects, fromAssemblyName, fromClass, fromMember);
    }

    private async Task<SymbolDetail> GetUsageTreeFromProjects(
        IReadOnlyCollection<Project> projects,
        string fromAssemblyName,
        string fromClass,
        string fromMember)
    {
        var compilationsDictionary = new Dictionary<string, Compilation>();
        var compilations = new List<Compilation>();
        foreach (var project in projects)
        {
            var compilation = await project.GetCompilationAsync();
            compilations.Add(compilation);
            compilationsDictionary.Add(compilation.AssemblyName, compilation);
        }

        var inheritanceManager = InheritanceManager.Create(compilations.ToArray());

        var namespaces = new HashSet<string>();
        foreach (var projectWithNamespace in projects)
        {
            namespaces.UnionWith(await projectWithNamespace.GetAllNamespacesFromProject());
        }

        foreach (var project in projects)
        {
            var usagesResolver = new SymbolsDetailsTreeBuilder(inheritanceManager,
                new InvocableSymbolUsedSymbolsResolver(compilationsDictionary),
                new InvocableSymbolUsedSymbolsResolver(compilationsDictionary),
                new FieldSymbolUsedSymbolsResolver(projects),
                new TypeSymbolUsedSymbolsResolver(compilationsDictionary));
            var compilation = await project.GetCompilationAsync();
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                var classDeclarations = (await syntaxTree.GetRootAsync())
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classDeclarations.Where(x =>
                         {
                             var symbol = semanticModel.GetDeclaredSymbol(x);
                             return symbol.Name == fromClass && symbol.ContainingAssembly.Name == fromAssemblyName;
                         }))
                {
                    var method = classDeclaration.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(x => semanticModel.GetDeclaredSymbol(x)?.Name == fromMember);
                    if (method != null)
                    {
                        var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                        var usageNode = await usagesResolver.PopulateNode(methodSymbol, new HashSet<int>());
                        return CleanupNode(usageNode, namespaces);
                    }

                    var property = classDeclaration.DescendantNodes()
                        .OfType<PropertyDeclarationSyntax>()
                        .FirstOrDefault(x => semanticModel.GetDeclaredSymbol(x)?.Name == fromMember);
                    if (property != null)
                    {
                        var propertySymbol = semanticModel.GetDeclaredSymbol(property);
                        var usageNode = await usagesResolver.PopulateNode(propertySymbol, new HashSet<int>());
                        return CleanupNode(usageNode, namespaces);
                    }

                    var field = classDeclaration.DescendantNodes()
                        .OfType<FieldDeclarationSyntax>()
                        .FirstOrDefault(x => semanticModel.GetDeclaredSymbol(x)?.Name == fromMember);
                    if (field != null)
                    {
                        var fieldSymbol = semanticModel.GetDeclaredSymbol(field);
                        var usageNode = await usagesResolver.PopulateNode(fieldSymbol, new HashSet<int>());
                        return CleanupNode(usageNode, namespaces);
                    }

                    throw new InvalidOperationException("Type of member is not supported yet");
                }
            }
        }

        throw new InvalidOperationException("Cannot find such member");
    }

    private SymbolDetail CleanupNode(SymbolDetail usageNode, HashSet<string> namespaces)
    {
        if (!ContainsProjectReference(usageNode, namespaces))
            return null;

        var newUsages = new HashSet<SymbolDetail>();

        foreach (var innerUsageNode in usageNode.SymbolsInside)
        {
            if (ContainsProjectReference(innerUsageNode, namespaces))
            {
                var cleanedUpNode = CleanupNode(innerUsageNode, namespaces);
                if (cleanedUpNode != null)
                    newUsages.Add(cleanedUpNode);
            }
        }

        usageNode.SymbolsInside = newUsages.ToList();

        var newDerivedUsages = new HashSet<SymbolDetail>();

        foreach (var innerUsageNode in usageNode.SymbolsInsideDerived)
        {
            if (ContainsProjectReference(innerUsageNode, namespaces))
            {
                var cleanedUpNode = CleanupNode(innerUsageNode, namespaces);
                if (cleanedUpNode != null)
                    newUsages.Add(cleanedUpNode);
            }
        }

        usageNode.SymbolsInsideDerived = newDerivedUsages.ToList();

        return usageNode;
    }

    private bool ContainsProjectReference(SymbolDetail usageNode, HashSet<string> namespaces)
    {
        if (!usageNode.Symbol.IsNamespace())
        {
            if (
                usageNode.Symbol.ContainingNamespace == null ||
                !namespaces.Contains(usageNode.Symbol.ContainingNamespace.ToDisplayString()) ||
                TypeManager.GetDeclaringUnderlyingType(usageNode.Symbol)?.ContainingNamespace == null)
                return false;
        }

        return true;
    }
}