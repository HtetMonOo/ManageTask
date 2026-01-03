using ManageTask.Controllers.Project;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.Project
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly ProjectService _projectService;

        public ProjectController(ProjectService projectService)
        {
            _projectService = projectService;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Create project
        [HttpPost("/project/create")]
        public async Task<IActionResult> CreateProject(string orgId, string name, string description)
        {
            var result = await _projectService.CreateProject(orgId, name, description, CurrentUserId());
            return Ok(result);
        }

        // Delete (toggle)
        [HttpPost("/project/toggle")]
        public async Task<IActionResult> ToggleProject(string projectId)
        {
            var result = await _projectService.ToggleProject(projectId, CurrentUserId());
            return Ok(result);
        }

        // List projects in org
        [HttpGet("/project/list")]
        public async Task<IActionResult> ListProjects(string orgId)
        {
            var projects = await _projectService.ListProjects(orgId);
            return Ok(projects);
        }

        // Project detail
        [HttpGet("/project/detail")]
        public async Task<IActionResult> GetProjectDetail(string projectId)
        {
            var detail = await _projectService.GetProjectDetail(projectId);
            return Ok(detail);
        }

        // 5. Update project
        [HttpPost("/project/update")]
        public async Task<IActionResult> UpdateProject(string projectId, string name, string description)
        {
            var result = await _projectService.UpdateProject(projectId, name, description, CurrentUserId());
            return Ok(result);
        }

        // Assign project admin
        [HttpPost("/project/admin/add")]
        public async Task<IActionResult> AddProjectAdmin(string projectId, string userId)
        {
            var result = await _projectService.AddProjectAdmin(projectId, userId, CurrentUserId());
            return Ok(result);
        }

        // Remove project admin
        [HttpPost("/project/admin/remove")]
        public async Task<IActionResult> RemoveProjectAdmin(string projectId, string userId)
        {
            var result = await _projectService.RemoveProjectAdmin(projectId, userId, CurrentUserId());
            return Ok(result);
        }
    }
}
