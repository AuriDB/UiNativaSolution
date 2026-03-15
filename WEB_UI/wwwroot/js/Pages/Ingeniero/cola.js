// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Ingeniero/cola.js
//  Cola FIFO de fincas pendientes + TomarFinca (CU18)
//  Concurrencia optimista con RowVersion.
// ═══════════════════════════════════════════════════════

let gridApi = null;

// Renderizador de mapa en badge lat/lng
function ubicacionRenderer(params) {
    const d = params.data;
    return `<span class="text-muted small">${d.lat?.toFixed(4)}, ${d.lng?.toFixed(4)}</span>`;
}

// Renderizador botón Tomar
function tomarBtn(params) {
    const d = params.data;
    return `<button class="btn btn-sm btn-primary rounded-2"
                onclick="tomarFinca(${d.id}, '${d.rowVersion}')">
                <i class="bi bi-hand-index me-1"></i>Tomar
            </button>`;
}

// Tomar finca con RowVersion (CU18)
function tomarFinca(id, rowVersion) {
    Swal.fire({
        title: "¿Tomar esta finca?",
        text:  "La finca pasará a tu lista de 'Mis Asignadas' y saldrá de la cola.",
        icon:  "question",
        showCancelButton:   true,
        confirmButtonText:  "Sí, tomar",
        cancelButtonText:   "Cancelar",
        confirmButtonColor: "#78c2ad"
    }).then(result => {
        if (!result.isConfirmed) return;

        $.ajax({
            url:         `${API_URL_BASE}/Ingeniero/TomarFinca`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ id, rowVersion }),
            success: (res) => {
                if (res.success) {
                    Swal.fire({
                        icon:             "success",
                        title:            "¡Finca tomada!",
                        text:             res.message,
                        confirmButtonText: "Ver Mis Asignadas",
                        confirmButtonColor: "#78c2ad"
                    }).then(() => {
                        window.location.href = "/Ingeniero/MisAsignadas";
                    });
                } else if (res.conflict) {
                    // Otro ingeniero ganó la carrera — refrescar la cola
                    showAlert("alertContainer", res.message, "warning");
                    cargarCola();
                } else {
                    showAlert("alertContainer", res.message || "Error al tomar la finca.", "danger");
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
            }
        });
    });
}

function cargarCola() {
    $.getJSON(`${API_URL_BASE}/Ingeniero/ColaFifo`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => {
        showAlert("alertContainer", "Error al cargar la cola.", "danger");
    });
}

$(document).ready(() => {

    const colDefs = [
        { field: "id",            headerName: "#",          width: 70,  sortable: true },
        { field: "nombreDueno",   headerName: "Dueño",      flex: 1,    sortable: true },
        { field: "hectareas",     headerName: "Hectáreas",  width: 120, sortable: true,
          valueFormatter: p => p.value?.toFixed(4) },
        { field: "fechaRegistro", headerName: "En cola desde", width: 150, sortable: true },
        { headerName: "Ubicación", width: 160,
          cellRenderer: ubicacionRenderer, sortable: false, filter: false },
        { headerName: "Acción", width: 120,
          cellRenderer: tomarBtn, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs:         colDefs,
        rowData:            [],
        pagination:         true,
        paginationPageSize: 10,
        defaultColDef:      { filter: true, resizable: true },
        localeText:         { noRowsToShow: "No hay fincas pendientes en la cola." }
    };

    const container = document.getElementById("gridCola");
    gridApi = agGrid.createGrid(container, gridOptions);

    cargarCola();

    // Refrescar manualmente
    $("#btnRefrescar").on("click", () => {
        clearAlert("alertContainer");
        cargarCola();
    });
});
