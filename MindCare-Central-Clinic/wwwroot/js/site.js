var Painel = {
    Modal: async (Estrutura) => {
        await _CarregarModal(Estrutura);
    },
    PopUp: (Estrutura) => {
        _CarregarPopUp(Estrutura);
    },
    ValidarForm: (idForm) => {
        let formParaVerificar = document.getElementById(idForm);
        var inputs = formParaVerificar.getElementsByTagName("input");
        var isValid = true;
        for (var i = 0; i < inputs.length; i++) {
            if (inputs[i].hasAttribute("required")) {
                if (inputs[i].value == "") {
                    inputs[i].classList.add("is-invalid");
                    isValid = false;
                } else {
                    inputs[i].classList.remove("is-invalid");
                }
            }
            inputs[i].addEventListener('input', function () {
                this.classList.remove('is-invalid');
            });
        }
        return isValid;
    },
    SerializeForm: (form) => {
        for (let field in form) {
            let inputElement = document.querySelector(`[name="${field}"]`);
            let dataType = inputElement.getAttribute('data-type');

            switch (dataType) {
                case 'int':
                    form[field] = parseInt(form[field]);
                    break;
                case 'bool':
                    form[field] = form[field] === '1';
                    break;
            }
        }

        return form;
    },
    GetTextFromResponse: async (form) => {
        try {
            const text = await response.text();
            return text;
        } catch (error) {
            throw new Error('Ocorreu um erro ao obter o texto da resposta: ' + error.message);
        }
    },
}