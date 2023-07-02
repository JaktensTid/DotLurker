namespace DotLurker;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AssembliesExtractor
{
    public static HashSet<string> ExtractAssemblies(IEnumerable<Project> projects)
    {
        return projects.Select(x => x.DefaultNamespace).ToHashSet();
    }
}