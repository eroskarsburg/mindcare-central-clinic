var ActProfile = {
    UpdateUserProfile: function (id) {
        var obj = {
            id: id,
            username: $('#profile-username').val(),
            password: $('#profile-password').val(),
            accesslevel: $('#profile-accesslevel').val(),
        };

        if (obj.password != $('#profile-confirmpassword').val()) {
            return alert("Senhas não coincidem. Preencha-as novamente");
        }

        $.ajax({
            url: '/User/Update',
            type: 'PUT',
            data: obj,
            success: function (data) {
                alert(data.message);
                window.location.reload();
            },
            error: function (data) {
                alert(data.message);
            }
        });
    },
    UpdateProfessionalProfile: function (id) {
        var obj = {
            id: id,
            name: $('#profile-professional-name').val(),
            cpf: $('#profile-professional-cpf').val(),
            gender: $('#profile-professional-gender').val(),
            speciality: $('#profile-professional-speciality').val(),
        };
        $.ajax({
            url: '/Professional/Update',
            type: 'PUT',
            data: obj,
            success: function (data) {
                alert(data.message);
                window.location.reload();
            },
            error: function (data) {
                alert(data.message);
            }
        });
    },
}