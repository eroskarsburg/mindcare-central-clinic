var ActProfessional = {
    Update: function (id) {
        var obj = {
            id: id,
            name: $('#professional-update-name').val(),
            cpf: $('#professional-update-cpf').val(),
            gender: $('#professional-update-gender').val(),
            speciality: $('#professional-update-speciality').val(),
        };
        $.ajax({
            url: '/Professional/Update',
            type: 'PUT',
            data: obj,
            success: function (data) {
                window.location.reload();
            },
            error: function (data) {
                alert(data.message);
            }
        });
    },
}