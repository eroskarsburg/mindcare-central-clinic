using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ILogger<AppointmentController> _logger;
        private readonly AppointmentViewModel _model;
        private readonly IAppointmentService _service;
        private readonly IClientService _clientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppointmentController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="model">Appointment view model.</param>
        /// <param name="service">Appointment service instance.</param>
        /// <param name="clientService">Client service instance.</param>
        public AppointmentController(ILogger<AppointmentController> logger, AppointmentViewModel model, IAppointmentService service, IClientService clientService)
        {
            _logger = logger;
            _model = model;
            _service = service;
            _clientService = clientService;
        }

        /// <summary>
        /// Retrieves appointment and client lists and returns the model to the view.
        /// </summary>
        /// <returns>The view with the model.</returns>
        public IActionResult Index()
        {
            _model.AppointmentList = _service.GetAppointments().Result;
            _model.ClientList = _clientService.GetClients().Result;
            return View(_model);
        }

        /// <summary>
        /// Inserts a new appointment and returns a JSON result with a message.
        /// </summary>
        /// <param name="appointment">The appointment to insert.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpPost]
        public JsonResult Insert(Appointment appointment)
        {
            string message = "Agendamento inserido!";
            try
            {
                _service.InsertAppointment(appointment);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Updates an existing appointment and returns a JSON result with a message.
        /// </summary>
        /// <param name="appointment">The appointment to update.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpPut]
        public JsonResult Update(Appointment appointment)
        {
            string message = "Agendamento atualizado!";
            try
            {
                _service.UpdateAppointment(appointment);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Deletes an appointment by ID and returns a JSON result with a message.
        /// </summary>
        /// <param name="id">The ID of the appointment to delete.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Agendamento deletado!";
            try
            {
                _service.DeleteAppointment(id, 0);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Returns an error view with the request ID for debugging purposes.
        /// </summary>
        /// <returns>The error view with the request ID.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
