using ManageTask.Models;
using ManageTask.Models.Task;
using Npgsql;

namespace ManageTask.Controllers.Task
{
    public class TaskService
    {
        private readonly IConfiguration _configuration;
        private string Conn => _configuration["ConnectionString"]!;

        public TaskService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Create task
        public async Task<GeneralResponseModel> CreateTask(TaskModel task, string currentUserId)
        {
            try
            {
                task.TaskId = Guid.NewGuid().ToString();

                string query = @"
                INSERT INTO Task (taskid, projectid, parenttaskid, title, description, deadline, status, createdat)
                SELECT @Id, @ProjectId, @ParentId, @Title, @Desc, @Deadline, 'Pending', NOW()
                WHERE EXISTS (
                    SELECT 1 FROM OrganizationMember om
                    JOIN Project p ON p.orgid = om.orgid
                    WHERE p.projectid = @ProjectId
                      AND om.userid = @User
                      AND om.role IN ('Admin')
                      AND om.status = 'Active'
                );";

                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", task.TaskId);
                cmd.Parameters.AddWithValue("@ProjectId", task.ProjectId);
                cmd.Parameters.AddWithValue("@ParentId", (object?)task.ParentTaskId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Title", task.Title);
                cmd.Parameters.AddWithValue("@Desc", task.Description ?? "");
                cmd.Parameters.AddWithValue("@Deadline", task.Deadline);
                cmd.Parameters.AddWithValue("@User", currentUserId);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new GeneralResponseModel { Success = true, Message = "Task created" }
                    : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }
        }

        // Delete task
        public async Task<GeneralResponseModel> DeleteTask(string taskId, string currentUserId)
        {
            try
            {
                string query = @"
                UPDATE Task t
                SET status = 'Inactive'
                WHERE t.taskid = @TaskId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      JOIN Project p ON p.orgid = om.orgid
                      WHERE p.projectid = t.projectid
                        AND om.userid = @User
                        AND om.role = 'Admin'
                  );";

                return await ExecuteSimple(query, taskId, currentUserId, "Task deleted");
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }
            
        }

        // Update task info
        public async Task<GeneralResponseModel> UpdateTask(
            string taskId, string title, string description, DateOnly deadline, string user)
        {
            try
            {
                string query = @"
                UPDATE Task t
                SET title = @Title,
                    description = @Desc,
                    deadline = @Deadline,
                    updatedat = NOW(),
                    updatedby = @User
                WHERE t.taskid = @TaskId
                  AND EXISTS (
                      SELECT 1 FROM OrganizationMember om
                      JOIN Project p ON p.orgid = om.orgid
                      WHERE p.projectid = t.projectid
                        AND om.userid = @User
                        AND om.role = 'Admin'
                  );";

                return await ExecuteSimple(query, taskId, user, "Task updated",
                    ("@Title", title),
                    ("@Desc", description),
                    ("@Deadline", deadline));
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }

        }

        // Toggle status
        public async Task<GeneralResponseModel> ToggleTaskStatus(string taskId, string user)
        {
            try
            {
                string query = @"
                UPDATE Task t
                SET status = CASE WHEN status = 'Pending' THEN 'Done' ELSE 'Pending' END,
                    updatedat = NOW(),
                    updatedby = @User
                WHERE t.taskid = @TaskId
                  AND EXISTS (
                      SELECT 1 FROM TaskAssignment ta
                      WHERE ta.taskid = t.taskid
                        AND ta.status = 'Active'
                        AND (
                            (ta.assigntype = 'User' AND ta.assignid = @User)
                            OR
                            (ta.assigntype = 'Team' AND ta.assignid IN (
                                SELECT teamid FROM TeamMember
                                WHERE userid = @User AND status = 'Active'
                            ))
                        )
                  );";

                return await ExecuteSimple(query, taskId, user, "Task status updated");
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }
            
        }

        // List by project
        public async Task<List<TaskModel>> ListTasksByProject(string projectId)
        {
            var list = new List<TaskModel>();
            try
            {
                string query = "SELECT * FROM Task WHERE projectid = @ProjectId AND status != 'Inactive';";

                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", projectId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapTask(reader));
                }

                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return list;
            }
            
            
        }

        // Task detail
        public async Task<TaskModel> GetTaskDetail(string taskId)
        {
            try
            {
                string query = "SELECT * FROM Task WHERE taskid = @Id;";
                await using var conn = new NpgsqlConnection(Conn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", taskId);

                await using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapTask(reader) : null;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            
        }

        

        // Helpers
        private TaskModel MapTask(NpgsqlDataReader r) => new()
        {
            TaskId = r["taskid"].ToString(),
            Title = r["title"].ToString(),
            Description = r["description"].ToString(),
            ProjectId = r["projectid"].ToString(),
            ParentTaskId = r["parenttaskid"]?.ToString(),
            Deadline = DateOnly.FromDateTime((DateTime)r["deadline"]),
            Status = r["status"].ToString(),
            UpdatedBy = r["updatedby"]?.ToString(),
            CreatedAt = (DateTime)r["createdat"],
            UpdatedAt = (DateTime?)r["updatedat"] ?? DateTime.MinValue
        };

        private async Task<GeneralResponseModel> ExecuteSimple(
            string query, string taskId, string user, string msg, params (string, object)[] extra)
        {
            await using var conn = new NpgsqlConnection(Conn);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@TaskId", taskId);
            cmd.Parameters.AddWithValue("@User", user);
            foreach (var (k, v) in extra)
                cmd.Parameters.AddWithValue(k, v);

            return await cmd.ExecuteNonQueryAsync() > 0
                ? new GeneralResponseModel { Success = true, Message = msg }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }

        
    }
}
