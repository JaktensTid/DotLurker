using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotLurker.UsagesResolvers;

public class MethodContainingSymbolsResolver : IContainingSymbolsResolver<IMethodSymbol>
{
    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(IMethodSymbol symbol, Compilation compilation)
    {
        return await GetAllContainingSymbols(symbol, x => true, compilation);
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(
        IMethodSymbol methodSymbol,
        Predicate<ISymbol> symbolFilter,
        Compilation compilation)
    {
        var result = new List<ISymbol>();

        foreach (var symbol in await FindSymbolsInMethod(methodSymbol, compilation))
        {
            result.Add(symbol);
        }

        return result;
    }

    private async Task<List<ISymbol>> FindSymbolsInMethod(IMethodSymbol methodSymbol, Compilation compilation)
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
                if (symbol != null)
                    usedSymbols.Add(symbol);
            }
        }

        return usedSymbols;
    }
}