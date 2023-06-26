using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotLurker.UsagesResolvers;

public class FieldSymbolUsedSymbolsResolver : IUsedSymbolsResolver<IFieldSymbol>
{
    private readonly IReadOnlyCollection<Project> _projects;

    public FieldSymbolUsedSymbolsResolver(IReadOnlyCollection<Project> projects)
    {
        _projects = projects;
    }

    public async Task<IReadOnlyCollection<ISymbol>> GetUsedSymbols(IFieldSymbol symbol)
    {
        var symbols = new List<ISymbol>();
        var assignments = await FindFieldAssignmentsAsync(symbol);
        foreach (var (assignment, semanticModel) in assignments)
        {
            var assignmentSymbol = semanticModel.GetSymbolInfo(assignment.Right).Symbol;
            symbols.Add(assignmentSymbol);
        }

        return symbols;
    }
    
    private async Task<List<(AssignmentExpressionSyntax Assignment, SemanticModel SemanticModel)>> FindFieldAssignmentsAsync(IFieldSymbol fieldSymbol)
    {
        var assignments = new List<(AssignmentExpressionSyntax Assignment, SemanticModel SemanticModel)>();

        foreach (var project in _projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();

                var assignmentExpressions = syntaxRoot.DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>();

                foreach (var assignmentExpression in assignmentExpressions)
                {
                    var leftSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(leftSymbol, fieldSymbol))
                    {
                        assignments.Add((assignmentExpression, semanticModel));
                    }
                }
            }
        }

        return assignments;
    }
}