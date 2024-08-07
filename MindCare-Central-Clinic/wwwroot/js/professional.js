﻿var ActProfessional = {
    Save: function () {
        var obj = {
            name: $('#professional-name').val(),
            cpf: $('#professional-cpf').val(),
            gender: $('#professional-gender').val(),
            speciality: $('#professional-speciality').val(),
        };

        $.ajax({
            url: '/Professional/Insert',
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
    Delete: function (id) {
        $.ajax({
            url: `/Professional/Delete/${id}`,
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
var ModalProfessional = {
    Update: function (profObj) {
        modal = `<div id="md-update-professional" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="professionalModalLabel">Atualizar</h5>
                      </div>
                      <div class="modal-body">
                        <form>
                            <div class="form-group">
                                <label for="professional-update-name" class="col-form-label">Nome:</label>
                                <input type="text" class="form-control" id="professional-update-name" value="${profObj.Name}">
                            </div>
                            <div class="form-group">
                                <label for="professional-update-cpf" class="col-form-label">CPF:</label>
                                <input type="text" class="form-control" id="professional-update-cpf" value="${profObj.Cpf}">
                            </div>
                            <div class="form-group">
                                <label for="professional-update-gender" class="col-form-label">Gênero:</label>
                                <select class="form-select" id="professional-update-gender">
                                    ${ModalProfessional.ValidateGenderDropdown(profObj.Gender)}
                                </select>
                            </div>
                            <div class="form-label">
                                <label for="professional-update-speciality" class="col-form-label">Especialidade:</label>
                                <input type="text" class="form-control" id="professional-update-speciality" value="${profObj.Speciality}">
                            </div>
                        </form>
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActProfessional.Update(${profObj.Id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalProfessional.CloseModalUpdate()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-update-professional').innerHTML = modal;
        document.getElementById('md-update-professional').style.display = 'block';
    },
    Delete: function (id, name) {
        modal = `<div id="md-delete-professional" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Deletar</h5>
                      </div>
                      <div class="modal-body">
                        Tem certeza que deseja deletar o professional ${name}?
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActProfessional.Delete(${id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalProfessional.CloseModalDelete()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-delete-professional').innerHTML = modal;
        document.getElementById('md-delete-professional').style.display = 'block';
    },

    CloseModalDelete: function () {
        document.getElementById('md-delete-professional').style.display = 'none';
    },
    CloseModalUpdate: function () {
        document.getElementById('md-update-professional').style.display = 'none';
    },

    ValidateGenderDropdown(gender) {
        if (gender == "Feminino") {
            return `<option>Feminino</option>
                    <option>Masculino</option>
                    <option>Outro</option>`;
        }
        if (gender == "Outro") {
            return `<option>Outro</option>
                    <option>Masculino</option>
                    <option>Feminino</option>`;
        }
        return `<option>Masculino</option>
                <option>Feminino</option>
                <option>Outro</option>`;
    }
}