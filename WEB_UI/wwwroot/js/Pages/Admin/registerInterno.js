// Usado tanto en RegisterAdmin como en RegisterEngineer.
// El campo hidden #idRol determina el rol (1=Admin, 2=Ingeniero).

function RegisterInternoView() {

    this.InitView = () => {
        bindTogglePassword("btnTogglePwd", "password", "eyePwd");
        this.BindEvents();
    };

    this.BindEvents = () => {
        $("#password").on("input", () => {
            const pwd    = $("#password").val();
            const nombre = $("#nombre").val();
            evaluatePassword(pwd, nombre);
        });

        $("#confirmarPassword").on("input", () => {
            const pwd  = $("#password").val();
            const conf = $("#confirmarPassword").val();
            const match = pwd === conf && conf.length > 0;
            $("#confirmarPassword").toggleClass("is-valid", match)
                                    .toggleClass("is-invalid", !match && conf.length > 0);
        });

        $("#frmRegistroInterno").on("submit", (e) => {
            e.preventDefault();
            this.Registrar();
        });
    };

    this.Registrar = () => {
        const pwd  = $("#password").val();
        const conf = $("#confirmarPassword").val();

        if (pwd !== conf) {
            showAlert("alertContainer", "Las contrase\u00f1as no coinciden.", "danger");
            return;
        }

        const payload = {
            IdRol:           parseInt($("#idRol").val()),
            Nombre:          $("#nombre").val().trim(),
            PrimerApellido:  $("#primerApellido").val().trim(),
            SegundoApellido: $("#segundoApellido").val().trim(),
            Email:           $("#email").val().trim(),
            Cedula:          $("#cedula").val().trim(),
            Password:        pwd
        };

        setLoading("#btnRegistrar", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         `${API_URL_BASE}/Usuario/RegistrarInterno`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify(payload),
            success: (res) => {
                if (res.result === "ok") {
                    Swal.fire({
                        icon:              "success",
                        title:             "\u00a1Registrado!",
                        text:              res.data || "Usuario registrado exitosamente.",
                        confirmButtonText: "Aceptar",
                        confirmButtonColor: "#78c2ad"
                    }).then(() => { window.location.href = "/Admin/Users"; });
                } else {
                    showAlert("alertContainer", res.message || "Error al registrar.", "danger");
                    setLoading("#btnRegistrar", false);
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexi\u00f3n. Intenta de nuevo.", "danger");
                setLoading("#btnRegistrar", false);
            }
        });
    };
}

$(document).ready(() => {
    const view = new RegisterInternoView();
    view.InitView();
});
