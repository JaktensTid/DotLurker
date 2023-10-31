namespace DotLurker.CodeGenerator;

public static class RandomExtensions
{
    public static bool RandomBoolean(this Random random)
    {
        return (new Random()).Next(0, 2) == 0;
    }
}