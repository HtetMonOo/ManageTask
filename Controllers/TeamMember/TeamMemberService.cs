using ManageTask.Models;
using ManageTask.Models.Team;
using Npgsql;

namespace ManageTask.Controllers.TeamMember
{
    public class TeamMemberService
    {
        private readonly IConfiguration _configuration;
        private string ConnStr => _configuration["ConnectionString"]!;

        public TeamMemberService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Add team member (must be org member)
        public async Task<GeneralResponseModel> AddTeamMember(
            string teamId, string userId, string currentUserId)
        {
            try
            {
                string query = @"
                INSERT INTO TeamMember (teamid, userid, status)
                SELECT @TeamId, @UserId, 'Active'
                WHERE EXISTS (
                    SELECT 1 FROM OrganizationMember om
                    JOIN Team t ON t.orgid = om.orgid
                    WHERE t.teamid = @TeamId
                      AND om.userid = @TargetUser
                      AND om.status = 'Active'
                )
                AND EXISTS (
                    SELECT 1 FROM OrganizationMember om
                    JOIN Team t ON t.orgid = om.orgid
                    WHERE t.teamid = @TeamId
                      AND om.userid = @CurrentUser
                      AND om.role = 'Admin'
                      AND om.status = 'Active'
                );";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TargetUser", userId);
                cmd.Parameters.AddWithValue("@CurrentUser", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Member added" }
                    : new GeneralResponseModel { Success = false, Message = "Operation not allowed" };
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }

        }

        // Remove team member
        public async Task<GeneralResponseModel> RemoveTeamMember(
            string teamId, string userId, string currentUserId)
        {
            try
            {
                string query = @"
                UPDATE TeamMember tm
                SET status = 'Inactive'
                WHERE tm.teamid = @TeamId
                  AND tm.userid = @UserId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      JOIN Team t ON t.orgid = om.orgid
                      WHERE t.teamid = @TeamId
                        AND om.userid = @CurrentUser
                        AND om.role = 'Admin'
                        AND om.status = 'Active'
                  );";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CurrentUser", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Member removed" }
                    : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }

        }

        // List team members
        public async Task<List<TeamMemberModel>> ListTeamMembers(string teamId)
        {
            var list = new List<TeamMemberModel>();

            try
            {
                string query = @"
                SELECT u.userid, u.name, u.email
                FROM TeamMember tm
                JOIN ""User"" u ON u.userid = tm.userid
                WHERE tm.teamid = @TeamId AND tm.status = 'Active';";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new TeamMemberModel
                    {
                        UserId = reader["userid"].ToString(),
                        Name = reader["name"].ToString(),
                        Email = reader["email"].ToString()
                    });
                }

                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return list;
            }

        }
    }
}
