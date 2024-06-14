var ActClient = {
    IsertOrUpdate: function (id) {

        let response = await fetch(`/Client/Partials/_ClientsModal?id=${id}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });


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
    },
    Delete: function (id) {
        $.ajax({
            url: '/Client/Delete',
            type: 'DELETE',
            data: id,
            success: function (data) {
                alert(data.message);
                window.location.reload();
            }
        });
    }
}