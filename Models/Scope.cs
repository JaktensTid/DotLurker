namespace DotLurker.Models;

public class Scope
{
    public string Name { get; set; }
    public ScopeEntryPoint ScopeEntryPoint { get; set; }
}

public class ScopeEntryPoint
{
    public string AssemblyName { get; set; }
    public string ClassName { get; set; }
    public string MemberName { get; set; }
}