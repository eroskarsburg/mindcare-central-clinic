using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;

namespace MindCare_Central_Clinic.Controllers
{
    public class ProfileController : Controller
    {
        public ProfileViewModel _model;
        private readonly IUserService _userService;
        private readonly IProfessionalService _professionalService;
        private readonly IHttpContextAccessor _contextAccessor;

        public ProfileController(ProfileViewModel model, IUserService userService, IProfessionalService professionalService, IHttpContextAccessor contextAccessor)
        {
            _model = model;
            _userService = userService;
            _professionalService = professionalService;
            _contextAccessor = contextAccessor;
        }

        public IActionResult Index()
        {
            _model.User = _userService.GetUser(id: (int)_contextAccessor.HttpContext.Session.GetInt32("UserId")).Result;
            _model.Professional = _professionalService.GetProfessional((int)_contextAccessor.HttpContext.Session.GetInt32("UserId")).Result;

            return View(_model);
        }
    }
}
