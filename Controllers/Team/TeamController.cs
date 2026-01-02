using ManageTask.Controllers.Team;
using ManageTask.Models.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.Team
{
    [Authorize]
    public class TeamController : Controller
    {
        private readonly TeamService _teamService;

        public TeamController(TeamService teamService)
        {
            _teamService = teamService;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Create team
        [HttpPost("/team/create")]
        public async Task<IActionResult> CreateTeam(string orgId, string name, string description)
        {
            var result = await _teamService.CreateTeam(orgId, name, description, CurrentUserId());
            return Ok(result);
        }

        // Delete (toggle) team
        [HttpPost("/team/toggle")]
        public async Task<IActionResult> ToggleTeam(string teamId)
        {
            var result = await _teamService.ToggleTeam(teamId, CurrentUserId());
            return Ok(result);
        }

        // List teams in organization
        [HttpGet("/team/list")]
        public async Task<IActionResult> ListTeams(string orgId)
        {
            var teams = await _teamService.ListTeams(orgId);
            return Ok(teams);
        }

        // Team detail
        [HttpGet("/team/detail")]
        public async Task<IActionResult> GetTeamDetail(string teamId)
        {
            var detail = await _teamService.GetTeamDetail(teamId);
            return Ok(detail);
        }

        // Update team info
        [HttpPost("/team/update")]
        public async Task<IActionResult> UpdateTeam(string teamId, string name, string description)
        {
            var result = await _teamService.UpdateTeam(teamId, name, description, CurrentUserId());
            return Ok(result);
        }

        
    }
}
