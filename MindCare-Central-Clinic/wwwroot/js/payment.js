﻿var ActClient = {
    Save: function () {
        var obj = {
            name: $('#client-name').val(),
            cpf: $('#client-cpf').val(),
            gender: $('#client-gender').val(),
            age: $('#client-age').val(),
        };

        $.ajax({
            url: '/Appointment/Insert',
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
            name: $('#client-update-name').val(),
            cpf: $('#client-update-cpf').val(),
            gender: $('#client-update-gender').val(),
            age: $('#client-update-age').val(),
        };
        $.ajax({
            url: '/Appointment/Update',
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
            url: `/Appointment/Delete/${id}`,
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
var ModalClient = {
    Update: function (id, name, cpf, gender, age) {
        $('#client-update-gender').val(gender);
        modal = `<div id="md-update-client" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Atualizar</h5>
                      </div>
                      <div class="modal-body">
                        <form>
                            <div class="form-group">
                                <label for="client-update-name" class="col-form-label">Nome:</label>
                                <input type="text" class="form-control" id="client-update-name" value="${name}">
                            </div>
                            <div class="form-group">
                                <label for="client-update-cpf" class="col-form-label">CPF:</label>
                                <input type="text" class="form-control" id="client-update-cpf" value="${cpf}">
                            </div>
                            <div class="form-group">
                                <label for="client-update-gender" class="col-form-label">Gênero:</label>
                                <select class="form-select" id="client-update-gender">
                                    ${ModalClient.ValidateGenderDropdown(gender)}
                                </select>
                            </div>
                            <div class="form-label">
                                <label for="client-update-age" class="col-form-label">Idade:</label>
                                <input type="text" class="form-control" id="client-update-age" value="${age}">
                            </div>
                        </form>
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActClient.Update(${id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalClient.CloseModalUpdate()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-update-client').innerHTML = modal;
        document.getElementById('md-update-client').style.display = 'block';
    },
    Delete: function (id, name) {
        modal = `<div id="md-delete-client" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Deletar</h5>
                      </div>
                      <div class="modal-body">
                        Tem certeza que deseja deletar o cliente ${name}?
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActClient.Delete(${id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalClient.CloseModalDelete()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-delete-client').innerHTML = modal;
        document.getElementById('md-delete-client').style.display = 'block';
    },

    CloseModalDelete: function () {
        document.getElementById('md-delete-client').style.display = 'none';
    },
    CloseModalUpdate: function () {
        document.getElementById('md-update-client').style.display = 'none';
    },
}