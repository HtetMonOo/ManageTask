using ManageTask.Controllers.CipherHelper;
using ManageTask.Models;
using Npgsql;
using System.Security.Cryptography;

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
                string hashedToken = CipherHelperService.GenerateSecureTokenHashed();

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                string query = @"
                    INSERT INTO organization_invitation (orgid, email, token, status, expiredat)
                    SELECT 
                        @OrgId,
                        @Email,
                        @Token,
                        'Pending',
                        NOW() + INTERVAL '7 days'
                    WHERE EXISTS (
                        SELECT 1
                        FROM organization_member om
                        WHERE om.orgid = @OrgId
                          AND om.userid = @InviterId
                          AND om.role = 'Admin'
                          AND om.status = 'Active'
                    )
                    ON CONFLICT (orgid, email)
                    WHERE status = 'Pending'
                    DO UPDATE
                    SET expiredat = NOW() + INTERVAL '7 days';
                    ";
                var cmd = new NpgsqlCommand(query,conn);

                cmd.Parameters.AddWithValue("@OrgId", orgId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Token", hashedToken);
                cmd.Parameters.AddWithValue("@InviterId", inviterId);

                await cmd.ExecuteNonQueryAsync();

                return new GeneralResponseModel { Success = true, Message = "Invitation sent" };
            }
            catch
            {
                return new GeneralResponseModel { Success = false, Message = "Failed to invite user" };
            }
        }

        public async Task<GeneralResponseModel> AcceptInviation(string token)
        {
            try
            {
                string hashedToken = CipherHelperService.HashToken(token);

                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();

                await using var tx = await conn.BeginTransactionAsync();

                string query = @"
                    INSERT INTO organization_member (orgid, userid, role, status)
                    SELECT i.orgid, u.userid, i.role, 'Active'
                    FROM OrganizationInvitation i
                    JOIN ""user"" u ON u.email = i.email
                    WHERE i.token = @Token
                      AND i.status = 'Pending';
                    ";
                var memberCmd = new NpgsqlCommand(query, conn);

                memberCmd.Parameters.AddWithValue("@Token", hashedToken);
                await memberCmd.ExecuteNonQueryAsync();


                string inviteQuery = @"
                    UPDATE organization_invitation
                    SET status = 'Accepted'
                    WHERE token = @Token;
                    ";
                var inviteCmd = new NpgsqlCommand(inviteQuery,conn);

                inviteCmd.Parameters.AddWithValue("@Token", hashedToken);
                await inviteCmd.ExecuteNonQueryAsync();

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
                    UPDATE organization_invitation
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
