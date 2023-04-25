using Microsoft.CodeAnalysis;

namespace DotLurker;

public class InheritanceManager
{
    private readonly HashSet<INamedTypeSymbol> _allTypes;

    private InheritanceManager(HashSet<INamedTypeSymbol> hashSet)
    {
        _allTypes = hashSet;
    }

    public static async Task<InheritanceManager> Create(params Compilation[] compilations)
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

    public INamedTypeSymbol GetBaseClass(INamedTypeSymbol type)
    {
        if (type.BaseType is { TypeKind: TypeKind.Class })
        {
            return GetBaseClass(type.BaseType);
        }

        return type;
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
    
    public IEnumerable<IMethodSymbol> GetAllOverridesOfBaseOf(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.IsOverride || methodSymbol.IsVirtual || methodSymbol.IsAbstract)
        {
            
        }
        
        yield break;
        
        if (methodSymbol is { IsVirtual: false, IsAbstract: false } &&
            methodSymbol.ContainingType.TypeKind != TypeKind.Interface)
            yield break;

        var overridenMethod = methodSymbol.OverriddenMethod;
        if (overridenMethod == null)
        {
            var inheritedTypes = GetAllDerivedClasses(methodSymbol.ContainingType);
            foreach (var inheritedType in inheritedTypes)
            {
                foreach (var member in inheritedType.GetMembers().Where(x => x.Name == methodSymbol.Name))
                {
                    yield return (IMethodSymbol)member;
                }
            }

            yield break;
        }

        foreach (var subOverride in GetAllOverridesOfBaseOf(overridenMethod))
        {
            yield return subOverride;
        }
    }
}