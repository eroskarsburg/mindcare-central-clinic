using Microsoft.AspNetCore.Mvc;

namespace MindCare_Central_Clinic.Services.Abstractions
{
    public interface IWebClientService
    {
        Task<IActionResult> Get(int id);
    }
}
