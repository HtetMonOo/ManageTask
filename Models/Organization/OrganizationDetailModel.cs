namespace ManageTask.Models.Organization
{
    public class OrganizationDetailModel
    {
        public int MemberCount { get; set; }
        public List<ProjectSummaryModel> Projects { get; set; }

        public OrganizationDetailModel()
        {
            Projects = new List<ProjectSummaryModel>();
        }
    }
}
