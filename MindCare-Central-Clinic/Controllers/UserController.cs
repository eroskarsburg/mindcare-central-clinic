using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        public UserViewModel _model;
        private readonly IUserService _service;

        public UserController(ILogger<UserController> logger, UserViewModel model, IUserService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        public IActionResult Index()
        {
            _model.UserList = _service.GetUsers().Result;

            return View(_model);
        }

        [HttpPost]
        public JsonResult Insert(User user)
        {
            string message = "Profissional inserido!";
            try
            {
                _service.InsertUser(user);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        [HttpPut]
        public JsonResult Update(User user)
        {
            string message = "Profissional atualizado!";
            try
            {
                _service.UpdateUser(user);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Profissional deletado!";
            try
            {
                _service.DeleteUser(id);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
