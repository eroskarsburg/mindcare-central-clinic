var ActLogin = {
    Get: function () {
        var obj = {
            username: $('#login-username').val(),
            password: $('#login-password').val(),
        };

        $.ajax({
            url: '/User/Get',
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
}

var modal;
var ModalLogin = {
    Warning: function (message) {
        modal = `<div id="md-warning-login" class="modal" style="background-color: #0000004d;" aria-hidden="true">
                  <div class="modal-dialog" role="document">
                    <div class="modal-content">
                      <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLabel">Aviso</h5>
                      </div>
                      <div class="modal-body">
                        ${message}
                      </div>
                      <div class="modal-footer">
                        <button type="button" class="button-6 button-6-primary" data-dismiss="modal" onclick="ModalLogin.CloseWarning()">Ok</button>
                      </div>
                    </div>
                  </div>
                </div>`;
        document.getElementById('index-warning-login').innerHTML = modal;
        document.getElementById('md-warning-login').style.display = 'block';
    },

    CloseWarning: function () {
        document.getElementById('md-warning-login').style.display = 'none';
    }
}
