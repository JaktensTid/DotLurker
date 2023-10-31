using Microsoft.AspNetCore.Mvc;

namespace DotLurker.Web.Controllers;

public class PresentationsController : Controller
{
    [Route("/jointjs")]
    public IActionResult JointJs()
    {
        return View("JointJsView");
    }
}