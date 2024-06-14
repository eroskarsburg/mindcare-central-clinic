let dicionarioFuncoesModal = {};
let novoId;

const _CarregarModal = async (Estrutura) => {
    var Default = {
        Titulo: Estrutura.Titulo,
        Body: Estrutura.Body,
        Tamanho: Estrutura.Tamanho,
        ExibeBotoes: Estrutura.ExibeBotoes,
        FnConfirma: Estrutura.FnConfirma,
        FnCancela: Estrutura.FnCancela,
        NomeBtnConfirma: Estrutura.NomeBtnConfirma
    }

    var modalsPartial = document.querySelectorAll('[id^="modal_partial-"]');
    novoId = modalsPartial.length += 1
    let tamanhoModal = 1;
    let exibeBotoes = true;
    let nomeBtnConfirma = "";

    if (Default.Tamanho != undefined) {
        tamanhoModal = parseInt(Default.Tamanho);
    }

    if (Default.ExibeBotoes != undefined) {
        exibeBotoes = Default.ExibeBotoes;
    }

    if (Default.NomeBtnConfirma != undefined) {
        nomeBtnConfirma = Default.NomeBtnConfirma;
    }

    await fetch(`/Shared/Modal/Modal?titulo=${Default.Titulo}&id=${novoId}&tamanhoModal=${tamanhoModal}&exibeBotoes=${exibeBotoes}&nomeBtnConfirma=${nomeBtnConfirma}`)
        .then(response => response.text())
        .then(data => {
            let divModal = document.createElement("div");
            divModal.id = `modal_partial-${novoId}`;
            divModal.innerHTML = data;
            document.body.appendChild(divModal);

            var modalBody = document.getElementById(`modal_partialBody-${novoId}`);
            modalBody.innerHTML += Default.Body;

            var btnModalConfirma = document.getElementById(`btn_modalConfirma-${novoId}`);
            var btnModalCancela = document.getElementById(`btn_modalCancela-${novoId}`);
            var btnModalClose = document.getElementById(`btn_modalClose-${novoId}`);

            if (btnModalConfirma) {
                if (Default.FnConfirma !== undefined) {
                    if (btnModalConfirma.id in dicionarioFuncoesModal) {
                        delete dicionarioFuncoesModal[btnModalConfirma.id];
                    }

                    dicionarioFuncoesModal[btnModalConfirma.id] = Default.FnConfirma;
                    btnModalConfirma.setAttribute('onclick', '_ActionModal._ExecutarAcao(this)');
                }
            }

            if (btnModalCancela) {
                if (btnModalCancela.id in dicionarioFuncoesModal) {
                    delete dicionarioFuncoesModal[btnModalCancela.id];
                }

                if (Default.FnCancela !== undefined) {
                    dicionarioFuncoesModal[btnModalCancela.id] = Default.FnConfirma;
                }
                else {
                    dicionarioFuncoesModal[btnModalCancela.id] = function () { _CloseModal(btnModalCancela.id); };
                }

                btnModalCancela.setAttribute('onclick', '_ActionModal._ExecutarAcao(this)');
            }

            if (btnModalClose) {
                if (btnModalClose.id in dicionarioFuncoesModal) {
                    delete dicionarioFuncoesModal[btnModalClose.id];
                }

                dicionarioFuncoesModal[btnModalClose.id] = function () { _CloseModal(btnModalClose.id); };
                btnModalClose.setAttribute('onclick', '_ActionModal._ExecutarAcao(this)');
            }
        })
        .catch(error => {
            console.error('Ocorreu um erro:', error);
        });
}

const _CloseModal = (id) => {
    if (id != undefined) {
        var numero = id.split('-')[1];
        let modalPartial = document.getElementById(`modal_partial-${numero}`);
        modalPartial.remove();
    }
}

var _ActionModal = {
    _ExecutarAcao: function (button) {
        let funcaoParaExecutar = dicionarioFuncoesModal[button.id];
        if (funcaoParaExecutar) {
            funcaoParaExecutar();
        }
    },
}

var ActionModal = {
    FixarAltura: function () {
        let modalContent = document.getElementById(`modal-content-${novoId}`);
        if (modalContent) {
            modalContent.style.minHeight = `${modalContent.offsetHeight}px`;
        }
    },
}