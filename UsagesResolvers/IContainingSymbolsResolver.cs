using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

public interface IContainingSymbolsResolver<T> where T : ISymbol
{
    Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(T symbol, Compilation compilation);
    Task<IReadOnlyCollection<ISymbol>> GetAllContainingSymbols(T symbol, Predicate<ISymbol> symbolFilter, Compilation compilation);
}