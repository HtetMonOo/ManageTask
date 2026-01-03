using Microsoft.AspNetCore.Mvc;

namespace ManageTask.Controllers.Project
{
    public class ProjectController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
