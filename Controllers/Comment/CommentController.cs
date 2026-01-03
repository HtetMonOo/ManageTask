using ManageTask.Controllers.Comment;
using ManageTask.Models.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.Comment
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly CommentService _commentService;

        public CommentController(CommentService commentService)
        {
            _commentService = commentService;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Add comment
        [HttpPost("/task/comment/add")]
        public async Task<IActionResult> AddComment(string taskId, string content)
        {
            return Ok(await _commentService.AddComment(taskId, content, CurrentUserId()));
        }

        // List comments
        [HttpGet("/task/comment/list")]
        public async Task<IActionResult> ListComments(string taskId)
        {
            return Ok(await _commentService.ListComments(taskId));
        }

        // Delete own comment
        [HttpPost("/task/comment/delete")]
        public async Task<IActionResult> DeleteComment(string commentId)
        {
            return Ok(await _commentService.DeleteComment(commentId, CurrentUserId()));
        }

        // Edit own comment
        [HttpPost("/task/comment/edit")]
        public async Task<IActionResult> EditComment(string commentId, string content)
        {
            return Ok(await _commentService.EditComment(commentId, content, CurrentUserId()));
        }
    }
}
