// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Admin/fincasaprobadas.js
//  Activar planes de pago para fincas Aprobadas.
// ═══════════════════════════════════════════════════════

let gridApi = null;

function activarBtn(params) {
    const d = params.data;
    return `<button class="btn btn-sm btn-success rounded-2"
                onclick="activarPlan(${d.id}, '${d.nombreDueno.replace(/'/g,"\\'")}')">
                <i class="bi bi-play-circle me-1"></i>Activar Plan
            </button>`;
}

function activarPlan(activoId, nombreDueno) {
    Swal.fire({
        title:            `¿Activar plan para finca #${activoId}?`,
        text:             `Dueño: ${nombreDueno}. Se generarán 12 cuotas mensuales.`,
        icon:             "question",
        showCancelButton: true,
        confirmButtonText: "Sí, activar",
        confirmButtonColor: "#78c2ad"
    }).then(r => {
        if (!r.isConfirmed) return;

        $.ajax({
            url: `${API_URL_BASE}/Admin/ActivarPlan`, method: "POST",
            contentType: "application/json",
            data: JSON.stringify({ activoId }),
            success: res => {
                if (res.success) {
                    Swal.fire({
                        icon: "success", title: "Plan activado",
                        text: res.message, confirmButtonColor: "#78c2ad"
                    });
                    cargarFincas();
                } else {
                    showAlert("alertContainer", res.message, "danger");
                }
            },
            error: () => showAlert("alertContainer", "Error de conexión.", "danger")
        });
    });
}

function cargarFincas() {
    $.getJSON(`${API_URL_BASE}/Admin/FincasAprobadasData`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => showAlert("alertContainer", "Error al cargar las fincas.", "danger"));
}

$(document).ready(() => {
    const colDefs = [
        { field: "id",              headerName: "#",        width: 70 },
        { field: "nombreDueno",     headerName: "Dueño",    flex: 1,   sortable: true },
        { field: "nombreIngeniero", headerName: "Ingeniero", flex: 1,  sortable: true },
        { field: "hectareas",       headerName: "Hectáreas", width: 120,
          valueFormatter: p => p.value?.toFixed(4) },
        { field: "fechaRegistro",   headerName: "F. Aprobación", width: 140, sortable: true },
        { headerName: "Acción",     width: 150,
          cellRenderer: activarBtn, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs: colDefs, rowData: [],
        pagination: true, paginationPageSize: 10,
        defaultColDef: { filter: true, resizable: true },
        localeText: { noRowsToShow: "No hay fincas aprobadas pendientes de activar plan." }
    };

    gridApi = agGrid.createGrid(document.getElementById("gridFincas"), gridOptions);
    cargarFincas();
});
