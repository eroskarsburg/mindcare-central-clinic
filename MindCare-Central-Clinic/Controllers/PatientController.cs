using Microsoft.AspNetCore.Mvc;

namespace MindCare_Central_Clinic.Controllers
{
    public class PatientController : Controller
    {
        private readonly ILogger<PatientController> _logger;

        public PatientController(ILogger<PatientController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {


            return View();
        }
    }
}
