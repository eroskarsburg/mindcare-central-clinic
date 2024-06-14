using Microsoft.AspNetCore.Mvc;
using MindCare_Central_Clinic.Services.Abstractions;

namespace MindCare_Central_Clinic.Services
{
    public class WebClientService : IWebClientService
    {


        public Task<IActionResult> Get(int id)
        {
            throw new NotImplementedException();
        }
    }
}
