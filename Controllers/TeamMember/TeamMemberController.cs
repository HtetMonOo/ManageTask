using ManageTask.Controllers.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.TeamMember
{
    [Authorize]
    public class TeamMemberController : Controller
    {
        private readonly TeamMemberService _teamMemberService;

        public TeamMemberController(TeamMemberService teamMemberService)
        {
            _teamMemberService = teamMemberService;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Add team member
        [HttpPost("/team/member/add")]
        public async Task<IActionResult> AddTeamMember(string teamId, string userId)
        {
            var result = await _teamMemberService.AddTeamMember(teamId, userId, CurrentUserId());
            return Ok(result);
        }

        // Remove team member
        [HttpPost("/team/member/remove")]
        public async Task<IActionResult> RemoveTeamMember(string teamId, string userId)
        {
            var result = await _teamMemberService.RemoveTeamMember(teamId, userId, CurrentUserId());
            return Ok(result);
        }

        // List team members
        [HttpGet("/team/members")]
        public async Task<IActionResult> ListTeamMembers(string teamId)
        {
            var members = await _teamMemberService.ListTeamMembers(teamId);
            return Ok(members);
        }
    }
}
