using System.Text;

namespace DotLurker.CodeGenerator.CodeGeneratorModels;

public class NamespaceNode : CodeGeneratorNode
{
    public override string NodeType => "Namespace";

    protected override string Generate()
    {
        var stringBuilder = new StringBuilder();

        var inlineNamespace = (new Random()).RandomBoolean();
        var newLineBracket = (new Random()).RandomBoolean();

        var content = string.Join("", InnerNodes.Select(x => x.GenerateCode()));
        
        if (inlineNamespace)
        {
            stringBuilder.Append($"namespace {Name}");
            stringBuilder.AppendLine();
            stringBuilder.Append(content);
        }
        else
        {
            stringBuilder.Append($"namespace {Name}{Environment.NewLine} " + (newLineBracket
                ? Environment.NewLine + "{"
                : "{"));
            stringBuilder.AppendLine();
            stringBuilder.Append(content);
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    public NamespaceNode(int randomSeed, StringGenerator stringGenerator) : base(stringGenerator, randomSeed)
    {
    }
}