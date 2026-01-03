using ManageTask.Controllers.Account;
using ManageTask.Controllers.Comment;
using ManageTask.Controllers.Organization;
using ManageTask.Controllers.OrganizationMember;
using ManageTask.Controllers.Project;
using ManageTask.Controllers.SendEmail;
using ManageTask.Controllers.Task;
using ManageTask.Controllers.TaskAssignment;
using ManageTask.Controllers.Team;
using ManageTask.Controllers.TeamMember;
using ManageTask.Controllers.User;

namespace ManageTask
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<OrganizationService>();
            builder.Services.AddScoped<OrgMemberService>();
            builder.Services.AddScoped<TeamService>();
            builder.Services.AddScoped<TeamMemberService>();
            builder.Services.AddScoped<ProjectService>();
            builder.Services.AddScoped<TaskService>();
            builder.Services.AddScoped<TaskAssignmentService>();
            builder.Services.AddScoped<CommentService>();

            builder.Services.AddAuthentication("ManageTaskCookie")
                .AddCookie("ManageTaskCookie", options =>
                {
                    options.LoginPath = "/signin";
                    options.AccessDeniedPath = "/access-denied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
