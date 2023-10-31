using System.Text;

namespace DotLurker.CodeGenerator.CodeGeneratorModels;

public class FileNode : CodeGeneratorNode
{
    public FileNode(IEnumerable<NamespaceNode> namespaces, StringGenerator stringGenerator, int randomSeed) : base(
        stringGenerator, randomSeed)
    {
        InnerNodes = namespaces.Cast<CodeGeneratorNode>().ToList();
    }

    public override string NodeType => "File";

    protected override string Generate()
    {
        var stringBuilder = new StringBuilder();
        foreach (var codeGeneratorNode in InnerNodes)
        {
            stringBuilder.Append(codeGeneratorNode.GenerateCode());
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }
}