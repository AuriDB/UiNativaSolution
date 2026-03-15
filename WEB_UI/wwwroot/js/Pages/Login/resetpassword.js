function ResetPasswordView() {

    this.InitView = () => {
        bindTogglePassword("btnToggle1", "nuevaContrasena", "eye1");
        bindTogglePassword("btnToggle2", "confirmarContrasena", "eye2");
        this.BindEvents();
    };

    this.BindEvents = () => {
        $("#nuevaContrasena").on("input", () => {
            const pwd    = $("#nuevaContrasena").val();
            const checks = evaluatePassword(pwd, "");
            const allOk  = Object.values(checks).every(Boolean);
            const match  = pwd === $("#confirmarContrasena").val() && pwd.length > 0;
            $("#btnReset").prop("disabled", !(allOk && match));
        });

        $("#confirmarContrasena").on("input", () => {
            const pwd   = $("#nuevaContrasena").val();
            const conf  = $("#confirmarContrasena").val();
            const match = pwd === conf && conf.length > 0;
            $("#confirmarContrasena").toggleClass("is-valid", match).toggleClass("is-invalid", !match && conf.length > 0);
            const checks = evaluatePassword(pwd, "");
            const allOk  = Object.values(checks).every(Boolean);
            $("#btnReset").prop("disabled", !(allOk && match));
        });

        $("#frmReset").on("submit", (e) => {
            e.preventDefault();
            this.ResetPassword();
        });
    };

    this.ResetPassword = () => {
        const token              = $("#token").val();
        const nuevaContrasena    = $("#nuevaContrasena").val();
        const confirmarContrasena = $("#confirmarContrasena").val();

        if (nuevaContrasena !== confirmarContrasena) {
            showAlert("alertContainer", "Las contraseñas no coinciden.", "warning");
            return;
        }

        setLoading("#btnReset", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         `${API_URL_BASE}/Auth/ResetPassword`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ token, nuevaContrasena }),
            success: (res) => {
                if (res.success) {
                    Swal.fire({
                        icon:             "success",
                        title:            "¡Contraseña actualizada!",
                        text:             "Ahora puedes iniciar sesión con tu nueva contraseña.",
                        confirmButtonText: "Ir al login",
                        confirmButtonColor: "#78c2ad"
                    }).then(() => { window.location.href = "/Auth/Login"; });
                } else {
                    showAlert("alertContainer", res.message || "El enlace es inválido o ha expirado.", "danger");
                    setLoading("#btnReset", false);
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                setLoading("#btnReset", false);
            }
        });
    };
}

$(document).ready(() => {
    const view = new ResetPasswordView();
    view.InitView();
});
