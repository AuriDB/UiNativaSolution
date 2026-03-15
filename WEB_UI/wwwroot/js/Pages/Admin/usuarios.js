// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Admin/usuarios.js
//  Gestión de usuarios: crear Ingeniero, editar, inactivar/reactivar.
// ═══════════════════════════════════════════════════════

let gridApi = null;

function estadoBadge(params) {
    const colorMap = { success: "bg-success", secondary: "bg-secondary", danger: "bg-danger" };
    const cls = colorMap[params.data.estadoColor] || "bg-light";
    return `<span class="badge ${cls} text-white px-2">${params.data.estado}</span>`;
}

function rolBadge(params) {
    const colorMap = { Admin: "bg-primary", Ingeniero: "bg-info", Dueno: "bg-warning text-dark" };
    const cls = colorMap[params.value] || "bg-light";
    return `<span class="badge ${cls} px-2">${params.value}</span>`;
}

function accionesBtn(params) {
    const d = params.data;
    if (d.esSelf) return `<span class="text-muted small">(tú)</span>`;

    let html = `<button class="btn btn-sm btn-outline-warning rounded-2 me-1"
                    onclick="abrirEditar(${d.id}, '${d.nombre.replace(/'/g,"\\'")}', '${d.correo}')">
                    <i class="bi bi-pencil"></i>
                </button>`;

    if (d.estado === "Activo" || d.estado === "Bloqueado") {
        html += `<button class="btn btn-sm btn-outline-danger rounded-2"
                    onclick="confirmarInactivar(${d.id}, '${d.nombre.replace(/'/g,"\\'")}')">
                    <i class="bi bi-person-dash"></i>
                 </button>`;
    } else {
        html += `<button class="btn btn-sm btn-outline-success rounded-2"
                    onclick="confirmarReactivar(${d.id}, '${d.nombre.replace(/'/g,"\\'")}')">
                    <i class="bi bi-person-check"></i>
                 </button>`;
    }

    return html;
}

function abrirEditar(id, nombre, correo) {
    $("#editId").val(id);
    $("#editNombre").val(nombre);
    $("#editCorreo").val(correo);
    clearAlert("alertModalEditar");
    new bootstrap.Modal(document.getElementById("modalEditar")).show();
}

function confirmarInactivar(id, nombre) {
    Swal.fire({
        title:             `¿Inactivar a ${nombre}?`,
        text:              "No podrá iniciar sesión hasta que sea reactivado.",
        icon:              "warning",
        showCancelButton:  true,
        confirmButtonText: "Inactivar",
        cancelButtonText:  "Cancelar",
        confirmButtonColor: "#dc3545"
    }).then(r => {
        if (!r.isConfirmed) return;
        cambiarEstado(`${API_URL_BASE}/Admin/InactivarUsuario`, id);
    });
}

function confirmarReactivar(id, nombre) {
    Swal.fire({
        title:             `¿Reactivar a ${nombre}?`,
        icon:              "question",
        showCancelButton:  true,
        confirmButtonText: "Reactivar",
        confirmButtonColor: "#78c2ad"
    }).then(r => {
        if (!r.isConfirmed) return;
        cambiarEstado(`${API_URL_BASE}/Admin/ReactivarUsuario`, id);
    });
}

function cambiarEstado(url, id) {
    $.ajax({
        url, method: "POST", contentType: "application/json",
        data: JSON.stringify({ id }),
        success: res => {
            if (res.success) { showAlert("alertContainer", res.message, "success"); cargarUsuarios(); }
            else              showAlert("alertContainer", res.message, "danger");
        },
        error: () => showAlert("alertContainer", "Error de conexión.", "danger")
    });
}

function cargarUsuarios() {
    $.getJSON(`${API_URL_BASE}/Admin/UsuariosData`, data => {
        gridApi.setGridOption("rowData", data);
    }).fail(() => showAlert("alertContainer", "Error al cargar usuarios.", "danger"));
}

$(document).ready(() => {

    const colDefs = [
        { field: "cedula",       headerName: "Cédula",    width: 130 },
        { field: "nombre",       headerName: "Nombre",    flex: 1,   sortable: true },
        { field: "correo",       headerName: "Correo",    flex: 1 },
        { field: "rol",          headerName: "Rol",       width: 110, cellRenderer: rolBadge },
        { field: "estado",       headerName: "Estado",    width: 120, cellRenderer: estadoBadge },
        { field: "fechaCreacion", headerName: "Creado",   width: 110, sortable: true },
        { headerName: "Acciones", width: 140,
          cellRenderer: accionesBtn, sortable: false, filter: false }
    ];

    const gridOptions = {
        columnDefs: colDefs, rowData: [],
        pagination: true, paginationPageSize: 15,
        defaultColDef: { filter: true, resizable: true },
        localeText: { noRowsToShow: "No hay usuarios registrados." }
    };

    gridApi = agGrid.createGrid(document.getElementById("gridUsuarios"), gridOptions);
    cargarUsuarios();

    // ── Crear Ingeniero ──────────────────────────────────
    $("#btnCrearIngeniero").on("click", () => {
        clearAlert("alertModalCrear");
        const payload = {
            nombre:     $("#crNombre").val().trim(),
            cedula:     $("#crCedula").val().trim(),
            correo:     $("#crCorreo").val().trim(),
            contrasena: $("#crContrasena").val()
        };
        if (!payload.nombre || !payload.cedula || !payload.correo || !payload.contrasena) {
            showAlert("alertModalCrear", "Todos los campos son obligatorios.", "warning");
            return;
        }
        setLoading("#btnCrearIngeniero", true);
        $.ajax({
            url: `${API_URL_BASE}/Admin/CrearIngeniero`, method: "POST",
            contentType: "application/json", data: JSON.stringify(payload),
            success: res => {
                setLoading("#btnCrearIngeniero", false);
                if (res.success) {
                    bootstrap.Modal.getInstance(document.getElementById("modalCrear")).hide();
                    $("#crNombre, #crCedula, #crCorreo, #crContrasena").val("");
                    showAlert("alertContainer", res.message, "success");
                    cargarUsuarios();
                } else showAlert("alertModalCrear", res.message, "danger");
            },
            error: () => { setLoading("#btnCrearIngeniero", false); showAlert("alertModalCrear", "Error de conexión.", "danger"); }
        });
    });

    // ── Editar usuario ───────────────────────────────────
    $("#btnGuardarEditar").on("click", () => {
        clearAlert("alertModalEditar");
        const payload = {
            id:     parseInt($("#editId").val()),
            nombre: $("#editNombre").val().trim(),
            correo: $("#editCorreo").val().trim()
        };
        if (!payload.nombre || !payload.correo) {
            showAlert("alertModalEditar", "Nombre y correo son obligatorios.", "warning");
            return;
        }
        setLoading("#btnGuardarEditar", true);
        $.ajax({
            url: `${API_URL_BASE}/Admin/EditarUsuario`, method: "POST",
            contentType: "application/json", data: JSON.stringify(payload),
            success: res => {
                setLoading("#btnGuardarEditar", false);
                if (res.success) {
                    bootstrap.Modal.getInstance(document.getElementById("modalEditar")).hide();
                    showAlert("alertContainer", res.message, "success");
                    cargarUsuarios();
                } else showAlert("alertModalEditar", res.message, "danger");
            },
            error: () => { setLoading("#btnGuardarEditar", false); showAlert("alertModalEditar", "Error de conexión.", "danger"); }
        });
    });
});
