using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public interface ISymbolReferencesResolver<in T> where T : ISymbol
{
    Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(T ofSymbol);
}