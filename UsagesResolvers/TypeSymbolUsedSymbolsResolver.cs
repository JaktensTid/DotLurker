using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public class TypeSymbolUsedSymbolsResolver : IUsedSymbolsResolver<ITypeSymbol>
{
    private readonly IDictionary<string, Compilation> _compilations;

    public TypeSymbolUsedSymbolsResolver(IDictionary<string, Compilation> compilations)
    {
        _compilations = compilations;
    }
    
    public async Task<IReadOnlyCollection<ISymbol>> GetUsedSymbols(ITypeSymbol ofSymbol)
    {
        var usedSymbols = new List<ISymbol>();
        foreach (var syntaxReference in ofSymbol.DeclaringSyntaxReferences)
        {
            var methodNode = await syntaxReference.GetSyntaxAsync();
            var compilation = _compilations[ofSymbol.ContainingAssembly.Name];
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