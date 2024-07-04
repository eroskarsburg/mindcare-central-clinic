
//window.onload = function () {
//    Util.LimitTableRows(this);
//}


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
    },
    Sort: function (n, table) {
        var table, rows, switching, i, x, y, shouldSwitch, dir, switchcount = 0;
        table = document.getElementById(table);
        switching = true;
        dir = "asc";
        while (switching) {
            switching = false;
            rows = table.rows;
            for (i = 1; i < (rows.length - 1); i++) {
                shouldSwitch = false;
                x = rows[i].getElementsByTagName("TD")[n];
                y = rows[i + 1].getElementsByTagName("TD")[n];
                if (dir == "asc") {
                    if (x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase()) {
                        shouldSwitch = true;
                        break;
                    }
                } else if (dir == "desc") {
                    if (x.innerHTML.toLowerCase() < y.innerHTML.toLowerCase()) {
                        shouldSwitch = true;
                        break;
                    }
                }
            }
            if (shouldSwitch) {
                rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
                switching = true;
                switchcount++;
            } else {
                if (switchcount == 0 && dir == "asc") {
                    dir = "desc";
                    switching = true;
                }
            }
        }
    },
    //LimitTableRows: function (tableName) {
    //    const rowCount = document.getElementById('rowCount').value;
    //    const table = document.getElementById(tableName);
    //    const rows = table.getElementsByTagName('tr');

    //    for (let i = 1; i < rows.length; i++) {
    //        if (i <= rowCount) {
    //            rows[i].style.display = '';
    //        } else {
    //            rows[i].style.display = 'none';
    //        }
    //    }
    //},
}