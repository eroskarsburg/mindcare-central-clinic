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

        public LoginController(ILogger<HomeController> logger, IUserService userService, IProfessionalService professionalService, IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _userService = userService;
            _professionalService = professionalService;
            _contextAccessor = contextAccessor;
        }

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
