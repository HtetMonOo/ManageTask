using ManageTask.Models.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageTask.Controllers.Account
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("/signin")]
        public IActionResult SignInFormView(string email, string password, string error)
        {
            ViewBag.Password = password;
            ViewBag.Email = email;
            ViewBag.Error = error;
            return View("~/Views/Account/Signin.cshtml");
        }

        [HttpPost("/signin/verify")]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return RedirectToAction("SignInFormView", new { email, password, error = "Password is required!" });
            }

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("SignInFormView", new { email, password, error = "Email is required!" });
            }

            UserVerifyResponse result = await _accountService.VerifyUser(password, email);
            if (!result.Success)
            {
                return RedirectToAction("SignInFormView", new { email, password, error = result.Message });
            }

            UserModel user = result.UserData;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            };

            var identity = new ClaimsIdentity(claims, "ManageTaskCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("ManageTaskCookie", principal);

            return RedirectToAction("MainDashboardView", "Dashboard");
        }

        [HttpPost("/signout")]
        public async Task<IActionResult> SignOutAction()
        {
            await HttpContext.SignOutAsync("ManageTaskCookie");
            return RedirectToAction("SignInFormView");
        }


        [HttpGet("/access-denied")]
        public IActionResult AccessDeniedView()
        { 
            return View("~/Views/Account/AccessDenied.cshtml");
        }
    }
}
