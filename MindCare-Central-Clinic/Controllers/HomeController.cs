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

        public HomeController(ILogger<HomeController> logger, HomeViewModel model, IAppointmentService appointmentService, IPaymentService paymentService, IClientRepository clientRepository)
        {
            _logger = logger;
            _model = model;
            _appointmentService = appointmentService;
            _paymentService = paymentService;
            _clientRepository = clientRepository;
        }

        public IActionResult Index()
        {
            Dictionary<int,int> dict = new Dictionary<int,int>();
            _model.ListAppointments = _appointmentService.GetAppointments().Result;
            var payments = _model.ListPayments = _paymentService.GetPayments().Result;
            
            foreach (var item in _model.ListAppointments)
            {
                dict.Add(item.Id, item.Client.Id);
            }
            
            foreach ( var payment in payments)
            {
                if (dict.TryGetValue(payment.IdAppointment, out int value))
                {
                    payment.Client = _clientRepository.Get(value).Result;
                }
            }

            return View(_model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
