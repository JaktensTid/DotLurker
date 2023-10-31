using System.Text;

namespace DotLurker.CodeGenerator.CodeGeneratorModels;

public class ClassNode : CodeGeneratorNode
{
    public ClassNode(StringGenerator stringGenerator, int randomSeed) : base(stringGenerator, randomSeed)
    {
    }

    public override string NodeType => "Class";

    protected override string Generate()
    {
        var sb = new StringBuilder();
        var numberOfWordsInName = (new Random()).Next(5);

        for (var i = 0; i < numberOfWordsInName; i++)
        {
            sb.Append(StringGenerator.GenerateRandomString(4, false, false));
            sb.Append(' ');
        }

        sb.Append("class ");
        sb.Append(Name);
        sb.Append("{");
        sb.Append(Environment.NewLine);

        foreach (var innerNode in InnerNodes)
        {
            sb.Append(innerNode.GenerateCode());
            sb.AppendLine();
        }

        sb.Append("}");

        return sb.ToString();
    }
}