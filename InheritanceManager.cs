using Microsoft.CodeAnalysis;

namespace DotLurker;

public class InheritanceManager
{
    private readonly HashSet<INamedTypeSymbol> _allTypes;

    private InheritanceManager(HashSet<INamedTypeSymbol> hashSet)
    {
        _allTypes = hashSet;
    }

    public static InheritanceManager Create(params Compilation[] compilations)
    {
        var allTypes = new HashSet<INamedTypeSymbol>();
        foreach (var compilation in compilations)
        {
            GetAllTypesRecursively(compilation.GlobalNamespace, allTypes);
        }

        return new InheritanceManager(allTypes);
    }

    private static void GetAllTypesRecursively(INamespaceSymbol namespaceSymbol, ISet<INamedTypeSymbol> allTypes)
    {
        foreach (var namedTypeSymbol in namespaceSymbol.GetTypeMembers())
        {
            allTypes.Add(namedTypeSymbol);
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            GetAllTypesRecursively(nestedNamespace, allTypes);
        }
    }

    public IReadOnlyCollection<INamedTypeSymbol> GetAllDerivedClasses(INamedTypeSymbol baseType)
    {
        var derivedClasses = new List<INamedTypeSymbol>();
        foreach (var type in _allTypes)
        {
            if (type.BaseType != null &&
                SymbolEqualityComparer.Default.Equals(type.BaseType, baseType) || type.Interfaces.Contains(baseType))
            {
                derivedClasses.Add(type);
                derivedClasses.AddRange(GetAllDerivedClasses(type));
            }
        }

        return derivedClasses;
    }
}