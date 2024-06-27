var ActUser = {
    Save: function () {
        var obj = {
            username: $('#user-username').val(),
            password: $('#user-password').val(),
            accesslevel: $('#user-aclevel').val(),
        };

        if (obj.accesslevel == "Profissional") {
            obj.accesslevel = 2;
        } else {
            obj.accesslevel = 1;
        }

        $.ajax({
            url: '/User/Insert',
            type: 'POST',
            data: obj,
            success: function (data) {
                window.location.reload();
            },
            error: function (data) {
                alert(data.message);
            }
        });
    },
    Update: function (id) {
        var obj = {
            id: id,
            username: $('#user-update-username').val(),
            password: $('#user-update-password').val(),
            accesslevel: $('#user-update-aclevel').val(),
        };

        if (obj.accesslevel == "Profissional") {
            obj.accesslevel = 2;
        } else {
            obj.accesslevel = 1;
        }

        $.ajax({
            url: '/User/Update',
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
    Delete: function (id) {
        $.ajax({
            url: `/User/Delete/${id}`,
            type: 'DELETE',
            data: id,
            success: function (data) {
                window.location.reload();
            },
            error: function (data) {
                alert(data.message);
            }
        });
    },
}

var modal;
var ModalUser = {
    Update: function (id, username, password, access_level) {
        $('#user-update-aclevel').val(access_level);
        modal = `<div id="md-update-user" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Atualizar</h5>
                      </div>
                      <div class="modal-body">
                        <form>
                            <div class="form-group">
                                <label for="user-update-username" class="col-form-label">Usuário:</label>
                                <input type="text" class="form-control" id="user-update-username" value="${username}">
                            </div>
                            <div class="form-group">
                                <label for="user-update-password" class="col-form-label">Senha:</label>
                                <input type="password" class="form-control" id="user-update-password" value="${password}">
                                <input type="checkbox" onclick="ModalUser.ShowPasswordUpdate()">Mostrar Senha
                            </div>
                            <div class="form-group">
                                <label for="user-update-aclevel" class="col-form-label">Nível de Acesso:</label>
                                <select class="form-select" id="user-update-aclevel">
                                    ${ModalUser.ValidateAccessLevelDropdown(access_level)}
                                </select>
                            </div>
                        </form>
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActUser.Update(${id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalUser.CloseModalUpdate()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-update-user').innerHTML = modal;
        document.getElementById('md-update-user').style.display = 'block';
    },
    Delete: function (id, name) {
        modal = `<div id="md-delete-user" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Deletar</h5>
                      </div>
                      <div class="modal-body">
                        Tem certeza que deseja deletar o usuário ${name}?
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActUser.Delete(${id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalUser.CloseModalDelete()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-delete-user').innerHTML = modal;
        document.getElementById('md-delete-user').style.display = 'block';
    },

    CloseModalDelete: function () {
        document.getElementById('md-delete-user').style.display = 'none';
    },
    CloseModalUpdate: function () {
        document.getElementById('md-update-user').style.display = 'none';
    },

    ValidateAccessLevelDropdown: function (level) {
        if (level == 'Administrador') {
            return `<option>Administrador</option>
                        <option>Profissional</option>`;
        }
        return `<option>Profissional</option>
                        <option>Administrador</option>`;
    },

    ShowPassword: function () {
        var x = document.getElementById("user-password");
        if (x.type === "password") {
            x.type = "text";
        } else {
            x.type = "password";
        }
    },
    ShowPasswordUpdate: function () {
        var x = document.getElementById("user-update-password");
        if (x.type === "password") {
            x.type = "text";
        } else {
            x.type = "password";
        }
    }
}