using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        private readonly IProfessionalService _professionalService;
        private readonly IHttpContextAccessor _contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="userService">User service instance.</param>
        /// <param name="professionalService">Professional service instance.</param>
        /// <param name="contextAccessor">HTTP context accessor instance.</param>
        public LoginController(ILogger<HomeController> logger, IUserService userService, IProfessionalService professionalService, IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _userService = userService;
            _professionalService = professionalService;
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Clears session data and returns the login view.
        /// </summary>
        /// <returns>The login view.</returns>
        public IActionResult Index()
        {
            try
            {
                _contextAccessor.HttpContext.Session.Remove("Name");
                _contextAccessor.HttpContext.Session.Remove("UserId");
                _contextAccessor.HttpContext.Session.Remove("AccessLevel");
            }
            catch { }

            return View();
        }

        /// <summary>
        /// Authenticates the user and sets session data, returns a JSON result with a message.
        /// </summary>
        /// <param name="user">The user credentials for authentication.</param>
        /// <returns>A JSON result with a message.</returns>
        public JsonResult Get(User user)
        {
            string message = "Login efetuado!";
            try
            {
                var sUser = _userService.GetUser(user).Result;
                if (sUser.Id == 0)
                {
                    message = "Credenciais inválidas! Tente novamente ou entre em contato com o suporte.";
                    throw new Exception(message);
                }
                var sProf = _professionalService.GetProfessional(sUser.Id).Result;

                _contextAccessor.HttpContext.Session.SetString("Name", string.IsNullOrEmpty(sProf.Name) ? sUser.Username : sProf.Name);
                _contextAccessor.HttpContext.Session.SetInt32("UserId", sUser.Id);
                _contextAccessor.HttpContext.Session.SetInt32("AccessLevel", (int)sUser.AccessLevel);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }
    }

}
