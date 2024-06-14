using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MindCare_Central_Clinic.Controllers;
using MindCare_Central_Clinic.Shared.Entities;

namespace MindCare_Central_Clinic.Views.Client.Partials
{
    public class _ClientsModal : PageModel
    {
        private readonly ClientController _controller;

        [BindProperty]
        public ClientEntity Client { get; set; }

        public _ClientsModal(ClientController controller)
        {
            _controller = controller;
        }

        public async Task<IActionResult> OnGet(int id)
        {
            if (id >= 0)
            {
                var response = await _controller.Obter(id) as ObjectResult;

                if (response!.StatusCode != 200)
                {
                    return StatusCode((int)response.StatusCode!, response.Value!);
                }

                Client = response.Value as ClientEntity;
            }
            else
            {
                Client = new ClientEntity();
                Client.Id = -1;
            }

            return Page();
        }
    }
}
