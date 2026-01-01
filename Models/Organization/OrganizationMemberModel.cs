namespace ManageTask.Models.Organization
{
    public class OrganizationMemberModel
    {
        public string OrgId { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; }   // Admin | Member
        public string Status { get; set; } // Active | Inactive
    }
}
