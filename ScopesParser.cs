using System.Text.Json;
using System.Text.Json.Serialization;
using DotLurker.Models;

namespace DotLurker;

public class ScopesParser
{
    public async Task<IReadOnlyCollection<Scope>> GetConfiguration(string path)
    {
        var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<IReadOnlyCollection<Scope>>(stream);
    }
}