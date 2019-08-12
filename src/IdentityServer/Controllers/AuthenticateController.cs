using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthenticateController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;

        public AuthenticateController(IIdentityServerInteractionService interaction)
        {
            _interaction = interaction;
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string ReturnUrl { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody]LoginRequest request)
        {
            var context = await _interaction.GetAuthorizationContextAsync(request.ReturnUrl);
            var user = Config.GetTestUsers()
                   .FirstOrDefault(usr => usr.Password == request.Password && usr.Username == request.Username);

            if (user != null && context != null)
            {
                await HttpContext.SignInAsync(user.SubjectId, user.Username);
                return new JsonResult(new { RedirectUrl = request.ReturnUrl, IsOk = true });
            }

            return Unauthorized();
        }
    }
}
