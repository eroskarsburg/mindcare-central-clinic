using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class ProfessionalController : Controller
    {
        private readonly ILogger<ProfessionalController> _logger;
        public ProfessionalViewModel _model;
        private readonly IProfessionalService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfessionalController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="model">Professional view model.</param>
        /// <param name="service">Professional service instance.</param>
        public ProfessionalController(ILogger<ProfessionalController> logger, ProfessionalViewModel model, IProfessionalService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        /// <summary>
        /// Retrieves professional list and returns the model to the view.
        /// </summary>
        /// <returns>The view with the model.</returns>
        public IActionResult Index()
        {
            _model.ProfessionalList = _service.GetProfessionals().Result;
            return View(_model);
        }

        /// <summary>
        /// Inserts a new professional and returns a JSON result with a message.
        /// </summary>
        /// <param name="professional">The professional to insert.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpPost]
        public JsonResult Insert(Professional professional)
        {
            string message = "Profissional inserido!";
            try
            {
                _service.InsertProfessional(professional);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Updates an existing professional and returns a JSON result with a message.
        /// </summary>
        /// <param name="professional">The professional to update.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpPut]
        public JsonResult Update(Professional professional)
        {
            string message = "Profissional atualizado!";
            try
            {
                _service.UpdateProfessional(professional);
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return Json(new { message });
        }

        /// <summary>
        /// Deletes a professional by ID and returns a JSON result with a message.
        /// </summary>
        /// <param name="id">The ID of the professional to delete.</param>
        /// <returns>A JSON result with a message.</returns>
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Profissional deletado!";
            try
            {
                _service.DeleteProfessional(id);
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
