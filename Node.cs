using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotLurker;

public class Node
{
    [JsonProperty("nodeType")] 
    [JsonPropertyName("nodeType")] 
    public NodeType NodeType { get; set; }
    [JsonProperty("dependencyType")] 
    [JsonPropertyName("dependencyType")] 
    public DependencyType DependencyType { get; set; }
    [JsonProperty("typeName")]
    [JsonPropertyName("typeName")] 
    public string TypeName { get; set; }
    [JsonProperty("namespace")] 
    [JsonPropertyName("namespace")] 
    public string Namespace { get; set; }
    [JsonProperty("filePath")]
    [JsonPropertyName("filePath")] 
    public string Path { get; set; }
    
    [JsonProperty("fullName")]
    [JsonPropertyName("fullName")] 
    public string FullName => FullTypeName();

    public Node()
    {
    }

    public Node(NodeType type, string typeName, string @namespace, string path, DependencyType dependencyType)
    {
        NodeType = type;
        DependencyType = dependencyType;
        TypeName = typeName;
        Namespace = @namespace;
        Path = path;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Node)
        {
            return false;
        }

        var node = obj as Node;

        return node.TypeName == TypeName && node.Path == Path;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TypeName.GetHashCode(), Path.GetHashCode());
    }

    public string FullTypeName()
    {
        return string.Join('.', Namespace, TypeName);
    }

    public override string ToString()
    {
        return FullTypeName();
        //return TypeName;
        //return
        //    $"{(DependencyType == DependencyType.Base ? "Base" : "Depe")}, TypeName: {TypeName}, NameSpace: {Namespace}, FilePath: {Path}";
    }
}