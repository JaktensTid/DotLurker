using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class InvocableSymbolReferencesResolver : ISymbolReferencesResolver<IMethodSymbol>, ISymbolReferencesResolver<IPropertySymbol>
{
    private readonly IDictionary<string, Compilation> _compilations;

    public InvocableSymbolReferencesResolver(IDictionary<string, Compilation> compilations)
    {
        _compilations = compilations;
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(IMethodSymbol methodSymbol)
    {
        return await GetAllContainingSymbols(methodSymbol as ISymbol);
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(IPropertySymbol propertySymbol)
    {
        return await GetAllContainingSymbols(propertySymbol as ISymbol);
    }

    private async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(ISymbol invocableSymbol)
    {
        var usedSymbols = new List<ISymbol>();
        foreach (var syntaxReference in invocableSymbol.DeclaringSyntaxReferences)
        {
            var methodNode = await syntaxReference.GetSyntaxAsync();
            var compilation = _compilations[invocableSymbol.ContainingAssembly.Name];
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