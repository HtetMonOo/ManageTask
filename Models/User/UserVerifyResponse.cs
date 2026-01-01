namespace ManageTask.Models.User
{
    public class UserVerifyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserModel UserData { get; set; }
    }
}
