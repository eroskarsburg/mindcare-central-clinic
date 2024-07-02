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

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="model">Client view model.</param>
        /// <param name="service">Client service instance.</param>
        public ClientController(ILogger<ClientController> logger, ClientViewModel model, IClientService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        /// <summary>
        /// Retrieves client list and returns the model to the view.
        /// </summary>
        /// <returns>The view with the model.</returns>
        public IActionResult Index()
        {
            _model.ClientList = _service.GetClients().Result;
            return View(_model);
        }

        /// <summary>
        /// Retrieves a list of clients and returns it as a JSON result with a message.
        /// </summary>
        /// <returns>A JSON result with a list of clients and a message.</returns>
        [HttpGet]
        public JsonResult Get()
        {
            List<Client> listClient = new List<Client>();
            string message = "Clientes encontrados!";
            try
            {
                listClient = _service.GetClients().Result;
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new
            {
                message,
                listClient,
            });
        }

        /// <summary>
        /// Inserts a new client and returns a JSON result with a message.
        /// </summary>
        /// <param name="client">The client to insert.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpPost]
        public JsonResult Insert(Client client)
        {
            string message = "Cliente inserido!";
            try
            {
                _service.InsertClient(client);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Updates an existing client and returns a JSON result with a message.
        /// </summary>
        /// <param name="client">The client to update.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpPut]
        public JsonResult Update(Client client)
        {
            string message = "Cliente atualizado!";
            try
            {
                _service.UpdateClient(client);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Deletes a client by ID and returns a JSON result with a message.
        /// </summary>
        /// <param name="id">The ID of the client to delete.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Cliente deletado!";
            try
            {
                _service.DeleteClient(id);
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
