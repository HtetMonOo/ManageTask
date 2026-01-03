using ManageTask.Controllers.Task;
using ManageTask.Models.Task;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.Task
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly TaskService _taskService;

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Create task
        [HttpPost("/task/create")]
        public async Task<IActionResult> CreateTask(TaskModel task)
        {
            var result = await _taskService.CreateTask(task, CurrentUserId());
            return Ok(result);
        }

        // Delete task
        [HttpPost("/task/delete")]
        public async Task<IActionResult> DeleteTask(string taskId)
        {
            var result = await _taskService.DeleteTask(taskId, CurrentUserId());
            return Ok(result);
        }

        // Update task info
        [HttpPost("/task/update")]
        public async Task<IActionResult> UpdateTask(string taskId, string title, string description, DateOnly deadline)
        {
            var result = await _taskService.UpdateTask(taskId, title, description, deadline, CurrentUserId());
            return Ok(result);
        }

        // Toggle task status
        [HttpPost("/task/toggle-status")]
        public async Task<IActionResult> ToggleStatus(string taskId)
        {
            var result = await _taskService.ToggleTaskStatus(taskId, CurrentUserId());
            return Ok(result);
        }

        // List tasks by project
        [HttpGet("/task/list")]
        public async Task<IActionResult> ListByProject(string projectId)
        {
            return Ok(await _taskService.ListTasksByProject(projectId));
        }

        // Task detail
        [HttpGet("/task/detail")]
        public async Task<IActionResult> TaskDetail(string taskId)
        {
            return Ok(await _taskService.GetTaskDetail(taskId));
        }

        
    }
}
