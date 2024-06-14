using Microsoft.AspNetCore.Mvc;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using MindCare_Central_Clinic.Services.Abstractions;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class ClientController : Controller
    {
        private readonly ILogger<ClientController> _logger;
        public ClientViewModel _model;
        private readonly IClientService _service;
        private readonly IWebClientService _webClientService;

        public ClientController(ILogger<ClientController> logger, ClientViewModel model, IClientService service, IWebClientService webClientService)
        {
            _logger = logger;
            _model = model;
            _service = service;
            _webClientService = webClientService;
        }

        public IActionResult Index()
        {
            _model.ClientList = _service.GetClients().Result;

            return View(_model);
        }

        [HttpGet("Obter/{id:int}")]
        public async Task<IActionResult> Obter(int id)
        {
            try
            {
                return await _webClientService.Get(id);
            }
            catch (Exception e)
            {
                return StatusCode(500, "Ocorreu um erro inesperado ao tentar obter o projeto. Por favor, tente novamente.");
            }
        }

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

        public JsonResult Delete(int idClient)
        {
            string message = "Cliente deletado!";
            try
            {
                _service.DeleteClient(idClient);
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
