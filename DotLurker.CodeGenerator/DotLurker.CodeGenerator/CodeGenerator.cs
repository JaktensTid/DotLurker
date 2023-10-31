using System.Text;
using DotLurker.CodeGenerator.CodeGeneratorModels;

namespace DotLurker.CodeGenerator;

public class CodeGenerator
{
    public IEnumerable<NamespaceNode> GenerateTree(int randomSeed)
    {
        var random = new Random();
        var stringGenerator = new StringGenerator();
        var numberOfNamespaces = (new Random()).Next(1, 10);

        var namespaces = new List<NamespaceNode>();
        for (var iNamespaces = 0; iNamespaces < numberOfNamespaces; iNamespaces++)
        {
            var numberOfClasses = (new Random()).Next(1, 10);
            var @namespace = new NamespaceNode(randomSeed, stringGenerator);

            for (var iClasses = 0; iClasses < numberOfClasses; iClasses++)
            {
                var @class = new ClassNode(stringGenerator, randomSeed);
                @namespace.InnerNodes.Add(@class);
                var numberOfMethods = (new Random()).Next(1, 10);
                for (var iMethods = 0; iMethods < numberOfMethods; iMethods++)
                {
                    @class.InnerNodes.Add(new MethodNode(stringGenerator, randomSeed));
                }
            }
            namespaces.Add(@namespace);
        }

        return namespaces;
    }

    public string Generate(IEnumerable<NamespaceNode> namespaceNodes)
    {
        var sb = new StringBuilder();
        foreach (var node in namespaceNodes)
        {
            sb.Append(node.GenerateCode());
        }

        return sb.ToString();
    }
}