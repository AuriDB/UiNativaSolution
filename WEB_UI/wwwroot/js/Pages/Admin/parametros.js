// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Admin/parametros.js
//  Configuración de parámetros de pago PSA (Opción A/B).
// ═══════════════════════════════════════════════════════

let gridApi = null;

function vigenteRenderer(params) {
    return params.value
        ? `<span class="badge bg-success px-2">Vigente</span>`
        : `<span class="badge bg-secondary px-2">Histórico</span>`;
}

function pctFormatter(params) {
    return params.value != null ? `${(params.value * 100).toFixed(2)} %` : "—";
}

$(document).ready(() => {

    const colDefs = [
        { field: "id",           headerName: "#",        width: 65, sortable: true },
        { field: "precioBase",   headerName: "Precio Base (₡)", width: 150, sortable: true,
          valueFormatter: p => p.value?.toLocaleString("es-CR", {minimumFractionDigits: 2}) },
        { field: "pctVegetacion", headerName: "Veg.", width: 90, valueFormatter: pctFormatter },
        { field: "pctHidrologia", headerName: "Hid.", width: 90, valueFormatter: pctFormatter },
        { field: "pctNacional",   headerName: "Nac.", width: 90, valueFormatter: pctFormatter },
        { field: "pctTopografia", headerName: "Top.", width: 90, valueFormatter: pctFormatter },
        { field: "tope",          headerName: "Tope",  width: 90, valueFormatter: pctFormatter },
        { field: "vigente",       headerName: "Estado", width: 110, cellRenderer: vigenteRenderer },
        { field: "creadoPor",     headerName: "Por",    flex: 1, sortable: true },
        { field: "fechaCreacion", headerName: "Fecha",  width: 140, sortable: true }
    ];

    const gridOptions = {
        columnDefs: colDefs, rowData: [],
        pagination: true, paginationPageSize: 10,
        defaultColDef: { filter: true, resizable: true },
        localeText: { noRowsToShow: "No hay parámetros configurados aún." }
    };

    gridApi = agGrid.createGrid(document.getElementById("gridParametros"), gridOptions);

    // Cargar historial
    $.getJSON(`${API_URL_BASE}/Admin/ParametrosData`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => showAlert("alertContainer", "Error al cargar los parámetros.", "danger"));

    // Guardar nueva configuración
    $("#btnGuardarParametros").on("click", () => {
        clearAlert("alertContainer");

        const payload = {
            precioBase:    parseFloat($("#pPrecioBase").val()),
            pctVegetacion: parseFloat($("#pPctVeg").val()) || 0,
            pctHidrologia: parseFloat($("#pPctHid").val()) || 0,
            pctNacional:   parseFloat($("#pPctNac").val()) || 0,
            pctTopografia: parseFloat($("#pPctTop").val()) || 0,
            tope:          parseFloat($("#pTope").val())
        };

        if (isNaN(payload.precioBase) || payload.precioBase <= 0) {
            showAlert("alertContainer", "El precio base debe ser un número mayor a 0.", "warning");
            return;
        }
        if (isNaN(payload.tope) || payload.tope <= 0 || payload.tope > 1) {
            showAlert("alertContainer", "El tope debe estar entre 0.01 y 1.00.", "warning");
            return;
        }

        Swal.fire({
            title:            "¿Activar esta configuración?",
            text:             "Los parámetros anteriores dejarán de ser vigentes.",
            icon:             "warning",
            showCancelButton: true,
            confirmButtonText: "Sí, activar",
            confirmButtonColor: "#78c2ad"
        }).then(r => {
            if (!r.isConfirmed) return;

            setLoading("#btnGuardarParametros", true);
            $.ajax({
                url: `${API_URL_BASE}/Admin/CrearParametros`, method: "POST",
                contentType: "application/json", data: JSON.stringify(payload),
                success: res => {
                    setLoading("#btnGuardarParametros", false);
                    if (res.success) {
                        showAlert("alertContainer", res.message, "success");
                        // Limpiar formulario
                        $("#pPrecioBase, #pPctVeg, #pPctHid, #pPctNac, #pPctTop, #pTope").val("");
                        // Recargar tabla
                        $.getJSON(`${API_URL_BASE}/Admin/ParametrosData`, data => {
                            gridApi.setGridOption("rowData", data);
                        });
                    } else showAlert("alertContainer", res.message, "danger");
                },
                error: () => { setLoading("#btnGuardarParametros", false); showAlert("alertContainer", "Error de conexión.", "danger"); }
            });
        });
    });
});
