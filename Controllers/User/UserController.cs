using ManageTask.Models;
using ManageTask.Models.User;
using Microsoft.AspNetCore.Mvc;

namespace ManageTask.Controllers.User
{
    public class UserController : Controller
    {
        private readonly UserService userService;
        public UserController(UserService userService)
        {
            this.userService = userService;
        }

        [Route("/user/create/form")]
        public IActionResult UserCreateFormView(string error)
        {
            ViewBag.Error = error;
            return View("~/Views/User/UserCreateForm.cshtml");
        }

        public IActionResult VerifyEmailFormView(string email, string processId, string error)
        {
            ViewBag.ProcessId = processId;
            ViewBag.Email = email;
            ViewBag.Error = error;
            return View("~/Views/User/VerifyEmail.cshtml");
        }

        public IActionResult SetPasswordView(string email, string error)
        {
            ViewBag.Email = email;
            ViewBag.Error = error;
            return View("~/Views/User/PasswordSet.cshtml");
        }

        [HttpPost("/user/register")]
        public async Task<IActionResult> RegisterUser(string username, string email)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("UserCreateFormView", new { error = "Username is required!" });
            }

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("UserCreateFormView", new { error = "Email is required!" });
            }

            bool isEmailExisting = await userService.CheckExistingEmail(email);
            if (isEmailExisting)
            {
                return RedirectToAction("UserCreateFormView", new { error = "This email is already registered!" });
            }

            string processId = await userService.SendVerificationCode(email);
            if (string.IsNullOrEmpty(processId))
            {
                return RedirectToAction("UserCreateFormView", new { error = "Error sending verification code to your mail!" });
            }


            return RedirectToAction("VerifyEmailFormView", new { email, processId });

        }

        [HttpPost("/user/email/verify")]
        public async Task<IActionResult> VerifyEmail(string processId, string email, string code)
        {
            if (string.IsNullOrEmpty(processId))
            {
                return RedirectToAction("VerifyEmailFormView", new { email, processId, error = "Process ID is missing!" });
            }

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("VerifyEmailFormView", new { email, processId, error = "Email is missing!" });
            }

            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("VerifyEmailFormView", new { email, processId, error = "Verification Code is required!" });
            }

            if (code.Length != 6)
            {
                return RedirectToAction("VerifyEmailFormView", new { email, processId, error = "Invalid Verification Code length!" });
            }

            GeneralResponseModel result = await userService.CheckEmailVerification(processId, email, code);
            if (!result.Success)
            {
                return RedirectToAction("VerifyEmailFormView", new { email, processId, error = result.Message });
            }

            return RedirectToAction("SetPasswordView", new { email });


        }

        [HttpPost("user/create")]
        public async Task<IActionResult> AddUser([FromBody] UserModel user)
        {
            if(user == null)
            {
                return Ok("user cannot be null!");
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Ok("Email is required.");
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                return Ok("Password is required.");
            }

            int affectedRows = await userService.SaveUser(user);
            if(affectedRows == 1)
            {
                return Ok("User created successfully");
            }

            return Ok("Something went wrong!");
        }

        [HttpPost("user/update")]
        public async Task<IActionResult> UpdateUser([FromBody] UserModel user)
        {
            if (user == null)
            {
                return Ok("user cannot be null!");
            }

            if (string.IsNullOrEmpty(user.UserId))
            {
                return Ok("Userid is required.");
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Ok("Email is required.");
            }


            int affectedRows = await userService.UpdateUser(user);
            if (affectedRows == 1)
            {
                return Ok("User info updated successfully");
            }

            return Ok("Something went wrong!");
        }

        [HttpPost("user/password/change")]
        public async Task<IActionResult> ChangePassword(string userid, string password)
        {
            

            if (string.IsNullOrEmpty(userid))
            {
                return Ok("Userid is required.");
            }

            if (string.IsNullOrEmpty(password))
            {
                return Ok("Password is required.");
            }


            int affectedRows = await userService.ChangePassword(userid, password);
            if (affectedRows == 1)
            {
                return Ok("Password is changed successfully");
            }

            return Ok("Something went wrong!");
        }
    }
}
