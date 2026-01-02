using ManageTask.Controllers.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.OrganizationMember
{
    [Authorize]
    public class OrgMemberController : Controller
    {
        private readonly OrgMemberService _orgMemberService;

        public OrgMemberController(OrgMemberService orgMemberService)
        {
            _orgMemberService = orgMemberService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        // Add / Remove organization admin
        [HttpPost("/organization/admin/add")]
        public async Task<IActionResult> AddAdmin(string orgId, string userId)
        {
            string currentUserId = GetCurrentUserId();
            var result = await _orgMemberService.UpdateOrgRole(orgId, userId, "Admin", currentUserId);
            return Ok(result);
        }

        [HttpPost("/organization/admin/remove")]
        public async Task<IActionResult> RemoveAdmin(string orgId, string userId)
        {
            string currentUserId = GetCurrentUserId();
            var result = await _orgMemberService.UpdateOrgRole(orgId, userId, "Member", currentUserId);
            return Ok(result);
        }

        // Invite user to organization
        [HttpPost("/organization/invite")]
        public async Task<IActionResult> InviteUser(string orgId, string email)
        {
            string inviterId = GetCurrentUserId();
            var result = await _orgMemberService.InviteUser(orgId, email, inviterId);

            return Ok(result);
        }

        // Accept invitation to organization
        [HttpPost("/organization/accept")]
        public async Task<IActionResult> AcceptInvitation(string orgId)
        {
            string userId = GetCurrentUserId();
            var result = await _orgMemberService.AcceptInviation(orgId, userId);

            return Ok(result);
        }

        // Decline invitation to organization
        [HttpPost("/organization/decline")]
        public async Task<IActionResult> DeclineInvitation(string orgId)
        {
            string userId = GetCurrentUserId();
            var result = await _orgMemberService.DeclineInvitation(orgId, userId);

            return Ok(result);
        }
    }
}
