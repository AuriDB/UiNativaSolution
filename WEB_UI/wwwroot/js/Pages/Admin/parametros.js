// ── Admin/Parametros ─────────────────────────────────────────────────────────

const colDefs = [
    { field: "id",            headerName: "ID",         width: 70 },
    { field: "precioBase",    headerName: "Precio Base", width: 130, valueFormatter: p => `₡${(p.value||0).toLocaleString("es-CR",{minimumFractionDigits:2})}` },
    { field: "pctVegetacion", headerName: "% Veg",       width: 95,  valueFormatter: p => `${(p.value*100).toFixed(2)}%` },
    { field: "pctHidrologia", headerName: "% Hid",       width: 95,  valueFormatter: p => `${(p.value*100).toFixed(2)}%` },
    { field: "pctNacional",   headerName: "% Nac",       width: 95,  valueFormatter: p => `${(p.value*100).toFixed(2)}%` },
    { field: "pctTopografia", headerName: "% Top",       width: 95,  valueFormatter: p => `${(p.value*100).toFixed(2)}%` },
    { field: "tope",          headerName: "Tope",        width: 90,  valueFormatter: p => `${(p.value*100).toFixed(2)}%` },
    { field: "vigente",       headerName: "Vigente", width: 90,
        cellRenderer: p => p.value ? '<span class="badge bg-success">Sí</span>' : '<span class="badge bg-light text-dark border">No</span>' },
    { field: "fechaCreacion", headerName: "Creado", flex: 1 },
];

const gridOptions = {
    columnDefs: colDefs,
    rowData: [],
    defaultColDef: { resizable: true },
};

$(document).ready(() => {
    const el = document.getElementById("gridParametros");
    agGrid.createGrid(el, gridOptions);

    $.get(`${API_URL_BASE}/Admin/Parametros/Data`, data => gridOptions.api?.setRowData(data));

    $("#frmParametros").on("submit", (e) => {
        e.preventDefault();
        clearAlert("alertParametros");

        const opcion = $("input[name='opcion']:checked").val();
        // Los inputs son 0-100; la BD espera 0-1, se divide ÷100
        const pctToDecimal = id => (parseFloat($(id).val()) || 0) / 100;
        const payload = {
            precioBase:    parseFloat($("#precioBase").val()) || 0,
            pctVegetacion: pctToDecimal("#pctVeg"),
            pctHidrologia: pctToDecimal("#pctHid"),
            pctNacional:   pctToDecimal("#pctNac"),
            pctTopografia: pctToDecimal("#pctTop"),
            tope:          pctToDecimal("#tope"),
            opcion
        };

        if (!payload.precioBase || !payload.tope) {
            showAlert("alertParametros", "Precio base y tope son requeridos.", "warning");
            return;
        }

        const confirmMsg = opcion === "B"
            ? "Opción B recalculará todos los pagos Pendientes activos. ¿Continuar?"
            : "Los nuevos parámetros aplicarán solo a fincas nuevas. ¿Continuar?";

        Swal.fire({ title: "Confirmar", text: confirmMsg, icon: "question",
            showCancelButton: true, confirmButtonText: "Sí, guardar" })
        .then(r => {
            if (!r.isConfirmed) return;
            setLoading("#btnGuardarParametros", true);
            $.ajax({
                url: `${API_URL_BASE}/Admin/Parametros`, method: "POST",
                contentType: "application/json", data: JSON.stringify(payload),
                success: (res) => {
                    setLoading("#btnGuardarParametros", false);
                    if (res.success) {
                        Swal.fire({ icon: "success", text: res.message }).then(() => location.reload());
                    } else { showAlert("alertParametros", res.message, "danger"); }
                },
                error: () => { setLoading("#btnGuardarParametros", false); showAlert("alertParametros", "Error de conexión.", "danger"); }
            });
        });
    });
});
