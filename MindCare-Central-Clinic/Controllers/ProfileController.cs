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

        /// <summary>
        /// Initializes a new instance of the ProfileController class with the specified profile view model, user service, professional service, and HTTP context accessor.
        /// </summary>
        /// <param name="model">The model representing the view data for the profile page.</param>
        /// <param name="userService">The service for handling user-related operations.</param>
        /// <param name="professionalService">The service for handling professional-related operations.</param>
        /// <param name="contextAccessor">The accessor for accessing the HTTP context.</param>
        public ProfileController(ProfileViewModel model, IUserService userService, IProfessionalService professionalService, IHttpContextAccessor contextAccessor)
        {
            _model = model;
            _userService = userService;
            _professionalService = professionalService;
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Handles the default action for the profile page.
        /// Fetches user and professional data based on the current session user ID.
        /// </summary>
        /// <returns>A view populated with the model data.</returns>
        public IActionResult Index()
        {
            _model.User = _userService.GetUser(id: (int)_contextAccessor.HttpContext.Session.GetInt32("UserId")).Result;
            _model.Professional = _professionalService.GetProfessional((int)_contextAccessor.HttpContext.Session.GetInt32("UserId")).Result;
            Thread.Sleep(250);
            return View(_model);
        }

    }
}
