using Microsoft.CodeAnalysis;

namespace DotLurker.Models;

public class SymbolDetail
{
    public ISymbol Symbol { get; set; }
    public List<SymbolDetail> SymbolsInside { get; set; } = new();
    public List<SymbolDetail> SymbolsInsideDerived { get; set; } = new();

    public SymbolDetail(ISymbol symbol)
    {
        Symbol = symbol;
    }

    public override string ToString()
    {
        return Symbol.ToDisplayString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SymbolDetail node)
            return false;

        return node.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        return Symbol.GetHashCode();
    }
}