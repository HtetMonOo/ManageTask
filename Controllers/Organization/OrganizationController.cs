using ManageTask.Models.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.Organization
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly OrganizationService _organizationService;

        public OrganizationController(OrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // 1. Create organization
        [HttpPost("/organization/create")]
        public async Task<IActionResult> CreateOrganization(string name, string description)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Organization name is required");
            }

            string userId = GetCurrentUserId();
            var result = await _organizationService.CreateOrganization(name, description, userId);

            return Ok(result);
        }

        // 2. Toggle organization status
        [HttpPost("/organization/toggle")]
        public async Task<IActionResult> ToggleOrganization(string orgId)
        {
            string userId = GetCurrentUserId();
            var result = await _organizationService.ToggleOrganizationStatus(orgId, userId);

            return Ok(result);
        }

        // 3. Get organizations user belongs to
        [HttpGet("/organization/my")]
        public async Task<IActionResult> GetMyOrganizations()
        {
            string userId = GetCurrentUserId();
            var organizations = await _organizationService.GetOrganizationsForUser(userId);

            return Ok(organizations);
        }

        // 4. Organization details
        [HttpGet("/organization/detail")]
        public async Task<IActionResult> GetOrganizationDetail(string orgId)
        {
            var detail = await _organizationService.GetOrganizationDetail(orgId);
            return Ok(detail);
        }

        // 5. Update organization info
        [HttpPost("/organization/update")]
        public async Task<IActionResult> UpdateOrganization(string orgId, string name, string description)
        {
            string userId = GetCurrentUserId();
            var result = await _organizationService.UpdateOrganization(orgId, name, description, userId);

            return Ok(result);
        }

        // 6. Add / Remove organization admin
        [HttpPost("/organization/admin/add")]
        public async Task<IActionResult> AddAdmin(string orgId, string userId)
        {
            string currentUserId = GetCurrentUserId();
            var result = await _organizationService.UpdateOrgRole(orgId, userId, "Admin", currentUserId);
            return Ok(result);
        }

        [HttpPost("/organization/admin/remove")]
        public async Task<IActionResult> RemoveAdmin(string orgId, string userId)
        {
            string currentUserId = GetCurrentUserId();
            var result = await _organizationService.UpdateOrgRole(orgId, userId, "Member", currentUserId);
            return Ok(result);
        }

        // 7. Invite user to organization
        [HttpPost("/organization/invite")]
        public async Task<IActionResult> InviteUser(string orgId, string email)
        {
            string inviterId = GetCurrentUserId();
            var result = await _organizationService.InviteUser(orgId, email, inviterId);

            return Ok(result);
        }
    }
}
