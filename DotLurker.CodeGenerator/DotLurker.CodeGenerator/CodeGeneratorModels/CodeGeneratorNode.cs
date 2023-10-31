using System.Text.Json.Serialization;

namespace DotLurker.CodeGenerator.CodeGeneratorModels;

public abstract class CodeGeneratorNode
{
    private string _name;

    public string Name
    {
        get
        {
            if (_name == null)
            {
                _name = StringGenerator.GenerateRandomName();
            }

            return _name;
        }
    }

    public abstract string NodeType { get; }
    protected abstract string Generate();
    
    public string GenerateCode()
    {
        var generatedCode = Generate();
        GeneratedCode = generatedCode;
        return generatedCode;
    }
    
    [JsonIgnore]
    public string GeneratedCode { get; protected set; }

    public List<CodeGeneratorNode> InnerNodes { get; set; } = new();

    protected readonly StringGenerator StringGenerator;
    protected Random Random;
    
    public CodeGeneratorNode(StringGenerator stringGenerator, int randomSeed)
    {
        StringGenerator = stringGenerator;
        Random = new Random(randomSeed);
    }
}