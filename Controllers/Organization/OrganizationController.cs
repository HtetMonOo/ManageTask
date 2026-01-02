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

        // Create organization
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

        // Toggle organization status
        [HttpPost("/organization/toggle")]
        public async Task<IActionResult> ToggleOrganization(string orgId)
        {
            string userId = GetCurrentUserId();
            var result = await _organizationService.ToggleOrganizationStatus(orgId, userId);

            return Ok(result);
        }

        // Get organizations user belongs to
        [HttpGet("/organization/my")]
        public async Task<IActionResult> GetMyOrganizations()
        {
            string userId = GetCurrentUserId();
            var organizations = await _organizationService.GetOrganizationsForUser(userId);

            return Ok(organizations);
        }

        // Organization details
        [HttpGet("/organization/detail")]
        public async Task<IActionResult> GetOrganizationDetail(string orgId)
        {
            var detail = await _organizationService.GetOrganizationDetail(orgId);
            return Ok(detail);
        }

        // Update organization info
        [HttpPost("/organization/update")]
        public async Task<IActionResult> UpdateOrganization(string orgId, string name, string description)
        {
            string userId = GetCurrentUserId();
            var result = await _organizationService.UpdateOrganization(orgId, name, description, userId);

            return Ok(result);
        }

        
    }
}
