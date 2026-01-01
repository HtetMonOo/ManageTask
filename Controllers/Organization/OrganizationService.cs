using ManageTask.Models;
using ManageTask.Models.Organization;
using Npgsql;

namespace ManageTask.Controllers.Organization
{
    public class OrganizationService
    {
        private readonly IConfiguration _configuration;

        public OrganizationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnStr => _configuration["ConnectionString"]!;

        // 1. Create organization
        public async Task<GeneralResponseModel> CreateOrganization(string name, string description, string creatorId)
        {
            string orgId = Guid.NewGuid().ToString();

            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var tx = await conn.BeginTransactionAsync();

                var orgCmd = new NpgsqlCommand(
                    "INSERT INTO Organization (orgid, name, description, createdby, status) VALUES (@Id, @Name, @Description, @Creator, 'Active')",
                    conn);

                orgCmd.Parameters.AddWithValue("@Id", orgId);
                orgCmd.Parameters.AddWithValue("@Name", name);
                orgCmd.Parameters.AddWithValue("@Description", description);
                orgCmd.Parameters.AddWithValue("@Creator", creatorId);
                await orgCmd.ExecuteNonQueryAsync();

                var memberCmd = new NpgsqlCommand(
                    "INSERT INTO OrganizationMember (orgid, userid, role, status) VALUES (@OrgId, @UserId, 'Admin', 'Active')",
                    conn);

                memberCmd.Parameters.AddWithValue("@OrgId", orgId);
                memberCmd.Parameters.AddWithValue("@UserId", creatorId);
                await memberCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return new GeneralResponseModel { Success = true, Message = "Organization created" };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Error creating organization" };
            }
        }

        // 2. Toggle status
        public async Task<GeneralResponseModel> ToggleOrganizationStatus(string orgId, string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                string query = @"
                    UPDATE Organization o
                    SET status = CASE WHEN status = 'Active' THEN 'Inactive' ELSE 'Active' END
                    WHERE o.orgid = @OrgId
                        AND o.status = 'Active'
                        AND EXISTS (
                            SELECT 1
                            FROM OrganizationMember om
                            WHERE om.orgid = o.orgid
                            AND om.userid = @UserId
                            AND om.role = 'Admin'
                            AND om.status = 'Active'
                        );";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@OrgId", orgId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    return new GeneralResponseModel
                    {
                        Success = false,
                        Message = "You are not authorized or this operation is not allowed."
                    };
                }

                return new GeneralResponseModel { Success = true, Message = "Organization status updated" };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Failed to update status" };
            }
        }

        // 3. Get orgs for user
        public async Task<List<OrganizationModel>> GetOrganizationsForUser(string userId)
        {
            var list = new List<OrganizationModel>();

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            string query = @"
                SELECT o.orgid, o.name
                FROM Organization o
                JOIN OrganizationMember om ON o.orgid = om.orgid
                WHERE om.userid = @UserId AND o.status = 'Active'";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new OrganizationModel
                {
                    OrgId = reader["orgid"].ToString(),
                    Name = reader["name"].ToString()
                });
            }

            return list;
        }

        // 4. Organization details
        public async Task<OrganizationDetailModel> GetOrganizationDetail(string orgId)
        {
            var result = new OrganizationDetailModel();

            await using var conn = new NpgsqlConnection(ConnStr);
            await conn.OpenAsync();

            // Member count
            var memberCmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM OrganizationMember WHERE orgid = @OrgId AND status = 'Active'",
                conn);

            memberCmd.Parameters.AddWithValue("@OrgId", orgId);
            result.MemberCount = Convert.ToInt32(await memberCmd.ExecuteScalarAsync());

            // Projects + task count
            var projectCmd = new NpgsqlCommand(@"
                SELECT p.name, COUNT(t.taskid) AS taskcount
                FROM Project p
                LEFT JOIN Task t ON p.projectid = t.projectid
                WHERE p.orgid = @OrgId
                GROUP BY p.name", conn);

            projectCmd.Parameters.AddWithValue("@OrgId", orgId);

            await using var reader = await projectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Projects.Add(new ProjectSummaryModel
                {
                    ProjectName = reader["name"].ToString(),
                    TaskCount = Convert.ToInt32(reader["taskcount"])
                });
            }

            return result;
        }

        // 5. Update organization info
        public async Task<GeneralResponseModel> UpdateOrganization(string orgId, string name, string description, string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                string query = @"
                    UPDATE Organization o
                    SET name = @Name,
                        description = @Description,
                        updatedat = NOW()
                    WHERE o.orgid = @OrgId
                        AND o.status = 'Active'
                        AND EXISTS (
                            SELECT 1
                            FROM OrganizationMember om
                            WHERE om.orgid = o.orgid
                            AND om.userid = @UserId
                            AND om.role = 'Admin'
                            AND om.status = 'Active'
                        );
                    ";
                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.Parameters.AddWithValue("@OrgId", orgId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    return new GeneralResponseModel
                    {
                        Success = false,
                        Message = "You are not authorized or this operation is not allowed."
                    };
                }

                return new GeneralResponseModel { Success = true, Message = "Organization updated" };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Update failed" };
            }
        }

        // 6. Update role
        public async Task<GeneralResponseModel> UpdateOrgRole(string orgId, string userId, string role, string currentUserId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                string query = @"
                    UPDATE OrganizationMember o
                    SET role = @Role
                    WHERE o.orgid = @OrgId
                      AND o.userid = @UserId
                      AND (
                          @Role != 'Member'
                          OR (
                              SELECT COUNT(*)
                              FROM OrganizationMember
                              WHERE orgid = @OrgId
                                AND role = 'Admin'
                                AND status = 'Active'
                          ) > 1
                      )
                      AND EXISTS (
                          SELECT 1
                          FROM OrganizationMember om
                          WHERE om.orgid = o.orgid
                            AND om.userid = @CurrentUserId
                            AND om.role = 'Admin'
                            AND om.status = 'Active'
                      );
                    ";
                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@OrgId", orgId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CurrentUserId", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    return new GeneralResponseModel
                    {
                        Success = false,
                        Message = "You are not authorized or this operation is not allowed."
                    };
                }

                return new GeneralResponseModel { Success = true, Message = "Role updated" };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Role update failed" };
            }
        }

        // 7. Invite user (email-based)
        public async Task<GeneralResponseModel> InviteUser(string orgId, string email, string inviterId)
        {
            // Simplified: store invitation only
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(
                    "INSERT INTO OrganizationInvite (orgid, email, invitedby, status) VALUES (@OrgId, @Email, @Inviter, 'Pending')",
                    conn);

                cmd.Parameters.AddWithValue("@OrgId", orgId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Inviter", inviterId);

                await cmd.ExecuteNonQueryAsync();

                return new GeneralResponseModel { Success = true, Message = "Invitation sent" };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Failed to invite user" };
            }
        }
    }
}
