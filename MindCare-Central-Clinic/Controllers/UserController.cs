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

        /// <summary>
        /// Initializes a new instance of the UserController class with the specified logger, model, and user service.
        /// </summary>
        /// <param name="logger">The logger instance to log information.</param>
        /// <param name="model">The model representing the view data for the user page.</param>
        /// <param name="service">The service for handling user-related operations.</param>
        public UserController(ILogger<UserController> logger, UserViewModel model, IUserService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        /// <summary>
        /// Handles the default action for the user page. 
        /// Fetches the list of users.
        /// </summary>
        /// <returns>A view populated with the model data.</returns>
        public IActionResult Index()
        {
            _model.UserList = _service.GetUsers().Result;
            Thread.Sleep(250);
            return View(_model);
        }

        /// <summary>
        /// Inserts a new user and returns a JSON result indicating success or failure.
        /// </summary>
        /// <param name="user">The user to be inserted.</param>
        /// <returns>A JSON result with a message indicating the outcome.</returns>
        [HttpPost]
        public JsonResult Insert(User user)
        {
            string message = "Usuário inserido!";
            try
            {
                _service.InsertUser(user);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        /// <summary>
        /// Updates an existing user and returns a JSON result indicating success or failure.
        /// </summary>
        /// <param name="user">The user to be updated.</param>
        /// <returns>A JSON result with a message indicating the outcome.</returns>
        [HttpPut]
        public JsonResult Update(User user)
        {
            string message = "Usuário atualizado!";
            try
            {
                _service.UpdateUser(user);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        /// <summary>
        /// Deletes a user by its ID and returns a JSON result indicating success or failure.
        /// </summary>
        /// <param name="id">The ID of the user to be deleted.</param>
        /// <returns>A JSON result with a message indicating the outcome.</returns>
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Usuário deletado!";
            try
            {
                _service.DeleteUser(id);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        /// <summary>
        /// Handles errors and returns an error view with the current request ID.
        /// </summary>
        /// <returns>A view for the error page.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
