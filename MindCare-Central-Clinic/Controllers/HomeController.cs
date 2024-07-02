using Microsoft.AspNetCore.Mvc;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Services.IServices;
using MindCare_Central_Clinic.Models;
using System.Diagnostics;

namespace MindCare_Central_Clinic.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public HomeViewModel _model;
        private readonly IAppointmentService _appointmentService;
        private readonly IPaymentService _paymentService;
        private readonly IClientRepository _clientRepository;

        /// <summary>
        /// Initializes a new instance of the HomeController class with the specified logger, model, appointment service, payment service, and client repository.
        /// </summary>
        /// <param name="logger">The logger instance to log information.</param>
        /// <param name="model">The model representing the view data for the home page.</param>
        /// <param name="appointmentService">The service for handling appointments.</param>
        /// <param name="paymentService">The service for handling payments.</param>
        /// <param name="clientRepository">The repository for accessing client data.</param>
        public HomeController(ILogger<HomeController> logger, HomeViewModel model, IAppointmentService appointmentService, IPaymentService paymentService, IClientRepository clientRepository)
        {
            _logger = logger;
            _model = model;
            _appointmentService = appointmentService;
            _paymentService = paymentService;
            _clientRepository = clientRepository;
        }

        /// <summary>
        /// Handles the default action for the home page. 
        /// Fetches appointments and payments, links payments to clients, and identifies pending payments.
        /// </summary>
        /// <returns>A view populated with the model data.</returns>
        public IActionResult Index()
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            _model.ListPendingPayments = new List<MindCare.Application.Entities.Payment>();
            _model.ListAppointments = _appointmentService.GetAppointments().Result;
            _model.ListPayments = _paymentService.GetPayments().Result;

            foreach (var item in _model.ListAppointments)
            {
                dict.Add(item.Id, item.Client.Id);
            }

            foreach (var payment in _model.ListPayments)
            {
                if (dict.TryGetValue(payment.IdAppointment, out int value))
                {
                    payment.Client = _clientRepository.Get(value).Result;
                }
                if (payment.Status != MindCare.Application.Enums.EnumPaymentStatus.Confirmado)
                {
                    _model.ListPendingPayments.Add(payment);
                }
            }

            return View(_model);
        }

        /// <summary>
        /// Handles the privacy policy page action.
        /// </summary>
        /// <returns>A view for the privacy policy page.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Handles errors and returns appropriate views based on the error status code.
        /// </summary>
        /// <param name="statuscode">The HTTP status code of the error.</param>
        /// <returns>A view for the error page, or a specific view for 404 errors.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statuscode)
        {
            if (statuscode == 404)
            {
                return View("NotFound");
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
