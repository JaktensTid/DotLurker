using Microsoft.CodeAnalysis;

namespace DotLurker.Managers;

public static class NamespaceManager
{
    public static async Task<HashSet<string>> GetAllNamespacesFromProject(this Project project)
    {
        var uniqueNamespaces = new HashSet<string>();

        // Get the compilation for the project
        var compilation = await project.GetCompilationAsync();

        // Iterate over all the namespaces in the compilation
        GetAllNamespaces(compilation.Assembly.GlobalNamespace, uniqueNamespaces);

        return uniqueNamespaces;
    }

    static void GetAllNamespaces(INamespaceSymbol namespaceSymbol, HashSet<string> uniqueNamespaces)
    {
        if (namespaceSymbol is { IsGlobalNamespace: false })
        {
            uniqueNamespaces.Add(namespaceSymbol.ToDisplayString());
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            GetAllNamespaces(childNamespace, uniqueNamespaces);
        }
    }
}