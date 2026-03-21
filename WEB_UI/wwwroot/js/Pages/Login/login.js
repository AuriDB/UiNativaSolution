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

    // El login va por el MVC controller para poder establecer la sesion server-side.
    // LoginController.Authenticate llama al API, setea la sesion y retorna { result: "ok" }
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
            url:         "/Login/Authenticate",
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ Email: correo, Password: contrasena }),
            success: (res) => {
                if (res.result === "ok") {
                    window.location.href = "/Home";
                } else {
                    showAlert("alertContainer", res.message || "Credenciales incorrectas.", "danger");
                    setLoading("#btnLogin", false);
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexion. Intenta de nuevo.", "danger");
                setLoading("#btnLogin", false);
            }
        });
    };
}

$(document).ready(() => {
    const view = new LoginView();
    view.InitView();
});
