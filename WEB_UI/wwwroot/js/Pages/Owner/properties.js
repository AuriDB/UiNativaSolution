const estadoBadge = {
    Pendiente:  '<span class="badge bg-warning text-dark">Pendiente</span>',
    EnRevision: '<span class="badge bg-info text-white">En Revisión</span>',
    Aprobada:   '<span class="badge bg-success">Aprobada</span>',
    Devuelta:   '<span class="badge bg-secondary">Devuelta</span>',
    Rechazada:  '<span class="badge bg-danger">Rechazada</span>',
    Vencida:    '<span class="badge bg-dark">Vencida</span>',
};

const colDefs = [
    { field: "id",            headerName: "ID",         width: 80,  sortable: true },
    { field: "hectareas",     headerName: "Hectáreas",  width: 120, valueFormatter: p => p.value?.toFixed(4) },
    { field: "vegetacion",    headerName: "Vegetal %",  width: 110, valueFormatter: p => p.value?.toFixed(2) + "%" },
    { field: "hidrologia",    headerName: "Hídrica %",  width: 110, valueFormatter: p => p.value?.toFixed(2) + "%" },
    { field: "topografia",    headerName: "Topografía", width: 110, valueFormatter: p => p.value?.toFixed(2) },
    {
        field: "esNacional", headerName: "B. Nacional", width: 110,
        cellRenderer: p => p.value ? '<span class="badge bg-success">Sí</span>' : '<span class="badge bg-light text-dark border">No</span>'
    },
    { field: "fechaRegistro", headerName: "Registrada",  width: 160, sortable: true },
    {
        field: "estado", headerName: "Estado", width: 140,
        cellRenderer: p => estadoBadge[p.value] || p.value
    },
    {
        headerName: "Acciones", width: 110, sortable: false, filter: false,
        cellRenderer: p => `<a href="/Owner/PropertyDetail/${p.data.id}" class="btn btn-sm btn-outline-primary rounded-3 py-0">
            <i class="bi bi-eye"></i> Ver
        </a>`
    }
];

const gridOptions = {
    columnDefs: colDefs,
    rowData: [],
    defaultColDef: { resizable: true, filter: true },
    pagination: true,
    paginationPageSize: 20,
    domLayout: "autoHeight"
};

$(document).ready(() => {
    const el = document.getElementById("gridFincas");
    agGrid.createGrid(el, gridOptions);

    $.get(`${API_URL_BASE}/Owner/PropertiesData`, (data) => {
        gridOptions.api?.setRowData(data);
    }).fail(() => {
        showAlert("alertContainer", "Error al cargar las fincas.", "danger");
    });
});
