// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Ingeniero/misaprobadas.js
//  Fincas aprobadas sin plan — CU24 Activar Plan
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
        title:            `¿Activar plan PSA para finca #${activoId}?`,
        text:             `Dueño: ${nombreDueno}. Se generarán 12 cuotas mensuales según parámetros vigentes.`,
        icon:             "question",
        showCancelButton: true,
        confirmButtonText: "Sí, activar",
        confirmButtonColor: "#78c2ad"
    }).then(r => {
        if (!r.isConfirmed) return;

        $.ajax({
            url: `${API_URL_BASE}/Ingeniero/ActivarPlan`,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({ activoId }),
            success: res => {
                if (res.success) {
                    Swal.fire({
                        icon: "success", title: "Plan activado",
                        text: res.message, confirmButtonColor: "#78c2ad"
                    });
                    // Quitar la finca de la tabla (ya tiene plan)
                    $.getJSON(`${API_URL_BASE}/Ingeniero/MisAprobadasData`, data => {
                        gridApi.setGridOption("rowData", data);
                    });
                } else {
                    showAlert("alertContainer", res.message, "danger");
                }
            },
            error: () => showAlert("alertContainer", "Error de conexión.", "danger")
        });
    });
}

$(document).ready(() => {
    const colDefs = [
        { field: "id",            headerName: "#",          width: 70 },
        { field: "nombreDueno",   headerName: "Dueño",      flex: 1,   sortable: true },
        { field: "hectareas",     headerName: "Hectáreas",  width: 120,
          valueFormatter: p => p.value?.toFixed(4) },
        { field: "fechaRegistro", headerName: "F. Aprobación", width: 140, sortable: true },
        { headerName: "Acción",   width: 150,
          cellRenderer: activarBtn, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs: colDefs, rowData: [],
        pagination: true, paginationPageSize: 10,
        defaultColDef: { filter: true, resizable: true },
        localeText: { noRowsToShow: "Todas tus fincas aprobadas ya tienen plan activo." }
    };

    gridApi = agGrid.createGrid(document.getElementById("gridAprobadas"), gridOptions);

    $.getJSON(`${API_URL_BASE}/Ingeniero/MisAprobadasData`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => showAlert("alertContainer", "Error al cargar las fincas.", "danger"));
});
