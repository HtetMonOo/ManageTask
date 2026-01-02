using ManageTask.Models;
using ManageTask.Models.Team;
using Npgsql;

namespace ManageTask.Controllers.Team
{
    public class TeamService
    {
        private readonly IConfiguration _configuration;
        private string ConnStr => _configuration["ConnectionString"]!;

        public TeamService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Create team (org admin only)
        public async Task<GeneralResponseModel> CreateTeam(
            string orgId, string name, string description, string currentUserId)
        {
            try
            {
                string teamId = Guid.NewGuid().ToString();

                string query = @"
                INSERT INTO Team (teamid, orgid, name, description, status)
                SELECT @TeamId, @OrgId, @Name, @Desc, 'Active'
                WHERE EXISTS (
                    SELECT 1 FROM OrganizationMember
                    WHERE orgid = @OrgId
                      AND userid = @UserId
                      AND role = 'Admin'
                      AND status = 'Active'
                );";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);
                cmd.Parameters.AddWithValue("@OrgId", orgId);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Desc", description ?? "");
                cmd.Parameters.AddWithValue("@UserId", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Team created" }
                    : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
            }
            catch(Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }
            
        }

        // Toggle team
        public async Task<GeneralResponseModel> ToggleTeam(string teamId, string currentUserId)
        {
            try
            {
                string query = @"
                UPDATE Team t
                SET status = CASE WHEN status = 'Active' THEN 'Inactive' ELSE 'Active' END
                WHERE t.teamid = @TeamId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      WHERE om.orgid = t.orgid
                        AND om.userid = @UserId
                        AND om.role = 'Admin'
                        AND om.status = 'Active'
                  );";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);
                cmd.Parameters.AddWithValue("@UserId", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Team status updated" }
                    : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
            }
            catch(Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }
        }

        

        // List teams
        public async Task<List<TeamModel>> ListTeams(string orgId)
        {
            var list = new List<TeamModel>();
            try
            {
                
                string query = @"SELECT teamid, name, description, status FROM Team
                             WHERE orgid = @OrgId AND status = 'Active';";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@OrgId", orgId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new TeamModel
                    {
                        TeamId = reader["teamid"].ToString(),
                        OrgId = orgId,
                        Name = reader["name"].ToString(),
                        Description = reader["description"].ToString(),
                        Status = reader["status"].ToString()
                    });
                }

                return list;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return list;
            }
            
        }

        // Team detail
        public async Task<TeamDetailModel> GetTeamDetail(string teamId)
        {
            var result = new TeamDetailModel();
            try
            {
                string query = @"
                SELECT t.teamid, t.name, t.description,
                       COUNT(tm.userid) AS membercount
                FROM Team t
                LEFT JOIN TeamMember tm
                    ON t.teamid = tm.teamid AND tm.status = 'Active'
                WHERE t.teamid = @TeamId
                GROUP BY t.teamid, t.name, t.description;";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    result.TeamId = reader["teamid"].ToString();
                    result.Name = reader["name"].ToString();
                    result.Description = reader["description"].ToString();
                    result.MemberCount = Convert.ToInt32(reader["membercount"]);
                }

                return result;
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return result;
            }
            
        }

        // Update team info
        public async Task<GeneralResponseModel> UpdateTeam(
            string teamId, string name, string description, string currentUserId)
        {
            try
            {
                string query = @"
                UPDATE Team t
                SET name = @Name, description = @Desc
                WHERE t.teamid = @TeamId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      WHERE om.orgid = t.orgid
                        AND om.userid = @UserId
                        AND om.role = 'Admin'
                        AND om.status = 'Active'
                  );";

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamId", teamId);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Desc", description ?? "");
                cmd.Parameters.AddWithValue("@UserId", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Team updated" }
                    : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }

        }

        
    }
}
