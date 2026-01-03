using ManageTask.Models.Task;

namespace ManageTask.Models.Project
{
    public class ProjectDetailModel
    {
        public string ProjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TaskModel> Tasks { get; set; } = new();
    }
}
