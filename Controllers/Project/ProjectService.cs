using ManageTask.Models;
using ManageTask.Models.Common;
using ManageTask.Models.Project;
using ManageTask.Models.Task;
using Npgsql;

namespace ManageTask.Controllers.Project
{
    public class ProjectService
    {
        private readonly IConfiguration _configuration;
        private string ConnStr => _configuration["ConnectionString"]!;

        public ProjectService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Create project (Org Admin only)
        public async Task<GeneralResponseModel> CreateProject(
            string orgId, string name, string description, string currentUserId)
        {
            string projectId = Guid.NewGuid().ToString();

            string query = @"
                INSERT INTO Project (projectid, orgid, name, description, status)
                SELECT @ProjectId, @OrgId, @Name, @Desc, 'Active'
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
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@OrgId", orgId);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Desc", description ?? "");
            cmd.Parameters.AddWithValue("@UserId", currentUserId);

            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? new GeneralResponseModel { Success = true, Message = "Project created" }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }

        // Toggle project
        public async Task<GeneralResponseModel> ToggleProject(string projectId, string currentUserId)
        {
            string query = @"
                UPDATE Project p
                SET status = CASE WHEN status = 'Active' THEN 'Inactive' ELSE 'Active' END
                WHERE p.projectid = @ProjectId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      WHERE om.orgid = p.orgid
                        AND om.userid = @UserId
                        AND om.role = 'Admin'
                        AND om.status = 'Active'
                  );";

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@UserId", currentUserId);

            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? new GeneralResponseModel { Success = true, Message = "Project status updated" }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }

        // List projects in organization
        public async Task<List<ProjectModel>> ListProjects(string orgId)
        {
            var list = new List<ProjectModel>();

            string query = @"
                SELECT projectid, name, description, status
                FROM Project
                WHERE orgid = @OrgId AND status = 'Active';";

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@OrgId", orgId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProjectModel
                {
                    ProjectId = reader["projectid"].ToString(),
                    OrgId = orgId,
                    Name = reader["name"].ToString(),
                    Description = reader["description"].ToString(),
                    Status = reader["status"].ToString()
                });
            }

            return list;
        }

        // Project detail (with tasks)
        public async Task<ProjectDetailModel> GetProjectDetail(string projectId)
        {
            var result = new ProjectDetailModel();

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            // Project info
            string projectQuery = @"
                SELECT projectid, name, description
                FROM Project
                WHERE projectid = @ProjectId;";

            await using (var cmd = new NpgsqlCommand(projectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@ProjectId", projectId);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    result.ProjectId = reader["projectid"].ToString();
                    result.Name = reader["name"].ToString();
                    result.Description = reader["description"].ToString();
                }
            }

            // Tasks (simple version)
            string taskQuery = @"
                SELECT taskid, title, status
                FROM Task
                WHERE projectid = @ProjectId;";

            await using (var cmd = new NpgsqlCommand(taskQuery, conn))
            {
                cmd.Parameters.AddWithValue("@ProjectId", projectId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Tasks.Add(new TaskModel
                    {
                        TaskId = reader["taskid"].ToString(),
                        Title = reader["title"].ToString(),
                        Status = reader["status"].ToString()
                    });
                }
            }

            return result;
        }

        // Update project
        public async Task<GeneralResponseModel> UpdateProject(
            string projectId, string name, string description, string currentUserId)
        {
            string query = @"
                UPDATE Project p
                SET name = @Name, description = @Desc
                WHERE p.projectid = @ProjectId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      WHERE om.orgid = p.orgid
                        AND om.userid = @UserId
                        AND om.role = 'Admin'
                        AND om.status = 'Active'
                  );";

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Desc", description ?? "");
            cmd.Parameters.AddWithValue("@UserId", currentUserId);

            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? new GeneralResponseModel { Success = true, Message = "Project updated" }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }

        // Assign project admin
        public async Task<GeneralResponseModel> AddProjectAdmin(
            string projectId, string userId, string currentUserId)
        {
            string query = @"
                INSERT INTO ProjectAdmin (projectid, userid, status)
                SELECT @ProjectId, @UserId, 'Active'
                WHERE EXISTS (
                    SELECT 1
                    FROM OrganizationMember om
                    JOIN Project p ON p.orgid = om.orgid
                    WHERE p.projectid = @ProjectId
                      AND om.userid = @CurrentUser
                      AND om.role = 'Admin'
                      AND om.status = 'Active'
                )
                ON CONFLICT (projectid, userid)
                DO UPDATE SET status = 'Active';
            ";

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CurrentUser", currentUserId);

            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? new GeneralResponseModel { Success = true, Message = "Project admin added" }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }

        // Remove project admin
        public async Task<GeneralResponseModel> RemoveProjectAdmin(
            string projectId, string userId, string currentUserId)
        {
            string query = @"
                UPDATE ProjectAdmin pa
                SET status = 'Inactive'
                WHERE pa.projectid = @ProjectId
                  AND pa.userid = @UserId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      JOIN Project p ON p.orgid = om.orgid
                      WHERE p.projectid = @ProjectId
                        AND om.userid = @CurrentUser
                        AND om.role = 'Admin'
                        AND om.status = 'Active'
                  );";

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CurrentUser", currentUserId);

            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? new GeneralResponseModel { Success = true, Message = "Project admin removed" }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }
    }
}
