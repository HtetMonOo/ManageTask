using ManageTask.Controllers.CipherHelper;
using ManageTask.Controllers.SendEmail;
using ManageTask.Models.User;
using Npgsql;

namespace ManageTask.Controllers.Account
{
    public class AccountService
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AccountService(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<UserVerifyResponse> VerifyUser(string password, string email)
        {
            UserModel userData = new UserModel();
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = "SELECT userid, name, email, password FROM \"User\" WHERE email = @Email AND status = 'Active' LIMIT 1;";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", email);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string hashedPassword = reader["password"] as string;
                    

                    if (BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                    {
                        userData.Name = reader["name"] as string;
                        userData.Email = reader["email"] as string;
                        userData.UserId = reader["userid"] as string;
                        return new UserVerifyResponse
                        {
                            Success = true,
                            Message = "Verified",
                            UserData = userData
                        };
                    }else
                    {
                        return new UserVerifyResponse
                        {
                            Success = false,
                            Message = "Wrong password!"
                        };
                    }
                    
                }else
                {
                    return new UserVerifyResponse
                    {
                        Success = false,
                        Message = "This email is not registered!"
                    };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new UserVerifyResponse
            {
                Success = false,
                Message = "Error verifying user! Try again."
            };
        }

    }
}
