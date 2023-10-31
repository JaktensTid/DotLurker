using Microsoft.AspNetCore.Mvc;

namespace DotLurker.Web.Controllers;

public class TreeController : Controller
{
    private readonly TreeExtractor _treeExtractor;

    // GET
    public TreeController(TreeExtractor treeExtractor)
    {
        _treeExtractor = treeExtractor;
    }

    [HttpGet("/tree")]
    public async Task<IEnumerable<TreeJsonSerializer.JsonNode>> Index([FromQuery] string msBuildPath,
        [FromQuery] string solutionPath)
    {
        var treeJsonSerializer = new TreeJsonSerializer();
        return await Task.Run(async () =>
        {
            var result = await _treeExtractor.GenerateDependencyGraph(msBuildPath, solutionPath);
            return treeJsonSerializer.ToJsonNodes(result);
        });
    }
}