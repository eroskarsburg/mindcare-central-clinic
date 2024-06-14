const _CarregarPopUp = async (Estrutura) => {
    var Default = {
        TipoPopUp: Estrutura.TipoPopUp,
        Mensagem: Estrutura.Mensagem,
        FnConfirma: Estrutura.FnConfirma,
        FnCancela: Estrutura.FnCancela
    }

    if (Default.Mensagem.startsWith('"') && Default.Mensagem.endsWith('"')) {
        Default.Mensagem = Default.Mensagem.slice(1, -1);
    }

    await fetch(`/Shared/PopUp/PopUp?mensagem=${Default.Mensagem}&tipoPopUp=${Default.TipoPopUp}`)
        .then(response => response.text())
        .then(data => {
            var existePopUp = document.getElementById('container_modalPopup');
            if (existePopUp) {
                existePopUp.remove();
            }

            let divPopup = document.createElement("div");
            divPopup.id = `container_modalPopup`;
            divPopup.innerHTML = data;
            document.body.appendChild(divPopup);

            var popUpElemento = document.getElementById('modal_popUp');
            var instancia = new bootstrap.Modal(popUpElemento);
            instancia.show();

            popUpElemento.addEventListener('shown.bs.modal', function () {
                var backdrops = document.querySelectorAll('.modal-backdrop');
                var backdrop = backdrops[backdrops.length - 1];
                backdrop.id = "backdrop-popup"
            });

            var btnConfirma = document.getElementById('popUp_btnConfirma');
            var btnNega = document.getElementById('popUp_btnNega');

            if (btnConfirma !== undefined && Default.FnConfirma !== undefined) {
                btnConfirma.addEventListener('click', function () {
                    Default.FnConfirma();
                });
            }
            if (btnNega !== undefined && Default.FnCancela !== undefined) {
                btnNega.addEventListener('click', function () {
                    Default.FnCancela();
                });
            }
        })
        .catch(error => {
            console.error('Ocorreu um erro:', error);
        });
}