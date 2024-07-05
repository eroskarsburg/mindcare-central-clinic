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

        /// <summary>
        /// Initializes a new instance of the PaymentController class with the specified logger, model, and payment service.
        /// </summary>
        /// <param name="logger">The logger instance to log information.</param>
        /// <param name="model">The model representing the view data for the payment page.</param>
        /// <param name="service">The service for handling payments.</param>
        public PaymentController(ILogger<PaymentController> logger, PaymentViewModel model, IPaymentService service)
        {
            _logger = logger;
            _model = model;
            _service = service;
        }

        /// <summary>
        /// Handles the default action for the payment page. 
        /// Fetches the list of payments.
        /// </summary>
        /// <returns>A view populated with the model data.</returns>
        public IActionResult Index()
        {
            _model.PaymentList = _service.GetPayments().Result;
            Thread.Sleep(250);
            return View(_model);
        }

        /// <summary>
        /// Inserts a new payment and returns a JSON result indicating success or failure.
        /// </summary>
        /// <param name="payment">The payment to be inserted.</param>
        /// <returns>A JSON result with a message indicating the outcome.</returns>
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

        /// <summary>
        /// Updates an existing payment and returns a JSON result indicating success or failure.
        /// </summary>
        /// <param name="payment">The payment to be updated.</param>
        /// <returns>A JSON result with a message indicating the outcome.</returns>
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

        /// <summary>
        /// Deletes a payment by its ID and returns a JSON result indicating success or failure.
        /// </summary>
        /// <param name="id">The ID of the payment to be deleted.</param>
        /// <returns>A JSON result with a message indicating the outcome.</returns>
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
