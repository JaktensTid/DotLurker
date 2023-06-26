using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class InvocableSymbolUsedSymbolsResolver : IUsedSymbolsResolver<IMethodSymbol>, IUsedSymbolsResolver<IPropertySymbol>
{
    private readonly IDictionary<string, Compilation> _compilations;

    public InvocableSymbolUsedSymbolsResolver(IDictionary<string, Compilation> compilations)
    {
        _compilations = compilations;
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetUsedSymbols(IMethodSymbol methodSymbol)
    {
        return await GetAllContainingSymbols(methodSymbol);
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetUsedSymbols(IPropertySymbol propertySymbol)
    {
        return await GetAllContainingSymbols(propertySymbol);
    }

    private async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(ISymbol invokableSymbol)
    {
        var usedSymbols = new List<ISymbol>();
        foreach (var syntaxReference in invokableSymbol.DeclaringSyntaxReferences)
        {
            var methodNode = await syntaxReference.GetSyntaxAsync();
            var compilation = _compilations[invokableSymbol.ContainingAssembly.Name];
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            
            // Normal usages
            var allSymbolUsages = methodNode.DescendantNodes();

            foreach (var usage in allSymbolUsages)
            {
                var symbol = model.GetSymbolInfo(usage).Symbol;
                if (symbol != null)
                    usedSymbols.Add(symbol);
            }
        }
    
        return usedSymbols;
    }
}