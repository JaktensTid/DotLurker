using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotLurker.UsagesResolvers;

public class PropertyUsagesResolver : IContainingSymbolsResolver<IPropertySymbol>
{
    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(IPropertySymbol symbol, Compilation compilation)
    {
        return await GetAllContainingSymbols(symbol, x => true, compilation);
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(IPropertySymbol symbol, Predicate<ISymbol> symbolFilter, Compilation compilation)
    {
        var allOverrides = GetPropertyOverrides(symbol);
        var interfaceImplementations = symbol.ExplicitInterfaceImplementations;

        var allUsages = new List<ISymbol>();
        foreach (var usedSymbol in await FindSymbolsInProperty(symbol, compilation))
        {
            if(symbolFilter(usedSymbol))
                allUsages.Add(usedSymbol);
        }

        foreach (var methodSymbol in interfaceImplementations)
        {
            foreach (var usedSymbol in await FindSymbolsInProperty(methodSymbol, compilation))
            {
                if(symbolFilter(usedSymbol))
                    allUsages.Add(usedSymbol);
            }
        }
        
        foreach (var methodSymbol in allOverrides)
        {
            foreach (var usedSymbol in await FindSymbolsInProperty(methodSymbol, compilation))
            {
                if(symbolFilter(usedSymbol))
                    allUsages.Add(usedSymbol);
            }
        }

        return allUsages;
    }

    private IEnumerable<IPropertySymbol> GetPropertyOverrides(IPropertySymbol methodSymbol)
    {
        if(methodSymbol is { IsVirtual: false, IsAbstract: false })
            yield break;

        var overriddenProperty = methodSymbol.OverriddenProperty;
        if (overriddenProperty == null)
            yield break;

        yield return overriddenProperty;
        foreach (var subOverride in GetPropertyOverrides(overriddenProperty))
        {
            yield return subOverride;
        }
    }

    private async Task<List<ISymbol>> FindSymbolsInProperty(IPropertySymbol methodSymbol, Compilation compilation)
    {
        var usedSymbols = new List<ISymbol>();
        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
        {
            var methodNode = await syntaxReference.GetSyntaxAsync();
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            var allSymbolUsages = methodNode.DescendantNodes().OfType<IdentifierNameSyntax>();

            foreach (var usage in allSymbolUsages)
            {
                var symbol = model.GetSymbolInfo(usage).Symbol;
                if(symbol != null)
                    usedSymbols.Add(symbol);
            }
        }

        return usedSymbols;
    }
}