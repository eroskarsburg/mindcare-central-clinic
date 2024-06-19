using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        public PaymentViewModel _model;
        private readonly IPaymentService _service;

        public PaymentController(ILogger<PaymentController> logger, PaymentViewModel model, IPaymentService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        public IActionResult Index()
        {
            _model.PaymentList = _service.GetPayments().Result;

            return View(_model);
        }

        [HttpPost]
        public JsonResult Insert(Payment payment)
        {
            string message = "Pagamento inserido!";
            try
            {
                _service.InsertPayment(payment);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        [HttpPut]
        public JsonResult Update(Payment payment)
        {
            string message = "Pagamento atualizado!";
            try
            {
                _service.UpdatePayment(payment);
            }
            catch (Exception e) { message = e.Message; }

            return Json(new { message });
        }

        [HttpDelete]
        public JsonResult Delete(int id)
        {
            string message = "Pagamento deletado!";
            try
            {
                _service.DeletePayment(id);
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
