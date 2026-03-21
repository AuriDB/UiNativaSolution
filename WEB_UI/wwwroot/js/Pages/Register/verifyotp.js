function VerifyOtpView() {

    const OTP_SECONDS      = 90;
    const COOLDOWN_SECONDS = 30;
    const MAX_ATTEMPTS     = 3;
    const MAX_RESENDS      = 3;

    let timerInterval  = null;
    let remaining      = OTP_SECONDS;
    let intentos       = MAX_ATTEMPTS;
    let reenvios       = 0;
    let cooldownActive = false;

    this.InitView = () => {
        this.StartTimer();
        this.BindOtpInputs();
        this.BindEvents();
    };

    this.StartTimer = () => {
        remaining = OTP_SECONDS;
        clearInterval(timerInterval);

        timerInterval = setInterval(() => {
            remaining--;
            const mins = String(Math.floor(remaining / 60)).padStart(2, "0");
            const secs = String(remaining % 60).padStart(2, "0");
            $("#timerDisplay").text(`${mins}:${secs}`);

            if (remaining <= 0) {
                clearInterval(timerInterval);
                this.OnExpired();
            }
        }, 1000);
    };

    this.OnExpired = () => {
        $("#timerContainer").addClass("d-none");
        $("#timerExpired").removeClass("d-none");
        $("#btnVerificar").prop("disabled", true);
        $(".psa-otp-input").prop("disabled", true);
        if (intentos > 0) $("#btnReenviar").prop("disabled", false);
    };

    this.BindOtpInputs = () => {
        $(".psa-otp-input").on("input", function () {
            const val = $(this).val().replace(/\D/g, "");
            $(this).val(val);

            if (val.length === 1) {
                const next = $(".psa-otp-input[data-index='" + (parseInt($(this).data("index")) + 1) + "']");
                if (next.length) next.focus();
            }

            let otp = "";
            $(".psa-otp-input").each(function () { otp += $(this).val(); });
            $("#otpCompleto").val(otp);
            $("#btnVerificar").prop("disabled", otp.length < 6 || remaining <= 0);
        });

        $(".psa-otp-input").on("keydown", function (e) {
            if (e.key === "Backspace" && $(this).val() === "") {
                const prev = $(".psa-otp-input[data-index='" + (parseInt($(this).data("index")) - 1) + "']");
                if (prev.length) prev.focus().val("");
            }
        });

        $(".psa-otp-input").on("paste", (e) => {
            e.preventDefault();
            const pasted = (e.originalEvent.clipboardData || window.clipboardData)
                            .getData("text").replace(/\D/g, "").slice(0, 6);
            $(".psa-otp-input").each(function (i) {
                $(this).val(pasted[i] || "");
            });
            $("#otpCompleto").val(pasted);
            $("#btnVerificar").prop("disabled", pasted.length < 6);
        });
    };

    this.BindEvents = () => {
        $("#frmOtp").on("submit", (e) => {
            e.preventDefault();
            this.Verificar();
        });
        $("#btnReenviar").on("click", () => this.Reenviar());
    };

    this.Verificar = () => {
        const otp    = $("#otpCompleto").val();
        const correo = sessionStorage.getItem("registroCorreo") || "";

        setLoading("#btnVerificar", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         "/Register/VerificarOtp",
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ Email: correo, Codigo: otp }),
            success: (res) => {
                if (res.result === "ok") {
                    clearInterval(timerInterval);
                    Swal.fire({
                        icon:              "success",
                        title:             "\u00a1Cuenta verificada!",
                        text:              "Tu cuenta esta activa. Ya puedes iniciar sesion.",
                        confirmButtonText: "Iniciar sesion",
                        confirmButtonColor: "#78c2ad"
                    }).then(() => { window.location.href = "/Login"; });
                } else {
                    intentos--;
                    $("#intentosNum").text(intentos);
                    $(".psa-otp-input").val("").first().focus();
                    $("#otpCompleto").val("");
                    setLoading("#btnVerificar", false);

                    if (intentos <= 0) {
                        this.BloquearCuenta();
                    } else {
                        showAlert("alertContainer",
                            res.message || `Codigo incorrecto. Te quedan <strong>${intentos}</strong> intento(s).`,
                            "danger");
                    }
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexion. Intenta de nuevo.", "danger");
                setLoading("#btnVerificar", false);
            }
        });
    };

    this.Reenviar = () => {
        if (cooldownActive || reenvios >= MAX_RESENDS) return;

        reenvios++;
        const correo = sessionStorage.getItem("registroCorreo") || "";

        $.ajax({
            url:         "/Register/ReenviarOtp",
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ Email: correo }),
            success: (res) => {
                if (res.result === "ok") {
                    $("#timerContainer").removeClass("d-none");
                    $("#timerExpired").addClass("d-none");
                    $(".psa-otp-input").prop("disabled", false).val("").first().focus();
                    this.StartTimer();
                    showAlert("alertContainer", "Nuevo codigo enviado a tu correo.", "success");
                    this.StartCooldown();
                } else {
                    showAlert("alertContainer", res.message || "No se pudo reenviar el codigo.", "danger");
                }
            },
            error: () => {
                showAlert("alertContainer", "No se pudo reenviar el codigo.", "danger");
            }
        });
    };

    this.StartCooldown = () => {
        cooldownActive = true;
        let cd = COOLDOWN_SECONDS;
        $("#btnReenviar").prop("disabled", true);
        $("#cooldownLabel").removeClass("d-none");

        const iv = setInterval(() => {
            cd--;
            $("#cooldownTimer").text(cd);
            if (cd <= 0) {
                clearInterval(iv);
                cooldownActive = false;
                $("#cooldownLabel").addClass("d-none");
                if (reenvios < MAX_RESENDS) $("#btnReenviar").prop("disabled", false);
            }
        }, 1000);
    };

    this.BloquearCuenta = () => {
        clearInterval(timerInterval);
        $(".psa-otp-input").prop("disabled", true);
        $("#btnReenviar").prop("disabled", true);
        Swal.fire({
            icon:              "error",
            title:             "Cuenta bloqueada",
            text:              "Has superado el numero de intentos. Contacta al soporte para desbloquear tu cuenta.",
            confirmButtonText: "Entendido",
            confirmButtonColor: "#ff7851"
        }).then(() => { window.location.href = "/Login"; });
    };
}

$(document).ready(() => {
    const view = new VerifyOtpView();
    view.InitView();
});
