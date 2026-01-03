namespace ManageTask.Models.Task
{
    public class TaskModel
    {
        public string TaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ProjectId { get; set; }
        public string ParentTaskId { get; set; }
        public DateOnly Deadline { get; set; }
        public string Status { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
