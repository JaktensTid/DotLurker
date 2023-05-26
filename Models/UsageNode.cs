using Microsoft.CodeAnalysis;

namespace DotLurker.Models;

public class UsageNode
{
    public ISymbol Symbol { get; set; }
    public List<UsageNode> Usages { get; set; } = new();
    public List<UsageNode> DerivedUsages { get; set; } = new();

    public UsageNode(ISymbol symbol)
    {
        Symbol = symbol;
    }

    public override string ToString()
    {
        return Symbol.ToDisplayString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not UsageNode node)
            return false;

        return node.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        return Symbol.GetHashCode();
    }
}