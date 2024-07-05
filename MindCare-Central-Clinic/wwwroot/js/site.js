function OpenPaymentInfo(seta) {

    if ($("#" + seta).hasClass("active")) {
        $("#" + seta).removeClass("rotate");
        $("#" + seta).addClass("reverseRotate");
        $("#" + seta).removeClass("active");
    } else {
        $("#" + seta).removeClass("reverseRotate");
        $("#" + seta).addClass("rotate");
        $("#" + seta).addClass("active");
    }
    $("#setaConteudo_" + seta).slideToggle("slow");
}


var spinnerWrapper = document.getElementById("spinnerWrapper");
var spinnerModal = document.getElementById("spinMod");
window.addEventListener('load', function () {
    if (spinnerModal.style.display = "flex") {
        spinnerModal.style.display = "none";
        spinnerWrapper.style.display = "none";
    }
});

var Util = {
    MontarTable: async (idTabela = "table") => {
        return new Promise((resolve, reject) => {
            $("#" + idTabela).dataTable({
                language: {
                    url: 'https://cdn.datatables.net/plug-ins/1.10.16/i18n/Portuguese-Brasil.json'
                },
                order: [],
                initComplete: function () {
                    resolve();
                },
                error: function (xhr, errorType, exception) {
                    reject(exception);
                }
            });
        });
    },
}