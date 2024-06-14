using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MindCare_Central_Clinic.Shared.Entities;
using MindCare_Central_Clinic.Shared.Enum;

namespace MindCare_Central_Clinic.Views.Shared.PopUp
{
    public class PopUpModel : PageModel
    {
        [BindProperty]
        public PopUpEntity? PopUp { get; set; }

        public void OnGet(string mensagem, EnumPopUpType tipoPopUp)
        {
            this.PopUp = new PopUpEntity
            {
                Message = mensagem,
                PopUpType = tipoPopUp
            };
        }
    }
}
