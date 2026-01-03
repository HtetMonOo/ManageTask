using ManageTask.Models;
using ManageTask.Models.Comment;
using Npgsql;

namespace ManageTask.Controllers.Comment
{
    public class CommentService
    {
        private readonly IConfiguration _configuration;
        private string Conn => _configuration["ConnectionString"]!;

        public CommentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Add comment
        public async Task<GeneralResponseModel> AddComment(string taskId, string content, string userId)
        {
            try
            {
                string query = @"
                    INSERT INTO TaskComment (commentid, taskid, userid, content, status, createdat)
                    SELECT @Id, @TaskId, @UserId, @Content, 'Active', NOW()
                    WHERE EXISTS (
                        SELECT 1 FROM Task t
                        JOIN Project p ON p.projectid = t.projectid
                        JOIN OrganizationMember om ON om.orgid = p.orgid
                        WHERE t.taskid = @TaskId
                          AND om.userid = @UserId
                          AND om.status = 'Active'
                    );";

                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@TaskId", taskId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Content", content);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Comment added" }
                    : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GeneralResponseModel { Success = false, Message = "Error adding comment" };
            }
        }

        // List comments
        public async Task<List<CommentModel>> ListComments(string taskId)
        {
            var list = new List<CommentModel>();

            try
            {
                string query = @"
                    SELECT commentid, taskid, userid, content, status, createdat, updatedat
                    FROM TaskComment
                    WHERE taskid = @TaskId
                      AND status = 'Active'
                    ORDER BY createdat;";

                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TaskId", taskId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new CommentModel
                    {
                        CommentId = reader["commentid"].ToString(),
                        TaskId = reader["taskid"].ToString(),
                        UserId = reader["userid"].ToString(),
                        Content = reader["content"].ToString(),
                        Status = reader["status"].ToString(),
                        CreatedAt = (DateTime)reader["createdat"],
                        UpdatedAt = reader["updatedat"] as DateTime?
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return list;
        }

        // Delete own comment
        public async Task<GeneralResponseModel> DeleteComment(string commentId, string userId)
        {
            try
            {
                string query = @"
                    UPDATE TaskComment
                    SET status = 'Inactive'
                    WHERE commentid = @CommentId
                      AND userid = @UserId;";

                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CommentId", commentId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Comment deleted" }
                    : new GeneralResponseModel { Success = false, Message = "Not allowed" };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GeneralResponseModel { Success = false, Message = "Error deleting comment" };
            }
        }

        // Edit own comment
        public async Task<GeneralResponseModel> EditComment(string commentId, string content, string userId)
        {
            try
            {
                string query = @"
                    UPDATE TaskComment
                    SET content = @Content,
                        updatedat = NOW()
                    WHERE commentid = @CommentId
                      AND userid = @UserId
                      AND status = 'Active';";

                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CommentId", commentId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Content", content);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Comment updated" }
                    : new GeneralResponseModel { Success = false, Message = "Not allowed" };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GeneralResponseModel { Success = false, Message = "Error editing comment" };
            }
        }
    }
}
