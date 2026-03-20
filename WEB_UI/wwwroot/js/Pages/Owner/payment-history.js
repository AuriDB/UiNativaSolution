const estadoPagoBadge = {
    Pendiente: '<span class="badge bg-warning text-dark">Pendiente</span>',
    Ejecutado: '<span class="badge bg-success">Ejecutado</span>',
};

const colDefs = [
    { field: "numeroPago",    headerName: "#",          width: 70 },
    { field: "fincaId",       headerName: "Finca ID",   width: 100 },
    { field: "monto",         headerName: "Monto (₡)",  width: 140, valueFormatter: p => `₡${(p.value || 0).toLocaleString("es-CR", { minimumFractionDigits: 2 })}` },
    { field: "fechaPago",     headerName: "Fecha Pago", width: 160, sortable: true },
    { field: "fechaEjecucion",headerName: "Ejecutado",  width: 160 },
    { field: "estado",        headerName: "Estado",     width: 130, cellRenderer: p => estadoPagoBadge[p.value] || p.value },
];

const gridOptions = {
    columnDefs: colDefs,
    rowData: [],
    defaultColDef: { resizable: true, filter: true },
    pagination: true,
    paginationPageSize: 20,
};

$(document).ready(() => {
    const el = document.getElementById("gridPagos");
    agGrid.createGrid(el, gridOptions);

    $.get(`${API_URL_BASE}/Owner/PaymentHistoryData`, (data) => {
        gridOptions.api?.setRowData(data);
    });
});
