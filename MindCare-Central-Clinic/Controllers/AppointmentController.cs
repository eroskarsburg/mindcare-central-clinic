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
        public AppointmentViewModel _model;
        private readonly IAppointmentService _service;
        private readonly IClientService _clientService;

        public AppointmentController(ILogger<AppointmentController> logger, AppointmentViewModel model, IAppointmentService service, IClientService clientService)
        {
            _logger = logger;
            _model = model;
            _service = service;
            _clientService = clientService;
        }

        public IActionResult Index()
        {
            _model.AppointmentList = _service.GetAppointments().Result;
            _model.ClientList = _clientService.GetClients().Result;

            return View(_model);
        }

        [HttpPost]
        public JsonResult Insert(Appointment appointment)
        {
            string message = "Agendamento inserido!";
            try
            {
                _service.InsertAppointment(appointment);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        [HttpPut]
        public JsonResult Update(Appointment appointment)
        {
            string message = "Agendamento atualizado!";
            try
            {
                _service.UpdateAppointment(appointment);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Agendamento deletado!";
            try
            {
                _service.DeleteAppointment(id, 0);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
