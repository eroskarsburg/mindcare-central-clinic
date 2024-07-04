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
    document.cookie = 'user=; expires = ' + new Date(2010, 0, 01).toGMTString(); + '; path = /';
    window.location.href = "/Login";
}

var Util = {
    Search: function (elementId) {
        var input, filter, table, tr, td, i, txtValue;
        input = document.getElementById("search-input");
        filter = input.value.toUpperCase();
        table = document.getElementById(elementId);
        tr = table.getElementsByTagName("tr");
        for (i = 0; i < tr.length; i++) {
            td = tr[i].getElementsByTagName("td")[0];
            if (td) {
                txtValue = td.textContent || td.innerText;
                if (txtValue.toUpperCase().indexOf(filter) > -1) {
                    tr[i].style.display = "";
                } else {
                    tr[i].style.display = "none";
                }
            }
        }
    }
}