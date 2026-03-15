// ── Dueno/Cuenta — Registrar / Actualizar IBAN ──────────────────────────────

function validarIban(iban) {
    return /^CR\d{20}$/.test(iban);
}

$(document).ready(() => {
    // Validación visual IBAN en tiempo real
    $("#iban").on("input", function () {
        const val = $(this).val().trim().toUpperCase();
        $(this).val(val);
        const ok = validarIban(val);
        $(this).toggleClass("is-valid", ok).toggleClass("is-invalid", !ok && val.length > 0);
        $("#ibanFeedback").text(ok ? "" : "Formato inválido. Debe ser CR + 20 dígitos.");
    });

    // Submit
    $("#frmCuenta").on("submit", (e) => {
        e.preventDefault();
        clearAlert("alertContainer");

        const iban = $("#iban").val().trim().toUpperCase();
        if (!validarIban(iban)) {
            showAlert("alertContainer", "El IBAN no es válido. Debe ser CR seguido de 20 dígitos.", "warning");
            return;
        }

        const payload = {
            banco:      $("#banco").val(),
            tipoCuenta: $("#tipoCuenta").val(),
            titular:    $("#titular").val().trim(),
            iban:       iban
        };

        if (!payload.banco || !payload.tipoCuenta || !payload.titular) {
            showAlert("alertContainer", "Por favor completa todos los campos.", "warning");
            return;
        }

        setLoading("#btnGuardarCuenta", true);

        $.ajax({
            url:         `${API_URL_BASE}/Dueno/CuentaBancaria`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify(payload),
            success: (res) => {
                setLoading("#btnGuardarCuenta", false);
                if (res.success) {
                    Swal.fire({ icon: "success", title: "¡Listo!", text: res.message })
                        .then(() => window.location.reload());
                } else {
                    showAlert("alertContainer", res.message || "Error al guardar.", "danger");
                }
            },
            error: () => {
                setLoading("#btnGuardarCuenta", false);
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
            }
        });
    });
});
