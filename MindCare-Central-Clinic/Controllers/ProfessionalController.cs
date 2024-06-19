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

        public ProfessionalController(ILogger<ProfessionalController> logger, ProfessionalViewModel model, IProfessionalService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        public IActionResult Index()
        {
            _model.ProfessionalList = _service.GetProfessionals().Result;

            return View(_model);
        }

        [HttpPost]
        public JsonResult Insert(Professional professional)
        {
            string message = "Profissional inserido!";
            try
            {
                _service.InsertProfessional(professional);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message = message });
        }

        [HttpPut]
        public JsonResult Update(Professional professional)
        {
            string message = "Profissional atualizado!";
            try
            {
                _service.UpdateProfessional(professional);
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
                _service.DeleteProfessional(id);
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
