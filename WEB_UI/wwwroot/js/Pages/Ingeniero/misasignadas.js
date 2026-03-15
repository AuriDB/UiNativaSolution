// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Ingeniero/misasignadas.js
//  Tabla Ag-Grid de fincas en revisión asignadas al Ing.
// ═══════════════════════════════════════════════════════

let gridApi = null;

function evaluarBtn(params) {
    const d = params.data;
    return `<a href="/Ingeniero/Evaluar/${d.id}"
               class="btn btn-sm btn-outline-primary rounded-2">
                <i class="bi bi-clipboard2-check me-1"></i>Evaluar
            </a>`;
}

$(document).ready(() => {

    const colDefs = [
        { field: "id",            headerName: "#",          width: 70,  sortable: true },
        { field: "nombreDueno",   headerName: "Dueño",      flex: 1,    sortable: true },
        { field: "hectareas",     headerName: "Hectáreas",  width: 120, sortable: true,
          valueFormatter: p => p.value?.toFixed(4) },
        { field: "fechaRegistro", headerName: "F. Registro", width: 150, sortable: true },
        { headerName: "Acción",   width: 120,
          cellRenderer: evaluarBtn, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs:         colDefs,
        rowData:            [],
        pagination:         true,
        paginationPageSize: 10,
        defaultColDef:      { filter: true, resizable: true },
        localeText:         { noRowsToShow: "No tienes fincas asignadas actualmente." }
    };

    const container = document.getElementById("gridAsignadas");
    gridApi = agGrid.createGrid(container, gridOptions);

    $.getJSON(`${API_URL_BASE}/Ingeniero/MisAsignadasData`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => {
        showAlert("alertContainer", "Error al cargar las fincas asignadas.", "danger");
    });
});
