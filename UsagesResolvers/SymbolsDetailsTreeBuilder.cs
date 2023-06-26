using DotLurker.Managers;
using DotLurker.Models;
using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class SymbolsDetailsTreeBuilder
{
    private readonly InheritanceManager _inheritanceManager;
    private readonly IUsedSymbolsResolver<IMethodSymbol> _containingSymbolsResolverForMethods;
    private readonly IUsedSymbolsResolver<IPropertySymbol> _containingSymbolsResolverForProperties;
    private readonly IUsedSymbolsResolver<IFieldSymbol> _containingSymbolsResolverForFields;
    private readonly IUsedSymbolsResolver<ITypeSymbol> _containingSymbolsResolverForTypes;

    public SymbolsDetailsTreeBuilder(InheritanceManager inheritanceManager,
        IUsedSymbolsResolver<IMethodSymbol> containingSymbolsResolverForMethods,
        IUsedSymbolsResolver<IPropertySymbol> containingSymbolsResolverForProperties,
        IUsedSymbolsResolver<IFieldSymbol> containingSymbolsResolverForFields, IUsedSymbolsResolver<ITypeSymbol> containingSymbolsResolverForTypes)
    {
        _inheritanceManager = inheritanceManager;
        _containingSymbolsResolverForMethods = containingSymbolsResolverForMethods;
        _containingSymbolsResolverForProperties = containingSymbolsResolverForProperties;
        _containingSymbolsResolverForFields = containingSymbolsResolverForFields;
        _containingSymbolsResolverForTypes = containingSymbolsResolverForTypes;
    }

    private async Task<SymbolDetail> PopulateNodeFromInvokable<T>(
        T invokableSymbol,
        IUsedSymbolsResolver<T> usedSymbolsResolver,
        ISet<int> treeSymbolsHashes) where T : ISymbol
    {
        // Get all derived types usages
        if (invokableSymbol.IsAbstract)
        {
            var derivedTypesUsages =
                (await GetDerivedTypesUsages(invokableSymbol, treeSymbolsHashes)).ToList();
            var usageNode = new SymbolDetail(invokableSymbol)
            {
                SymbolsInsideDerived = derivedTypesUsages
            };
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
            return usageNode;
        }

        if (invokableSymbol.ContainingType.TypeKind == TypeKind.Interface
            || invokableSymbol.IsVirtual
            || invokableSymbol.IsOverride)
        {
            var usages = new List<SymbolDetail>();

            foreach (var symbolInsideThisMethod in await usedSymbolsResolver.GetUsedSymbols(invokableSymbol))
            {
                usages.Add(await PopulateNode(symbolInsideThisMethod, treeSymbolsHashes));
            }

            var usageNode = new SymbolDetail(invokableSymbol)
            {
                SymbolsInsideDerived =
                    (await GetDerivedTypesUsages(invokableSymbol, treeSymbolsHashes))
                    .ToList(),
                SymbolsInside = usages
            };
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
            return usageNode;
        }

        {
            var usageNode = new SymbolDetail(invokableSymbol);
            treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());

            foreach (var usageSymbol in
                     await usedSymbolsResolver.GetUsedSymbols(invokableSymbol))
            {
                var innerUsageNode =
                    await PopulateNode(usageSymbol, treeSymbolsHashes);
                usageNode.SymbolsInside.Add(innerUsageNode);
                treeSymbolsHashes.Add(usageNode.SymbolsInside.Last().Symbol.GetHashCode());
            }

            return usageNode;
        }
    }
    
    private async Task<SymbolDetail> PopulateNodeFromType(ITypeSymbol typeSymbol, ISet<int> treeSymbolsHashes)
    {
        var usageNode = new SymbolDetail(typeSymbol);

        foreach (var containingSymbol in await _containingSymbolsResolverForTypes.GetUsedSymbols(typeSymbol))
        {
            usageNode.SymbolsInside.Add(await PopulateNode(containingSymbol, treeSymbolsHashes));
            treeSymbolsHashes.Add(usageNode.SymbolsInside.Last().Symbol.GetHashCode());
        }

        return usageNode;
    }

    private async Task<SymbolDetail> PopulateNodeFromField(IFieldSymbol fieldSymbol, ISet<int> treeSymbolsHashes)
    {
        var usageNode = new SymbolDetail(fieldSymbol);

        foreach (var containingSymbol in await _containingSymbolsResolverForFields.GetUsedSymbols(fieldSymbol))
        {
            usageNode.SymbolsInside.Add(await PopulateNode(containingSymbol, treeSymbolsHashes));
            treeSymbolsHashes.Add(usageNode.SymbolsInside.Last().Symbol.GetHashCode());
        }

        return usageNode;
    }

    public async Task<SymbolDetail> PopulateNode(ISymbol symbol, ISet<int> treeSymbolsHashes)
    {
        if (treeSymbolsHashes.Contains(symbol.GetHashCode()))
            return new SymbolDetail(symbol);

        // DO NOT change order of if!

        if (symbol is IFieldSymbol fieldSymbol)
        {
            return await PopulateNodeFromField(fieldSymbol, treeSymbolsHashes);
        }

        if (symbol is IPropertySymbol propertySymbol)
        {
            return await PopulateNodeFromInvokable(propertySymbol, _containingSymbolsResolverForProperties,
                treeSymbolsHashes);
        }

        if (symbol is IMethodSymbol methodSymbol)
        {
            return await PopulateNodeFromInvokable(methodSymbol, _containingSymbolsResolverForMethods,
                treeSymbolsHashes);
        }

        if (symbol is ITypeSymbol typeSymbol)
        {
            return await PopulateNodeFromType(typeSymbol, treeSymbolsHashes);
        }

        // stub
        return new SymbolDetail(symbol);
    }

    private async Task<IReadOnlyCollection<SymbolDetail>> GetDerivedTypesUsages(ISymbol symbol,
        ISet<int> treeSymbolsHashes)
    {
        var derivedClasses = _inheritanceManager.GetAllDerivedClasses(symbol.ContainingType);

        var usageNodes = new List<SymbolDetail>();
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