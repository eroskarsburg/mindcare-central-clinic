using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare_Central_Clinic.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;

        public LoginController(ILogger<HomeController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public JsonResult Get(User user)
        {
            User login = new();
            string message = "Login efetuado!";
            try
            {
                login = _userService.GetUser(user).Result;
                if (login.Id == 0)
                {
                    message = "Credenciais inválidas! Tente novamente ou entre em contato com o suporte.";
                    throw new Exception(message);
                }

                
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message, login });
        }
    }
}
