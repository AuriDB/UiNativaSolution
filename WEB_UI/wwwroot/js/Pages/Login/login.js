function LoginView() {

    this.InitView = () => {
        bindTogglePassword("btnTogglePwd", "contrasena", "eyeIcon");
        this.BindEvents();
    };

    this.BindEvents = () => {
        $("#frmLogin").on("submit", (e) => {
            e.preventDefault();
            this.Login();
        });
    };

    this.Login = () => {
        const correo     = $("#correo").val().trim();
        const contrasena = $("#contrasena").val();

        if (!correo || !contrasena) {
            showAlert("alertContainer", "Por favor completa todos los campos.", "warning");
            return;
        }

        setLoading("#btnLogin", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         `${API_URL_BASE}/Auth/Login`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ correo, contrasena }),
            success: (res) => {
                if (res.success) {
                    window.location.href = "/Home";
                } else {
                    showAlert("alertContainer", res.message || "Credenciales incorrectas.", "danger");
                    setLoading("#btnLogin", false);
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                setLoading("#btnLogin", false);
            }
        });
    };
}

$(document).ready(() => {
    const view = new LoginView();
    view.InitView();
});
