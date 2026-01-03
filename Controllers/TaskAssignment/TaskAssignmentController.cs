using ManageTask.Controllers.Task;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.TaskAssignment
{
    public class TaskAssignmentController : Controller
    {
        private readonly TaskAssignmentService _taskAssignmentService;

        public TaskAssignmentController(TaskAssignmentService taskAssignmentService)
        {
            _taskAssignmentService = taskAssignmentService;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        // Assignment
        [HttpPost("/task/assign/user")]
        public async Task<IActionResult> AssignUser(string taskId, string userId)
        {
            return Ok(await _taskAssignmentService.AssignTask(taskId, "User", userId, CurrentUserId()));
        }

        [HttpPost("/task/assign/team")]
        public async Task<IActionResult> AssignTeam(string taskId, string teamId)
        {
            return Ok(await _taskAssignmentService.AssignTask(taskId, "Team", teamId, CurrentUserId()));
        }

        [HttpPost("/task/unassign")]
        public async Task<IActionResult> Unassign(string taskId, string assignType, string assignId)
        {
            return Ok(await _taskAssignmentService.UnassignTask(taskId, assignType, assignId, CurrentUserId()));
        }
    }
}
