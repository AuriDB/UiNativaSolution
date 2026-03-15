// ── Ingeniero/Cola — Ag-Grid FIFO + Tomar finca ─────────────────────────────

let rowVersionMap = {};   // id → rowVersion base64

const colDefs = [
    { field: "id",            headerName: "ID",          width: 80,  sortable: true },
    { field: "nombreDueno",   headerName: "Dueño",       flex: 1,    minWidth: 160 },
    { field: "hectareas",     headerName: "Hectáreas",   width: 115, valueFormatter: p => p.value?.toFixed(4) },
    { field: "vegetacion",    headerName: "Vegetal %",   width: 105, valueFormatter: p => p.value?.toFixed(2) + "%" },
    { field: "hidrologia",    headerName: "Hídrica %",   width: 105, valueFormatter: p => p.value?.toFixed(2) + "%" },
    { field: "topografia",    headerName: "Topografía",  width: 110, valueFormatter: p => p.value?.toFixed(2) },
    {
        field: "esNacional", headerName: "B. Nac.", width: 90,
        cellRenderer: p => p.value ? '<span class="badge bg-success">Sí</span>' : '<span class="badge bg-light text-dark border">No</span>'
    },
    { field: "fechaRegistro", headerName: "Registrada",  width: 165, sortable: true },
    {
        headerName: "Acción", width: 120, sortable: false, filter: false,
        cellRenderer: p => `<button class="btn btn-sm btn-primary rounded-3 py-0 btn-tomar" data-id="${p.data.id}">
            <i class="bi bi-hand-index me-1"></i>Tomar
        </button>`
    }
];

const gridOptions = {
    columnDefs: colDefs,
    rowData: [],
    defaultColDef: { resizable: true, filter: true },
    pagination: true,
    paginationPageSize: 15,
    onCellClicked: (e) => {
        if (e.event.target.closest(".btn-tomar")) {
            const id = parseInt(e.event.target.closest(".btn-tomar").dataset.id);
            tomarFinca(id);
        }
    }
};

function cargarCola() {
    $.get(`${API_URL_BASE}/Ingeniero/Cola/Data`, (data) => {
        rowVersionMap = {};
        data.forEach(f => { rowVersionMap[f.id] = f.rowVersion; });
        gridOptions.api?.setRowData(data);
        clearAlert("alertCola");
    }).fail(() => {
        showAlert("alertCola", "Error al cargar la cola.", "danger");
    });
}

function tomarFinca(id) {
    const rv = rowVersionMap[id];
    if (!rv) {
        showAlert("alertCola", "RowVersion no disponible. Actualiza la cola.", "warning");
        return;
    }

    Swal.fire({
        title: `¿Tomar finca #${id}?`,
        text:  "Pasará a estado 'En Revisión' y será asignada a ti.",
        icon:  "question",
        showCancelButton: true,
        confirmButtonText: "Sí, tomar",
        cancelButtonText:  "Cancelar"
    }).then(result => {
        if (!result.isConfirmed) return;

        $.ajax({
            url:         `${API_URL_BASE}/Ingeniero/Cola/Tomar/${id}`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ rowVersion: rv }),
            success: (res) => {
                if (res.success) {
                    Swal.fire({ icon: "success", title: "¡Finca tomada!", text: res.message })
                        .then(() => { window.location.href = `/Ingeniero/Fincas/Evaluar/${id}`; });
                } else {
                    showAlert("alertCola", res.message, "danger");
                    cargarCola(); // refrescar para obtener nuevo rowVersion
                }
            },
            error: (xhr) => {
                const msg = xhr.responseJSON?.message || "Error al tomar la finca.";
                if (xhr.status === 409) {
                    Swal.fire({ icon: "warning", title: "Conflicto (409)", text: msg })
                        .then(() => cargarCola());
                } else {
                    showAlert("alertCola", msg, "danger");
                }
            }
        });
    });
}

$(document).ready(() => {
    const el = document.getElementById("gridCola");
    agGrid.createGrid(el, gridOptions);
    cargarCola();

    $("#btnRefrescar").on("click", cargarCola);
});
