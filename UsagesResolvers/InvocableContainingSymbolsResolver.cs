using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class InvocableContainingSymbolsResolver : ISymbolUsagesResolver<IMethodSymbol>, ISymbolUsagesResolver<IPropertySymbol>
{
    private readonly IDictionary<string, Compilation> _compilations;

    public InvocableContainingSymbolsResolver(IDictionary<string, Compilation> compilations)
    {
        _compilations = compilations;
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(
        IMethodSymbol methodSymbol)
    {
        var result = new List<ISymbol>();

        foreach (var symbol in await FindSymbolsInMethod(methodSymbol))
        {
            result.Add(symbol);
        }

        return result;
    }

    private async Task<List<ISymbol>> FindSymbolsInMethod(IMethodSymbol methodSymbol)
    {
        var usedSymbols = new List<ISymbol>();
        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
        {
            var methodNode = await syntaxReference.GetSyntaxAsync();
            var compilation = _compilations[methodSymbol.ContainingAssembly.Name];
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

    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(IPropertySymbol propertySymbol)
    {
        var usedSymbols = new List<ISymbol>();
        foreach (var syntaxReference in propertySymbol.DeclaringSyntaxReferences)
        {
            var methodNode = await syntaxReference.GetSyntaxAsync();
            var compilation = _compilations[propertySymbol.ContainingAssembly.Name];
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