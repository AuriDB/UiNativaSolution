function VerifyOtpView() {

    const OTP_SECONDS     = 90;   // vigencia del OTP
    const COOLDOWN_SECONDS = 30;  // cooldown entre reenvíos
    const MAX_ATTEMPTS    = 3;
    const MAX_RESENDS     = 3;

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

    // ── Timer countdown ───────────────────────────────
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

    // ── Inputs OTP: focus automático ─────────────────
    this.BindOtpInputs = () => {
        $(".psa-otp-input").on("input", function () {
            const val   = $(this).val().replace(/\D/g, "");
            $(this).val(val);

            if (val.length === 1) {
                const next = $(".psa-otp-input[data-index='" + (parseInt($(this).data("index")) + 1) + "']");
                if (next.length) next.focus();
            }

            // Componer código completo
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

        // Pegar código completo
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

    // ── Eventos ───────────────────────────────────────
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
            url:         `${API_URL_BASE}/Auth/VerifyOtp`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ correo, otp }),
            success: (res) => {
                if (res.success) {
                    clearInterval(timerInterval);
                    Swal.fire({
                        icon:             "success",
                        title:            "¡Cuenta verificada!",
                        text:             "Tu cuenta está activa. Ya puedes iniciar sesión.",
                        confirmButtonText: "Iniciar sesión",
                        confirmButtonColor: "#78c2ad"
                    }).then(() => { window.location.href = "/Auth/Login"; });
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
                            `Código incorrecto. Te quedan <strong>${intentos}</strong> intento(s).`, "danger");
                    }
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                setLoading("#btnVerificar", false);
            }
        });
    };

    this.Reenviar = () => {
        if (cooldownActive || reenvios >= MAX_RESENDS) return;

        reenvios++;
        const correo = sessionStorage.getItem("registroCorreo") || "";

        $.ajax({
            url:         `${API_URL_BASE}/Auth/ResendOtp`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ correo }),
            success: () => {
                // Reiniciar timer de OTP
                $("#timerContainer").removeClass("d-none");
                $("#timerExpired").addClass("d-none");
                $(".psa-otp-input").prop("disabled", false).val("").first().focus();
                this.StartTimer();
                showAlert("alertContainer", "Nuevo código enviado a tu correo.", "success");
                this.StartCooldown();
            },
            error: () => {
                showAlert("alertContainer", "No se pudo reenviar el código.", "danger");
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
            icon:             "error",
            title:            "Cuenta bloqueada",
            text:             "Has superado el número de intentos. Contacta al soporte para desbloquear tu cuenta.",
            confirmButtonText: "Entendido",
            confirmButtonColor: "#ff7851"
        }).then(() => { window.location.href = "/Auth/Login"; });
    };
}

$(document).ready(() => {
    const view = new VerifyOtpView();
    view.InitView();
});
