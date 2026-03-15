// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Dueno/cuentas.js
//  Gestión de cuentas bancarias IBAN del Dueño.
// ═══════════════════════════════════════════════════════

let gridApi = null;

// Normaliza IBAN: quita espacios y convierte a mayúsculas
function normalizarIban(valor) {
    return valor.replace(/\s+/g, "").toUpperCase();
}

// Renderizador badge activo/inactivo
function estadoCuentaRenderer(params) {
    return params.value
        ? `<span class="badge bg-success text-white px-2">Activa</span>`
        : `<span class="badge bg-secondary text-white px-2">Inactiva</span>`;
}

// Renderizador botón desactivar
function desactivarBtn(params) {
    if (!params.data.activo) return "—";
    return `<button class="btn btn-sm btn-outline-danger rounded-2"
                onclick="confirmarDesactivar(${params.data.id})">
                <i class="bi bi-x-circle me-1"></i>Desactivar
            </button>`;
}

function confirmarDesactivar(id) {
    Swal.fire({
        title:             "¿Desactivar esta cuenta?",
        text:              "No podrás usarla para recibir pagos PSA.",
        icon:              "warning",
        showCancelButton:  true,
        confirmButtonText: "Sí, desactivar",
        cancelButtonText:  "Cancelar",
        confirmButtonColor: "#dc3545"
    }).then(result => {
        if (!result.isConfirmed) return;

        $.ajax({
            url:         `${API_URL_BASE}/Dueno/Cuenta/Desactivar`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ id }),
            success: (res) => {
                if (res.success) {
                    showAlert("alertContainer", res.message, "success");
                    cargarCuentas();
                } else {
                    showAlert("alertContainer", res.message, "danger");
                }
            },
            error: () => showAlert("alertContainer", "Error de conexión.", "danger")
        });
    });
}

function cargarCuentas() {
    $.getJSON(`${API_URL_BASE}/Dueno/Cuenta/MisCuentas`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => {
        showAlert("alertContainer", "Error al cargar las cuentas.", "danger");
    });
}

$(document).ready(() => {

    // ── Ag-Grid ─────────────────────────────────────────
    const colDefs = [
        { field: "banco",        headerName: "Banco",        flex: 1,   sortable: true },
        { field: "tipoCuenta",   headerName: "Tipo",         width: 120, sortable: true },
        { field: "titular",      headerName: "Titular",      flex: 1,   sortable: true },
        { field: "ibanOfuscado", headerName: "IBAN",         width: 180 },
        { field: "activo",       headerName: "Estado",       width: 110,
          cellRenderer: estadoCuentaRenderer },
        { field: "fechaCreacion", headerName: "Registrada",  width: 120, sortable: true },
        { headerName: "Acción",   width: 140,
          cellRenderer: desactivarBtn, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs:         colDefs,
        rowData:            [],
        pagination:         true,
        paginationPageSize: 10,
        defaultColDef:      { filter: true, resizable: true },
        localeText:         { noRowsToShow: "No tienes cuentas bancarias registradas." }
    };

    gridApi = agGrid.createGrid(document.getElementById("gridCuentas"), gridOptions);
    cargarCuentas();

    // ── Formateo live del IBAN ───────────────────────────
    $("#inpIban").on("input", function () {
        // Eliminar lo que no sea alfanumérico y pasar a mayúsculas
        let val = $(this).val().replace(/[^a-zA-Z0-9]/g, "").toUpperCase();
        // Limitar a 22 caracteres (CR + 20 dígitos)
        if (val.length > 22) val = val.slice(0, 22);
        $(this).val(val);
    });

    // ── Agregar cuenta ───────────────────────────────────
    $("#btnAgregarCuenta").on("click", () => {
        clearAlert("alertModal");

        const banco      = $("#selBanco").val()?.trim();
        const tipoCuenta = $("#selTipo").val()?.trim();
        const titular    = $("#inpTitular").val()?.trim();
        const iban       = normalizarIban($("#inpIban").val());

        if (!banco || !tipoCuenta || !titular || !iban) {
            showAlert("alertModal", "Todos los campos son obligatorios.", "warning");
            return;
        }

        setLoading("#btnAgregarCuenta", true);

        $.ajax({
            url:         `${API_URL_BASE}/Dueno/Cuenta/Agregar`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ banco, tipoCuenta, titular, iban }),
            success: (res) => {
                setLoading("#btnAgregarCuenta", false);
                if (res.success) {
                    bootstrap.Modal.getInstance(document.getElementById("modalAgregar")).hide();
                    // Limpiar formulario
                    $("#selBanco, #selTipo").val("");
                    $("#inpTitular, #inpIban").val("");
                    showAlert("alertContainer", res.message, "success");
                    cargarCuentas();
                } else {
                    showAlert("alertModal", res.message, "danger");
                }
            },
            error: () => {
                setLoading("#btnAgregarCuenta", false);
                showAlert("alertModal", "Error de conexión. Intenta de nuevo.", "danger");
            }
        });
    });
});
