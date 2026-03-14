function RegisterView() {

    let currentStep = 1;

    this.InitView = () => {
        bindTogglePassword("btnTogglePwd", "contrasena", "eyePwd");
        this.BindEvents();
    };

    this.BindEvents = () => {
        // Validación contraseña en tiempo real
        $("#contrasena").on("input", () => {
            const pwd    = $("#contrasena").val();
            const nombre = $("#nombre").val();
            const checks = evaluatePassword(pwd, nombre);
            const allOk  = Object.values(checks).every(Boolean);
            const match  = pwd === $("#confirmarContrasena").val() && pwd.length > 0;
            this.UpdateStep2Button(allOk && match);
        });

        $("#confirmarContrasena").on("input", () => {
            const pwd  = $("#contrasena").val();
            const conf = $("#confirmarContrasena").val();
            const match = pwd === conf && conf.length > 0;
            $("#confirmarContrasena").toggleClass("is-valid", match)
                                     .toggleClass("is-invalid", !match && conf.length > 0);
            const checks = evaluatePassword(pwd, $("#nombre").val());
            this.UpdateStep2Button(Object.values(checks).every(Boolean) && match);
        });

        // Términos
        $("#chkTerminos").on("change", function () {
            $("#btnRegister").prop("disabled", !this.checked);
        });

        // Navegación entre pasos
        $("#btnStep1").on("click", () => this.GoToStep(2));
        $("#btnBack1").on("click", () => this.GoToStep(1));
        $("#btnStep2").on("click", () => this.GoToStep(3));
        $("#btnBack2").on("click", () => this.GoToStep(2));

        // Envío
        $("#frmRegister").on("submit", (e) => {
            e.preventDefault();
            this.Register();
        });
    };

    this.UpdateStep2Button = (enabled) => {
        $("#btnStep2").prop("disabled", !enabled);
    };

    this.GoToStep = (step) => {
        if (step === 2 && !this.ValidateStep1()) return;

        $(`#step${currentStep}`).addClass("d-none");
        $(`#step${step}`).removeClass("d-none");

        // Dots
        for (let i = 1; i <= 3; i++) {
            $(`#dot${i}`).toggleClass("psa-step-dot-active", i <= step)
                         .toggleClass("psa-step-dot-done",   i < step);
        }

        if (step === 3) this.FillResumen();
        currentStep = step;
    };

    this.ValidateStep1 = () => {
        const nombre    = $("#nombre").val().trim();
        const apellido1 = $("#apellido1").val().trim();
        const apellido2 = $("#apellido2").val().trim();
        const cedula    = $("#cedula").val().trim();
        const correo    = $("#correo").val().trim();

        if (!nombre || !apellido1 || !apellido2 || !cedula || !correo) {
            showAlert("alertContainer", "Por favor completa todos los campos.", "warning");
            return false;
        }
        if (!/\S+@\S+\.\S+/.test(correo)) {
            showAlert("alertContainer", "Ingresa un correo electrónico válido.", "warning");
            return false;
        }
        clearAlert("alertContainer");
        return true;
    };

    this.FillResumen = () => {
        $("#resumeNombre").text($("#nombre").val());
        $("#resumeApellido1").text($("#apellido1").val());
        $("#resumeApellido2").text($("#apellido2").val());
        $("#resumeCedula").text($("#cedula").val());
        $("#resumeCorreo").text($("#correo").val());
    };

    this.Register = () => {
        const payload = {
            nombre:              $("#nombre").val().trim(),
            apellido1:           $("#apellido1").val().trim(),
            apellido2:           $("#apellido2").val().trim(),
            cedula:              $("#cedula").val().trim(),
            correo:              $("#correo").val().trim(),
            contrasena:          $("#contrasena").val(),
            confirmarContrasena: $("#confirmarContrasena").val()
        };

        setLoading("#btnRegister", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         `${API_URL_BASE}/Auth/Register`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify(payload),
            success: (res) => {
                if (res.success) {
                    sessionStorage.setItem("registroCorreo", payload.correo);
                    window.location.href = "/Register/VerifyOtp";
                } else {
                    showAlert("alertContainer", res.message || "Error al crear la cuenta.", "danger");
                    this.GoToStep(1);
                    setLoading("#btnRegister", false);
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                setLoading("#btnRegister", false);
            }
        });
    };
}

$(document).ready(() => {
    const view = new RegisterView();
    view.InitView();
});
