using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotLurker;

public class TreeJsonSerializer
{
    public class JsonNode
    {
        [JsonPropertyName("rootNode")]
        public Node RootNode { get; set; }
        [JsonPropertyName("related")]
        public IEnumerable<Node> Related { get; set; }

        public JsonNode(Node rootNode, IEnumerable<Node> related)
        {
            RootNode = rootNode;
            Related = related;
        }
    }
    
    public string ToJson(Dictionary<Node, HashSet<Node>> tree)
    {
        return JsonConvert.SerializeObject(tree);
    }

    public IEnumerable<JsonNode> ToJsonNodes(IDictionary<Node, HashSet<Node>> tree)
    {
        var result = new List<JsonNode>();
        foreach (var (rootNode, related) in tree)
        {
            result.Add(new JsonNode(rootNode, related));
        }

        return result;
    }

    public async Task ToJsonFile(Dictionary<Node, HashSet<Node>> tree, string filePath)
    {
        await using var file = File.CreateText(filePath);
        var serializer = new JsonSerializer();
        serializer.Serialize(file, tree);
    }
}