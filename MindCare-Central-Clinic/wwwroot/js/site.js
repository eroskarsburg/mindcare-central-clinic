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