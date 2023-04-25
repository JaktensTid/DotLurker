using Microsoft.CodeAnalysis;

namespace DotLurker.UsagesResolvers;

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

    public static UsageNode Empty => new UsageNode(null);
}