var listClients = [];

window.onload = (event) => {
    ActAppointment.Get();
};

var ActAppointment = {
    Get: function () {
        $.ajax({
            url: '/Client/Get',
            type: 'GET',
            success: function (data) {
                listClients = data.listClient;
            },
            error: function (data) {
                alert("Erro: " + data.message);
            }
        });
    },
    Save: function () {
        var obj = {
            client: {
                id: $('#appoint-client').val(),
            },
            scheduledDate: $('#appoint-date').val(),
            ScheduledHour: $('#appoint-hour').val(),
            modality: $('#appoint-modality').val(),
            observation: $('#appoint-observation').val(),
            payment: {
                price: $('#appoint-price').val(),
                paidprice: $('#appoint-paidprice').val(),
                paiddate: $('#appoint-paiddate').val(),
                status: $('#appoint-status').val(),
            },
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
            client: {
                id: $('#appoint-update-client').val(),
            },
            scheduledDate: $('#appoint-update-date').val(),
            ScheduledHour: $('#appoint-update-hour').val(),
            modality: $('#appoint-update-modality').val(),
            observation: $('#appoint-update-observation').val(),
            payment: {
                price: $('#appoint-update-price').val(),
                paidprice: $('#appoint-update-paidprice').val(),
                paiddate: $('#appoint-update-paiddate').val(),
                status: $('#appoint-update-status').val(),
            },
        };
        debugger;
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
var ModalAppointment = {
    Update: function (obj) {
        let dateObject = new Date(obj.ScheduledDate);

        let year = dateObject.getFullYear();
        let month = String(dateObject.getMonth() + 1).padStart(2, '0'); // Months are zero-based
        let day = String(dateObject.getDate()).padStart(2, '0');

        let hours = String(dateObject.getHours()).padStart(2, '0');
        let minutes = String(dateObject.getMinutes()).padStart(2, '0');
        //let seconds = String(dateObject.getSeconds()).padStart(2, '0');

        let formattedTime = `${hours}:${minutes}`;
        let formattedDate = `${year}-${month}-${day}`;

        modal = `<div id="md-update-appoint" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Atualizar</h5>
                      </div>
                      <div class="modal-body">
                        <form>
                            <div class="div-SubTotal" onclick="OpenPaymentInfo(3)">
                                <span style="font-size: 18px;font-weight: 600;">
                                    Dados Agendamento
                                </span>
                                <div id="3" class="containerSetInfos rotate active">
                                    <svg version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 17.851 11.965"
                                         style="width: 18px;transform: rotateZ(180deg);" xml:space="preserve">
                                    <g>
                                    <path class="icone-setaRotatoria" d="M8.049,1.969l1.935,0.005l7.457,8.022l-1.737-0.005L8.999,2.668L2.29,9.959L0.552,9.955L8.049,1.969z"></path>
                                    </g>
                                    </svg>
                                </div>
                            </div>
                            <div id="setaConteudo_3" class="container-resumoCard">
                                <div class="form-group">
                                    <label for="appoint-update-client" class="col-form-label">Cliente:</label>
                                    <select class="form-select" id="appoint-update-client">
                                        ${this.SetClientsDropdown(obj.Client.Id)}
                                    </select>
                                </div>
                                <div class="form-group">
                                    <label for="appoint-update-modality" class="col-form-label">Modalidade:</label>
                                    <select class="form-select" id="appoint-update-modality">
                                        ${this.ValidateModalityDropdown(obj.Modality)}
                                    </select>
                                </div>
                                <div class="form-group">
                                    <label for="appoint-update-date" class="col-form-label">Data:</label>
                                    <input type="date" class="form-control" id="appoint-update-date" value="${formattedDate}">
                                </div>
                                <div class="form-group">
                                    <label for="appoint-update-hour" class="col-form-label">Horario:</label>
                                    <input type="time" class="form-control" id="appoint-update-hour" value="${formattedTime}">
                                </div>
                                <div class="form-label">
                                    <label for="appoint-update-observation" class="col-form-label">Observação:</label>
                                    <textarea class="form-control" id="appoint-update-observation">${obj.Observation}</textarea>
                                </div>
                            </div>
                            <div class="div-SubTotal" onclick="OpenPaymentInfo(4)">
                                <span style="font-size: 18px;font-weight: 600;">
                                    Dados Pagamento
                                </span>
                                <div id="4" class="containerSetInfos rotate active">
                                    <svg version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 17.851 11.965"
                                         style="width: 18px;" xml:space="preserve">
                                    <g>
                                        <path class="icone-setaRotatoria" d="M8.049,1.969l1.935,0.005l7.457,8.022l-1.737-0.005L8.999,2.668L2.29,9.959L0.552,9.955L8.049,1.969z"></path>
                                    </g>
                                    </svg>
                                </div>
                            </div>
                            <div id="setaConteudo_4" class="container-resumoCard" style="display: none;">
                                <div class="form-group">
                                    <label for="appoint-update-price" class="col-form-label">Valor:</label>
                                    <input type="text" class="form-control" id="appoint-update-price" value="${obj.Payment.Price}">
                                </div>
                                <div class="form-group">
                                    <label for="appoint-update-paidprice" class="col-form-label">Valor Pago:</label>
                                    <input type="text" placeholder="Valor pago não registrado" class="form-control" id="appoint-update-paidprice" value="${obj.Payment.PaidPrice == 0 ? '' : obj.Payment.PaidPrice}">
                                </div>
                                <div class="form-group">
                                    <label for="appoint-update-paiddate" class="col-form-label">Data:</label>
                                    <input type="text" placeholder="Data do pagamento não registrada" class="form-control" id="appoint-update-paiddate" value="${obj.Payment.PaidDate}" disabled>
                                </div>
                                <div class="form-label">
                                    <label for="appoint-update-status" class="col-form-label">Status:</label>
                                    <select class="form-select" id="appoint-update-status">
                                        ${this.ValidateStatusDropdown(obj.Payment.Status)}
                                    </select>
                                </div>
                            </div>
                        </form>
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActAppointment.Update(${obj.Id})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalAppointment.CloseModalUpdate()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-update-appoint').innerHTML = modal;
        document.getElementById('md-update-appoint').style.display = 'block';
    },
    Delete: function (appointId, paymentId) {
        modal = `<div id="md-delete-appoint" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Deletar</h5>
                      </div>
                      <div class="modal-body">
                        Tem certeza que deseja deletar o agendamento ${appointId}?
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" onclick="ActAppointment.Delete(${appointId})">Sim</button>
                        <button type="button" class="button-6 button-6-secondary" data-dismiss="modal" onclick="ModalAppointment.CloseModalDelete()">Não</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('modal-delete-appoint').innerHTML = modal;
        document.getElementById('md-delete-appoint').style.display = 'block';
    },

    CloseModalDelete: function () {
        document.getElementById('md-delete-appoint').style.display = 'none';
    },
    CloseModalUpdate: function () {
        document.getElementById('md-update-appoint').style.display = 'none';
    },

    SetClientsDropdown: function (idClient) {
        var returnedList = this.AddClientsOptions();

        var expectedOption = returnedList.find(e => e.includes(`value="${idClient}"`));

        var index = returnedList.indexOf(expectedOption);
        if (index === -1) {
            return returnedList;
        }
        returnedList.splice(index, 1);
        returnedList.unshift(expectedOption);
        return returnedList;
    },
    ValidateStatusDropdown: function (status) {
        if (status == 0) {
            return `<option>Pendente</option>
                    <option>Confirmado</option>
                    <option>Parcial</option>`;
        }
        if (status == 2) {
            return `<option>Parcial</option>
                    <option>Confirmado</option>
                    <option>Pendente</option>`;
        }
        return `<option>Confirmado</option>
                <option>Pendente</option>
                <option>Parcial</option>`;
    },
    ValidateModalityDropdown: function (modality) {
        if (modality == 1) {
            return `<option>Teleconsulta</option>
                    <option>Presencial</option>`;
        }
        return `<option>Presencial</option>
                <option>Teleconsulta</option>`;
    },

    AddClientsOptions: function () {
        var optionList = [];
        for (var i = 0; i < listClients.length; i++) {
            var option = `<option value="${listClients[i].id}">${listClients[i].name}</option>\n`;
            optionList.push(option);
        }
        optionList.sort();
        return optionList;
    },
}