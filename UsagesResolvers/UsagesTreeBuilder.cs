using DotLurker.Models;
using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class UsagesTreeBuilder
{
    private readonly InheritanceManager _inheritanceManager;
    private readonly ISymbolReferencesResolver<IMethodSymbol> _containingSymbolsResolverForMethods;
    private readonly ISymbolReferencesResolver<IPropertySymbol> _containingSymbolsResolverForProperties;
    private readonly ISymbolReferencesResolver<IFieldSymbol> _containingSymbolsResolverForFields;

    public UsagesTreeBuilder(InheritanceManager inheritanceManager,
        ISymbolReferencesResolver<IMethodSymbol> containingSymbolsResolverForMethods,
        ISymbolReferencesResolver<IPropertySymbol> containingSymbolsResolverForProperties,
        ISymbolReferencesResolver<IFieldSymbol> containingSymbolsResolverForFields)
    {
        _inheritanceManager = inheritanceManager;
        _containingSymbolsResolverForMethods = containingSymbolsResolverForMethods;
        _containingSymbolsResolverForProperties = containingSymbolsResolverForProperties;
        _containingSymbolsResolverForFields = containingSymbolsResolverForFields;
    }

    private async Task<UsageNode> PopulateNodeFromInvocable(IPropertySymbol methodSymbol, ISet<int> treeSymbolsHashes)
    {
        // Get all derived types usages
        if (methodSymbol.IsAbstract)
        {
            var derivedTypesUsages =
                (await GetDerivedTypesUsages(methodSymbol, treeSymbolsHashes)).ToList();
            var usageNode = new UsageNode(methodSymbol)
            {
                DerivedUsages = derivedTypesUsages
            };
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
            return usageNode;
        }

        if (methodSymbol.ContainingType.TypeKind == TypeKind.Interface
            || methodSymbol.IsVirtual
            || methodSymbol.IsOverride)
        {
            var usages = new List<UsageNode>();

            foreach (var symbolInsideThisMethod in await
                         _containingSymbolsResolverForProperties.GetAllContainingSymbols(
                             methodSymbol))
            {
                usages.Add(await PopulateNode(symbolInsideThisMethod, treeSymbolsHashes));
            }

            var usageNode = new UsageNode(methodSymbol)
            {
                DerivedUsages =
                    (await GetDerivedTypesUsages(methodSymbol, treeSymbolsHashes))
                    .ToList(),
                Usages = usages
            };
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
            return usageNode;
        }

        {
            var usageNode = new UsageNode(methodSymbol);
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());

            foreach (var usageSymbol in
                     await _containingSymbolsResolverForProperties.GetAllContainingSymbols(methodSymbol))
            {
                var innerUsageNode =
                    await PopulateNode(usageSymbol, treeSymbolsHashes);
                usageNode.Usages.Add(innerUsageNode);
                treeSymbolsHashes.Add(usageNode.Usages.Last().Symbol.GetHashCode());
            }

            return usageNode;
        }
    }

    private async Task<UsageNode> PopulateNodeFromInvocable(IMethodSymbol methodSymbol, ISet<int> treeSymbolsHashes)
    {
        // Get all derived types usages
        if (methodSymbol.IsAbstract)
        {
            var derivedTypesUsages =
                (await GetDerivedTypesUsages(methodSymbol, treeSymbolsHashes)).ToList();
            var usageNode = new UsageNode(methodSymbol)
            {
                DerivedUsages = derivedTypesUsages
            };
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
            return usageNode;
        }

        if (methodSymbol.ContainingType.TypeKind == TypeKind.Interface
            || methodSymbol.IsVirtual
            || methodSymbol.IsOverride)
        {
            var usages = new List<UsageNode>();

            foreach (var symbolInsideThisMethod in await _containingSymbolsResolverForMethods.GetAllContainingSymbols(
                         methodSymbol))
            {
                usages.Add(await PopulateNode(symbolInsideThisMethod, treeSymbolsHashes));
            }

            var usageNode = new UsageNode(methodSymbol)
            {
                DerivedUsages =
                    (await GetDerivedTypesUsages(methodSymbol, treeSymbolsHashes))
                    .ToList(),
                Usages = usages
            };
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
            return usageNode;
        }

        {
            var usageNode = new UsageNode(methodSymbol);
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());

            foreach (var usageSymbol in
                     await _containingSymbolsResolverForMethods.GetAllContainingSymbols(methodSymbol))
            {
                var innerUsageNode =
                    await PopulateNode(usageSymbol, treeSymbolsHashes);
                usageNode.Usages.Add(innerUsageNode);
                treeSymbolsHashes.Add(usageNode.Usages.Last().Symbol.GetHashCode());
            }

            return usageNode;
        }
    }

    private async Task<UsageNode> PopulateNodeFromField(IFieldSymbol fieldSymbol, ISet<int> treeSymbolsHashes)
    {
        var usageNode = new UsageNode(fieldSymbol);

        foreach (var containingSymbol in await _containingSymbolsResolverForFields.GetAllContainingSymbols(fieldSymbol))
        {
            usageNode.Usages.Add(await PopulateNode(containingSymbol, treeSymbolsHashes));
            treeSymbolsHashes.Add(usageNode.Usages.Last().Symbol.GetHashCode());
        }

        return usageNode;
    }

    public async Task<UsageNode> PopulateNode(ISymbol symbol, ISet<int> treeSymbolsHashes)
    {
        if (treeSymbolsHashes.Contains(symbol.GetHashCode()))
            return new UsageNode(symbol);

        // DO NOT change order of if!

        if (symbol is IFieldSymbol fieldSymbol)
        {
            return await PopulateNodeFromField(fieldSymbol, treeSymbolsHashes);
        }

        if (symbol is IPropertySymbol propertySymbol)
        {
            return await PopulateNodeFromInvocable(propertySymbol, treeSymbolsHashes);
        }

        if (symbol is IMethodSymbol methodSymbol)
        {
            return await PopulateNodeFromInvocable(methodSymbol, treeSymbolsHashes);
        }

        // stub
        return new UsageNode(symbol);
    }

    private async Task<IReadOnlyCollection<UsageNode>> GetDerivedTypesUsages(ISymbol symbol,
        ISet<int> treeSymbolsHashes)
    {
        var derivedClasses = _inheritanceManager.GetAllDerivedClasses(symbol.ContainingType);

        var usageNodes = new List<UsageNode>();
        foreach (var derivedClass in derivedClasses)
        {
            // TODO check there
            var inheritedMember = derivedClass.GetMembers()
                .FirstOrDefault(x => x.MetadataName == symbol.MetadataName);
            if (inheritedMember != null)
            {
                var usageNode = await PopulateNode(inheritedMember, treeSymbolsHashes);
                treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
                usageNodes.Add(usageNode);
            }
        }

        return usageNodes;
    }
}