using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class ClientController : Controller
    {
        private readonly ILogger<ClientController> _logger;
        public ClientViewModel _model;
        private readonly IClientService _service;

        public ClientController(ILogger<ClientController> logger, ClientViewModel model, IClientService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        public IActionResult Index()
        {
            _model.ClientList = _service.GetClients().Result;

            return View(_model);
        }

        [HttpPost]
        public JsonResult Insert(Client client)
        {
            string message = "Cliente inserido!";
            try
            {
                _service.InsertClient(client);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        [HttpPut]
        public JsonResult Update(Client client)
        {
            string message = "Cliente atualizado!";
            try
            {
                _service.UpdateClient(client);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Cliente deletado!";
            try
            {
                _service.DeleteClient(id);
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
