// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Dueno/index.js
//  Tabla Ag-Grid de fincas del dueño + acciones
// ═══════════════════════════════════════════════════════

let gridApi = null;
let idCancelarPendiente = null;

// Renderizador de badge de estado
function estadoBadge(params) {
    const colorMap = {
        warning:   "bg-warning text-dark",
        info:      "bg-info text-white",
        success:   "bg-success text-white",
        secondary: "bg-secondary text-white",
        danger:    "bg-danger text-white",
        dark:      "bg-dark text-white"
    };
    const cls = colorMap[params.data.estadoColor] || "bg-light text-dark";
    return `<span class="badge ${cls} px-2 py-1">${params.value}</span>`;
}

// Renderizador de botones de acción
function accionesBtns(params) {
    const d = params.data;
    let html = `<a href="/Dueno/Detalle/${d.id}" class="btn btn-sm btn-outline-primary rounded-2 me-1">
                    <i class="bi bi-eye me-1"></i>Ver
                </a>`;

    if (d.puedeEditar) {
        html += `<a href="/Dueno/Editar/${d.id}" class="btn btn-sm btn-outline-warning rounded-2 me-1">
                     <i class="bi bi-pencil me-1"></i>Editar
                 </a>`;
    }

    if (d.puedeCancelar) {
        html += `<button class="btn btn-sm btn-outline-danger rounded-2" onclick="confirmarCancelar(${d.id})">
                     <i class="bi bi-x-circle me-1"></i>Cancelar
                 </button>`;
    }

    return html;
}

// Abre el modal de cancelación
function confirmarCancelar(id) {
    idCancelarPendiente = id;
    new bootstrap.Modal(document.getElementById("modalCancelar")).show();
}

$(document).ready(() => {

    // ── Configuración Ag-Grid ───────────────────────────
    const colDefs = [
        { field: "id",            headerName: "#",              width: 70,  sortable: true },
        { field: "hectareas",     headerName: "Hectáreas",      width: 120, sortable: true,
          valueFormatter: p => p.value?.toFixed(4) },
        { field: "estado",        headerName: "Estado",         width: 140,
          cellRenderer: estadoBadge },
        { field: "fechaRegistro", headerName: "F. Registro",    width: 130, sortable: true },
        { field: "lat",           headerName: "Latitud",         width: 110,
          valueFormatter: p => p.value?.toFixed(6) },
        { field: "lng",           headerName: "Longitud",        width: 110,
          valueFormatter: p => p.value?.toFixed(6) },
        { headerName: "Acciones", minWidth: 230, flex: 1,
          cellRenderer: accionesBtns, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs:       colDefs,
        rowData:          [],
        pagination:       true,
        paginationPageSize: 10,
        defaultColDef:    { filter: true, resizable: true },
        localeText:       { noRowsToShow: "No tienes fincas registradas." }
    };

    const container = document.getElementById("gridFincas");
    gridApi = agGrid.createGrid(container, gridOptions);

    // ── Cargar datos ────────────────────────────────────
    $.getJSON(`${API_URL_BASE}/Dueno/MisFincas`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => {
        showAlert("alertContainer", "Error al cargar las fincas.", "danger");
    });

    // ── Confirmar cancelación ───────────────────────────
    $("#btnConfirmarCancelar").on("click", () => {
        if (!idCancelarPendiente) return;

        $.ajax({
            url:         `${API_URL_BASE}/Dueno/Cancelar`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ id: idCancelarPendiente }),
            success: (res) => {
                bootstrap.Modal.getInstance(document.getElementById("modalCancelar")).hide();
                if (res.success) {
                    // Actualizar fila en el grid sin recargar
                    $.getJSON(`${API_URL_BASE}/Dueno/MisFincas`, data => {
                        gridApi.setGridOption("rowData", data);
                    });
                    showAlert("alertContainer", res.message, "success");
                } else {
                    showAlert("alertContainer", res.message, "danger");
                }
                idCancelarPendiente = null;
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión.", "danger");
            }
        });
    });

});
