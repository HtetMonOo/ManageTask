using ManageTask.Models;
using Npgsql;

namespace ManageTask.Controllers.TaskAssignment
{
    public class TaskAssignmentService
    {
        private readonly IConfiguration _configuration;
        private string Conn => _configuration["ConnectionString"]!;

        public TaskAssignmentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Assign 
        public async Task<GeneralResponseModel> AssignTask(
            string taskId, string type, string assignId, string user)
        {
            try
            {
                string query = @"
                INSERT INTO TaskAssignment (taskid, assigntype, assignid, status)
                SELECT @TaskId, @Type, @AssignId, 'Active'
                WHERE EXISTS (
                    SELECT 1 FROM OrganizationMember om
                    JOIN Task t ON t.projectid = om.orgid
                    WHERE t.taskid = @TaskId
                      AND om.userid = @User
                      AND om.role = 'Admin'
                )
                ON CONFLICT (taskid, assigntype, assignid)
                DO UPDATE SET status = 'Active';";

                return await ExecuteAssign(query, taskId, type, assignId, user, "Assigned");
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }

        }

        // Unassign
        public async Task<GeneralResponseModel> UnassignTask(
            string taskId, string type, string assignId, string user)
        {
            try
            {
                string query = @"
                UPDATE TaskAssignment ta
                SET status = 'Inactive'
                WHERE ta.taskid = @TaskId
                  AND ta.assigntype = @Type
                  AND ta.assignid = @AssignId;";

                return await ExecuteAssign(query, taskId, type, assignId, user, "Unassigned");
            }
            catch (Exception e)
            {
                return new GeneralResponseModel { Success = false, Message = e.Message };
            }

        }

        private async Task<GeneralResponseModel> ExecuteAssign(
            string query, string taskId, string type, string assignId, string user, string msg)
        {
            await using var conn = new NpgsqlConnection(Conn);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@TaskId", taskId);
            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@AssignId", assignId);
            cmd.Parameters.AddWithValue("@User", user);

            return await cmd.ExecuteNonQueryAsync() > 0
                ? new GeneralResponseModel { Success = true, Message = msg }
                : new GeneralResponseModel { Success = false, Message = "Unauthorized" };
        }
    }
}
