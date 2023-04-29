using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotLurker.UsagesResolvers;

public class MethodContainingSymbolsResolver : IContainingSymbolsResolver<IMethodSymbol>
{
    private readonly IDictionary<string, Compilation> _compilations;

    public MethodContainingSymbolsResolver(IDictionary<string, Compilation> compilations)
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
            
            // Invocable
            var allInvocableUsages = methodNode.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var usage in allInvocableUsages)
            {
                var symbol = model.GetSymbolInfo(usage).Symbol;
                //if (symbol. == TypeKind.Delegate)
                //{
               //     Console.WriteLine($"Type '{typeSymbol.Name}' is a delegate type.");
                //}
            }
        }
    
        return usedSymbols;
    }
}