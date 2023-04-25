using Microsoft.CodeAnalysis;

namespace DotLurker;

public static class CallableAnalyzer
{
    public static bool IsCallable(ISymbol symbol)
    {
        if (symbol is IMethodSymbol)
            return true;

        if (symbol is IPropertySymbol)
            return true;

        return false;
    }
}