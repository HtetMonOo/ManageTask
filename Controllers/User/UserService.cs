
using ManageTask.Controllers.SendEmail;
using ManageTask.Models;
using ManageTask.Models.User;
using Npgsql;
using System.Threading.Tasks;

namespace ManageTask.Controllers.User
{
    public class UserService
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public UserService(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<bool> CheckExistingEmail(string email)
        {
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = "SELECT 1 FROM \"user\" WHERE email = @Email LIMIT 1;";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", email);

                await using var reader = await cmd.ExecuteReaderAsync();
                return reader.HasRows;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }


        public async Task<string> SendVerificationCode(string email)
        {
            string code = GenerateRandomCode();
            string processId = Guid.NewGuid().ToString();
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = "INSERT INTO verification_record (processId, email, code, expired_date) " +
                    "VALUES (@ID, @Email, @Code, @Expire);";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", processId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Expire", DateTime.UtcNow.AddMinutes(10));

                await cmd.ExecuteNonQueryAsync();

                await _emailService.SendEmailVerificationCode(email, code);

                return processId;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }


        }

        public async Task<GeneralResponseModel> CheckEmailVerification(string processId, string email, string code)
        {
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = @"
                    DELETE FROM verification_record
                    WHERE processId = @Id
                      AND email = @Email
                      AND code = @Code
                      AND expired_date > NOW()
                    RETURNING 1;
                    ";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", processId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Code", code);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                {
                    return new GeneralResponseModel
                    {
                        Success = false,
                        Message = "Invalid or expired verification code."
                    };
                }

                return new GeneralResponseModel
                {
                    Success = true,
                    Message = "Verified"
                };

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new GeneralResponseModel
            {
                Success = false,
                Message = "Something went wrong! Try again."
            };
        }


        public async Task<int> SaveUser(UserModel user)
        {
            user.UserId = Guid.NewGuid().ToString();
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = "INSERT INTO \"user\" (userid, name, email, password, profile_picture, status) " +
                    "VALUES (@ID, @Name, @Email, @Password, @Profile, @Status);";

                await using var cmd = new NpgsqlCommand(query, conn);
                
                cmd.Parameters.AddWithValue("@ID", user.UserId);
                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword(user.Password));
                cmd.Parameters.AddWithValue("@Profile", user.ProfilePicture ?? "");
                cmd.Parameters.AddWithValue("@Status", "Active");

                int result = await cmd.ExecuteNonQueryAsync();
                return result;

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
            
        }


        public async Task<int> UpdateUser(UserModel user)
        {
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = @"
                    UPDATE ""user"" 
                    SET name = @Name, email = @Email, profile_picture = @Profile 
                    WHERE userid = @ID;";

                await using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ID", user.UserId);
                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Profile", user.ProfilePicture ?? "");
                cmd.Parameters.AddWithValue("@Status", "Active");

                int result = await cmd.ExecuteNonQueryAsync();
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }

        }

        public async Task<int> ChangePassword(string userid, string password)
        {
            string dbConnection = _configuration["ConnectionString"]!;
            try
            {
                await using var conn = new NpgsqlConnection(dbConnection);
                await conn.OpenAsync();

                string query = @"
                    UPDATE ""user"" 
                    SET password = @Password
                    WHERE userid = @ID;";

                await using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ID", userid);
                cmd.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword(password));

                int result = await cmd.ExecuteNonQueryAsync();
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }

        }


        private static Random generator = new Random();

        public static string GenerateRandomCode()
        {
            int r = generator.Next(0, 1000000);

            string sixDigitCode = r.ToString("D6");

            return sixDigitCode;
        }
    }
}
