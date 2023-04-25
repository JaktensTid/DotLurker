using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class UsagesResolver
{
    private readonly InheritanceManager _inheritanceManager;

    public UsagesResolver(InheritanceManager inheritanceManager)
    {
        _inheritanceManager = inheritanceManager;
    }

    public async Task<UsageNode> PopulateNode(ISymbol symbol,
        Predicate<ISymbol> symbolsFilter, Compilation compilation, ISet<int> treeSymbolsHashes)
    {
        if (treeSymbolsHashes.Contains(symbol.GetHashCode()) || !symbolsFilter(symbol))
            return new UsageNode(symbol);

        if (symbol is IMethodSymbol methodSymbol)
        {
            var methodUsagesResolver = new MethodContainingSymbolsResolver();

            // Get all derived types usages
            if (methodSymbol.IsAbstract)
            {
                var derivedTypesUsages =
                    (await GetDerivedTypesUsages(methodSymbol, symbolsFilter, compilation, treeSymbolsHashes)).ToList();
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

                foreach (var symbolInsideThisMethod in await methodUsagesResolver.GetAllContainingSymbols(methodSymbol, symbolsFilter, compilation))
                {
                    usages.Add(await PopulateNode(symbolInsideThisMethod, symbolsFilter, compilation, treeSymbolsHashes));
                }

                var usageNode = new UsageNode(methodSymbol)
                {
                    DerivedUsages =
                        (await GetDerivedTypesUsages(methodSymbol, symbolsFilter, compilation, treeSymbolsHashes))
                        .ToList(),
                    Usages = usages
                };
                treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
                return usageNode;
            }

            {
                var usageNode = new UsageNode(symbol);
                treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());

                foreach (var usageSymbol in await methodUsagesResolver.GetAllContainingSymbols(methodSymbol, symbolsFilter,
                             compilation))
                {
                    var innerUsageNode =
                        await PopulateNode(usageSymbol, symbolsFilter, compilation, treeSymbolsHashes);
                    usageNode.Usages.Add(innerUsageNode);
                    treeSymbolsHashes.Add(usageNode.Usages.Last().Symbol.GetHashCode());
                }

                return usageNode;
            }
        }
        
        // stub
        return new UsageNode(symbol);
    }

    public async Task<IReadOnlyCollection<UsageNode>> GetDerivedTypesUsages(ISymbol symbol,
        Predicate<ISymbol> symbolsFilter, Compilation compilation, ISet<int> treeSymbolsHashes)
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
                var usageNode = await PopulateNode(inheritedMember, symbolsFilter, compilation, treeSymbolsHashes);
                treeSymbolsHashes.Add(usageNode.Symbol.GetHashCode());
                usageNodes.Add(usageNode);
            }
        }

        return usageNodes;
    }
}