using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MindCare_Central_Clinic.Shared.Enum;

namespace MindCare_Central_Clinic.Views.Shared.Modal
{
    public class ModalModel : PageModel
    {
        public string? Title { get; set; }
        public int Id { get; set; }
        public EnumModalSize ModalSize { get; set; }
        public bool ShowButtons { get; set; }
        public string? ConfirmButtonName { get; set; }

        public IActionResult OnGet(string titulo, int id, EnumModalSize tamanhoModal, bool exibeBotoes = true, string nomeBtnConfirma = "")
        {
            Title = titulo;
            Id = id;
            ModalSize = tamanhoModal;
            ShowButtons = exibeBotoes;
            ConfirmButtonName = nomeBtnConfirma;
            return Page();
        }
    }
}
