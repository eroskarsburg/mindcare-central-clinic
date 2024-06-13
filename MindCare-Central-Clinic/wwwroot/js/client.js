function saveClient() {
    var obj = {
        name: $('#client-name').val(),
        cpf: $('#client-cpf').val(),
        gender: $('#client-gender').val(),
        age: $('#client-age').val(),
    };

    $.ajax({
        url: '/Client/Insert',
        type: 'POST',
        data: obj,
        success: function (data) {
            alert(data.message);
            window.location.reload();
        }
    });
}