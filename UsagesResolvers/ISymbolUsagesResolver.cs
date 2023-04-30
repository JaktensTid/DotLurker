using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public interface ISymbolUsagesResolver<in T> where T : ISymbol
{
    Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(T symbol);
}