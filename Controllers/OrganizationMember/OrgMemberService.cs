using ManageTask.Models;
using Npgsql;
using System.Xml.Linq;

namespace ManageTask.Controllers.OrganizationMember
{
    public class OrgMemberService
    {
        private readonly IConfiguration _configuration;

        public OrgMemberService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnStr => _configuration["ConnectionString"]!;

        
        // Invite user (email-based)
        public async Task<GeneralResponseModel> InviteUser(string orgId, string email, string inviterId)
        {
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

        public async Task<GeneralResponseModel> AcceptInviation(string orgId, string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var tx = await conn.BeginTransactionAsync();

                string query = @"
                    UPDATE OrganizationInvite
                    SET status = 'Accepted'
                    WHERE orgid = @OrgId
                      AND userid = @UserId;
                    ";
                var inviteCmd = new NpgsqlCommand(query, conn);

                inviteCmd.Parameters.AddWithValue("@OrgId", orgId);
                inviteCmd.Parameters.AddWithValue("@UserId", userId);

                await inviteCmd.ExecuteNonQueryAsync();

                var memberCmd = new NpgsqlCommand(
                    "INSERT INTO OrganizationMember (orgid, userid, role, status) VALUES (@OrgId, @UserId, 'Member', 'Active')",
                    conn);

                memberCmd.Parameters.AddWithValue("@OrgId", orgId);
                memberCmd.Parameters.AddWithValue("@UserId", userId);
                await memberCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return new GeneralResponseModel { Success = true, Message = "Invitation is accepted successfully." };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Error accepting" };
            }
        }

        public async Task<GeneralResponseModel> DeclineInvitation(string orgId, string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();


                string query = @"
                    UPDATE OrganizationInvite
                    SET status = 'Declined'
                    WHERE orgid = @OrgId
                      AND userid = @UserId;
                    ";
                var inviteCmd = new NpgsqlCommand(query, conn);

                inviteCmd.Parameters.AddWithValue("@OrgId", orgId);
                inviteCmd.Parameters.AddWithValue("@UserId", userId);

                await inviteCmd.ExecuteNonQueryAsync();

                

                return new GeneralResponseModel { Success = true, Message = "Invitation is declined successfully." };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Error declining." };
            }
        }


        public async Task<GeneralResponseModel> RemoveOrgMember(string orgId, string userId, string currentUserId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                string query = @"
                    UPDATE OrganizationMember o
                    SET status = 'Inactive'
                    WHERE o.orgid = @OrgId
                      AND o.userid = @UserId
                      
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

                return new GeneralResponseModel { Success = true, Message = "Member removed successfully." };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Removing member failed" };
            }
        }

        // Update role
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
    }
}
