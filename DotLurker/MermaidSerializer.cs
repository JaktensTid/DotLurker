namespace DotLurker;

public class MermaidSerializer
{
    public string ToMermaid(Dictionary<Node, HashSet<Node>> tree)
    {
        var mermaid = new List<string>
        {
            "classDiagram"
        };

        var typeNames = tree.Keys.Select(x => x.TypeName);
        var groupedByNamespaces = tree.Keys.GroupBy(x => x.Namespace);

        string GetNodeName(Node node)
        {
            var typeName = node.TypeName;
            if (typeNames.Count(x => x == typeName) > 1)
            {
                typeName = $"`{node.FullTypeName()}`";
            }

            return typeName;
        }

        // Define types
        foreach (var group in groupedByNamespaces)
        {
            mermaid.Add($"namespace {group.Key} {{");

            var tab = "    ";

            foreach (var node in group)
            {
                var typeName = GetNodeName(node);

                mermaid.Add($"{tab}class {typeName} {{");

                if (node.NodeType is NodeType.Interface)
                {
                    mermaid.Add($"{tab}{tab}<<Interface>> {typeName}");
                }

                if (node.NodeType is NodeType.Enum)
                {
                    mermaid.Add($"{tab}{tab}<<Enumeration>> {typeName}");
                }

                mermaid.Add($"{tab}}}");
            }

            mermaid.Add("}");
        }

        // Define relations
        foreach (var (node, relations) in tree)
        {
            var typeName = GetNodeName(node);

            foreach (var relationNode in relations)
            {
                var relationTypeName = GetNodeName(relationNode);

                if (relationNode.DependencyType == DependencyType.Base)
                {
                    mermaid.Add($"{relationTypeName} --|> {typeName} : Inheritance");
                }

                if (relationNode.DependencyType == DependencyType.Default)
                {
                    mermaid.Add($"{typeName} ..> {relationTypeName} : Dependency");
                }
            }
        }

        return string.Join(Environment.NewLine, mermaid);
    }
}