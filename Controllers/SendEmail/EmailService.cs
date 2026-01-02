namespace ManageTask.Controllers.SendEmail
{
    public class EmailService
    {
        public async Task<bool> SendEmailVerificationCode(string email, string code)
        {
            return true;
        }

        public async Task<bool> SendOrgInvitation(string email, string orgId)
        {
            return true;
        }
    }
}
