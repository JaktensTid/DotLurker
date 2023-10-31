using System.Text;

namespace DotLurker.CodeGenerator.CodeGeneratorModels;

public class MethodNode : CodeGeneratorNode
{
    public MethodNode(StringGenerator stringGenerator, int randomSeed) : base(stringGenerator, randomSeed)
    {
    }

    public override string NodeType => "Method";

    protected override string Generate()
    {
        var numberOfWordsBeforeName = (new Random()).Next(4);
        var sb = new StringBuilder();
        for (var i = 0; i < numberOfWordsBeforeName; i++)
        {
            sb.Append(StringGenerator.GenerateRandomString(4));
            sb.Append(' ');
        }

        sb.Append(Name);
        sb.Append("(");
        sb.Append(StringGenerator.GenerateRandomString(4));
        sb.Append(")");
        sb.AppendLine();
        sb.Append("{");
        sb.AppendLine();

        var numberOfLinesInsideMethod = (new Random()).Next(15);
        for (var i = 0; i < numberOfLinesInsideMethod; i++)
        {
            var line = StringGenerator.GenerateRandomString(new Random().Next(30), false, false, ". <>[]()-+=*/|&");
            sb.Append(line);
            sb.Append(';');
            sb.AppendLine();
        }

        sb.Append("}");
        return sb.ToString();
    }
}