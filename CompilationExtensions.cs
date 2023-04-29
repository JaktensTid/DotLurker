using Microsoft.CodeAnalysis;

namespace DotLurker;

public static class CompilationExtensions
{
    public static Compilation GetCompilation(this IDictionary<IAssemblySymbol, Compilation> compilations, ISymbol symbol)
    {
        var assembly = symbol.ContainingAssembly;
        return compilations[assembly];
    }
}