namespace ManageTask.Models.Comment
{
    public class CommentModel
    {
        public string CommentId { get; set; }
        public string TaskId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
