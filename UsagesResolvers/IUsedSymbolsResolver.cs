using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public interface IUsedSymbolsResolver<in T> where T : ISymbol
{
    Task<IReadOnlyCollection<ISymbol>> GetUsedSymbols(T ofSymbol);
}