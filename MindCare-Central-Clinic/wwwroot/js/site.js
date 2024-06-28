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

function LogOff() {
    document.cookie = 'UserLogin=; expires = ' + new Date(2010, 0, 01).toGMTString(); + '; path = /';
    window.location.href = "/Login.cshtml";
}