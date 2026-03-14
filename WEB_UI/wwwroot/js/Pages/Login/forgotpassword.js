function ForgotPasswordView() {

    this.InitView = () => {
        this.BindEvents();
    };

    this.BindEvents = () => {
        $("#frmForgot").on("submit", (e) => {
            e.preventDefault();
            this.EnviarEnlace();
        });
    };

    this.EnviarEnlace = () => {
        const correo = $("#correo").val().trim();
        if (!correo) {
            showAlert("alertContainer", "Por favor ingresa tu correo electrónico.", "warning");
            return;
        }

        setLoading("#btnEnviar", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         `${API_URL_BASE}/Auth/ForgotPassword`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ correo }),
            success: () => {
                // Siempre mostrar confirmación (no revelar si el correo existe)
                $("#stepCorreo").addClass("d-none");
                $("#stepConfirmacion").removeClass("d-none");
            },
            error: () => {
                // Igual: mostrar confirmación para evitar enumeración
                $("#stepCorreo").addClass("d-none");
                $("#stepConfirmacion").removeClass("d-none");
            }
        });
    };
}

$(document).ready(() => {
    const view = new ForgotPasswordView();
    view.InitView();
});
