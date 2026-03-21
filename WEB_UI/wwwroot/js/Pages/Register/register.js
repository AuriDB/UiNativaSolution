function RegisterView() {

    let currentStep = 1;
    let otpModal    = null;
    let timerInterval = null;

    this.InitView = () => {
        bindTogglePassword("btnTogglePwd", "contrasena", "eyePwd");
        this.UpdateStep2Button(false);
        // Mover el modal al <body> para evitar conflictos de stacking context - haciendo correcciones
        document.body.appendChild(document.getElementById("modalOtp"));
        otpModal = new bootstrap.Modal(document.getElementById("modalOtp"));
        this.BindEvents();
    };

    this.BindEvents = () => {
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

        $("#chkTerminos").on("change", function () {
            $("#btnRegister").prop("disabled", !this.checked);
        });

        $("#btnStep1").on("click", () => this.GoToStep(2));
        $("#btnBack1").on("click", () => this.GoToStep(1));
        $("#btnStep2").on("click", () => this.GoToStep(3));
        $("#btnBack2").on("click", () => this.GoToStep(2));

        $("#frmRegister").on("submit", (e) => {
            e.preventDefault();
            this.Register();
        });

        // Inputs OTP: auto-avanzar y borrar
        $(".otp-input").on("input", function () {
            const val = $(this).val().replace(/\D/g, "");
            $(this).val(val.slice(0, 1));
            if (val.length === 1) {
                $(this).next(".otp-input").trigger("focus");
            }
        }).on("keydown", function (e) {
            if (e.key === "Backspace" && $(this).val() === "") {
                $(this).prev(".otp-input").trigger("focus");
            }
        }).on("paste", function (e) {
            e.preventDefault();
            const pasted = (e.originalEvent.clipboardData || window.clipboardData)
                            .getData("text").replace(/\D/g, "").slice(0, 6);
            $(".otp-input").each(function (i) {
                $(this).val(pasted[i] || "");
            });
            $(".otp-input").last().trigger("focus");
        });

        $("#btnVerificarOtp").on("click", () => this.VerificarOtp());
        $("#btnReenviarOtp").on("click",  () => this.ReenviarOtp());
    };

    this.UpdateStep2Button = (enabled) => {
        $("#btnStep2").prop("disabled", !enabled);
    };

    this.GoToStep = (step) => {
        if (step === 2 && !this.ValidateStep1()) return;

        $(`#step${currentStep}`).addClass("d-none");
        $(`#step${step}`).removeClass("d-none");

        for (let i = 1; i <= 3; i++) {
            $(`#dot${i}`).toggleClass("psa-step-dot-active", i <= step)
                         .toggleClass("psa-step-dot-done",   i < step);
        }

        if (step === 3) this.FillResumen();
        currentStep = step;
    };

    this.ValidateStep1 = () => {
        const nombre         = $("#nombre").val().trim();
        const primerApellido = $("#primerApellido").val().trim();
        const cedula         = $("#cedula").val().trim();
        const correo         = $("#correo").val().trim();

        if (!nombre || !primerApellido || !cedula || !correo) {
            showAlert("alertContainer", "Por favor completa todos los campos obligatorios.", "warning");
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
        $("#resumePrimerApellido").text($("#primerApellido").val());
        $("#resumeSegundoApellido").text($("#segundoApellido").val() || "—");
        $("#resumeCedula").text($("#cedula").val());
        $("#resumeCorreo").text($("#correo").val());
    };

    this.Register = () => {
        const payload = {
            Nombre:          $("#nombre").val().trim(),
            PrimerApellido:  $("#primerApellido").val().trim(),
            SegundoApellido: $("#segundoApellido").val().trim(),
            Email:           $("#correo").val().trim(),
            Cedula:          $("#cedula").val().trim(),
            Password:        $("#contrasena").val()
        };

        setLoading("#btnRegister", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         "/Register/Registrar",
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify(payload),
            success: (res) => {
                setLoading("#btnRegister", false);
                if (res.result === "ok") {
                    sessionStorage.setItem("registroCorreo", payload.Email);
                    this.AbrirModalOtp(payload.Email, res.message || null);
                } else {
                    showAlert("alertContainer", res.message || "Error al crear la cuenta.", "danger");
                    this.GoToStep(1);
                }
            },
            error: (xhr) => {
                let msg = "Error de conexión con el servidor. Intenta de nuevo.";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    msg = xhr.responseJSON.message;
                }
                showAlert("alertContainer", msg, "danger");
                setLoading("#btnRegister", false);
            }
        });
    };

    // Modal OTP 

    this.AbrirModalOtp = (email, infoMsg = null) => {
        $("#otpEmailDisplay").text(email);
        $(".otp-input").val("");
        clearAlert("otpAlertContainer");
        if (infoMsg) {
            showAlert("otpAlertContainer", infoMsg, "info");
        }
        otpModal.show();
        $("#otp1").trigger("focus");
        this.StartTimer(90);
    };

    this.GetCodigoOtp = () => {
        return ["otp1","otp2","otp3","otp4","otp5","otp6"]
                .map(id => $("#" + id).val()).join("");
    };

    this.StartTimer = (seconds) => {
        clearInterval(timerInterval);
        $("#btnReenviarOtp").prop("disabled", true);
        let remaining = seconds;
        $("#otpTimer").text(`(${remaining}s)`);
        timerInterval = setInterval(() => {
            remaining--;
            if (remaining <= 0) {
                clearInterval(timerInterval);
                $("#otpTimer").text("");
                $("#btnReenviarOtp").prop("disabled", false);
            } else {
                $("#otpTimer").text(`(${remaining}s)`);
            }
        }, 1000);
    };

    this.VerificarOtp = () => {
        const codigo = this.GetCodigoOtp();
        const email  = sessionStorage.getItem("registroCorreo");

        if (codigo.length < 6) {
            showAlert("otpAlertContainer", "Ingresa los 6 dígitos del código.", "warning");
            return;
        }

        setLoading("#btnVerificarOtp", true);
        clearAlert("otpAlertContainer");

        $.ajax({
            url:         "/Register/VerificarOtp",
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ Email: email, Codigo: codigo }),
            success: (res) => {
                setLoading("#btnVerificarOtp", false);
                if (res.result === "ok") {
                    clearInterval(timerInterval);
                    otpModal.hide();
                    sessionStorage.removeItem("registroCorreo");
                    window.location.href = "/Login?registered=1";
                } else {
                    showAlert("otpAlertContainer", res.message || "Código incorrecto.", "danger");
                    $(".otp-input").val("");
                    $("#otp1").trigger("focus");
                }
            },
            error: () => {
                setLoading("#btnVerificarOtp", false);
                showAlert("otpAlertContainer", "Error de conexión. Intenta de nuevo.", "danger");
            }
        });
    };

    this.ReenviarOtp = () => {
        const email = sessionStorage.getItem("registroCorreo");

        $("#btnReenviarOtp").prop("disabled", true);
        clearAlert("otpAlertContainer");

        $.ajax({
            url:         "/Register/ReenviarOtp",
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ Email: email }),
            success: (res) => {
                if (res.result === "ok") {
                    $(".otp-input").val("");
                    $("#otp1").trigger("focus");
                    this.StartTimer(90);
                    showAlert("otpAlertContainer", "Código reenviado exitosamente.", "success");
                } else {
                    showAlert("otpAlertContainer", res.message || "No se pudo reenviar el código.", "danger");
                    $("#btnReenviarOtp").prop("disabled", false);
                }
            },
            error: () => {
                showAlert("otpAlertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                $("#btnReenviarOtp").prop("disabled", false);
            }
        });
    };
}

$(document).ready(() => {
    const view = new RegisterView();
    view.InitView();
});
