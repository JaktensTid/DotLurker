using Microsoft.CodeAnalysis;

namespace DotLurker;

public static class TypeManager
{
    public static ITypeSymbol GetUnderlyingType(ISymbol symbol)
    {
        if (symbol is IFieldSymbol fieldSymbol)
            return fieldSymbol.Type;

        if (symbol is IPropertySymbol propertySymbol)
            return propertySymbol.Type;

        if (symbol is IMethodSymbol methodSymbol)
            return methodSymbol.ContainingType;

        if (symbol is IParameterSymbol parameterSymbol)
            return parameterSymbol.Type;

        if (symbol is IEventSymbol eventSymbol)
            return eventSymbol.Type;

        if (symbol is ITypeSymbol typeSymbol)
            return typeSymbol;

        if (symbol is ILocalSymbol localSymbol)
            return localSymbol.Type;
        
        throw new ArgumentException("Cannot determine type of symbol");
    }

    public static bool IsNamespace(this ISymbol symbol)
    {
        return symbol is INamespaceSymbol;
    }
}