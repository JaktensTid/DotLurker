using Microsoft.CodeAnalysis;

namespace DotLurker;

public static class NodeTypeExtensions
{
    public static NodeType ToNodeType(this TypeKind typeKind)
    {
        if (typeKind == TypeKind.Class)
            return NodeType.Class;

        if (typeKind == TypeKind.Enum)
            return NodeType.Enum;

        if (typeKind == TypeKind.Interface)
            return NodeType.Interface;

        if (typeKind == TypeKind.Struct)
            return NodeType.Struct;

        return NodeType.Class;
    }
}