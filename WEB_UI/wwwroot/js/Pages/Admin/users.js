const estadoBadge = {
    Activo:    '<span class="badge bg-success">Activo</span>',
    Inactivo:  '<span class="badge bg-secondary">Inactivo</span>',
    Bloqueado: '<span class="badge bg-danger">Bloqueado</span>',
};

const colDefs = [
    { field: "id",      headerName: "ID",     width: 70 },
    { field: "cedula",  headerName: "Cédula",  width: 140 },
    { field: "nombre",  headerName: "Nombre",  flex: 1, minWidth: 200 },
    { field: "correo",  headerName: "Correo",  flex: 1, minWidth: 200 },
    { field: "rol",     headerName: "Rol",     width: 110 },
    { field: "estado",  headerName: "Estado",  width: 120, cellRenderer: p => estadoBadge[p.value] || p.value },
    {
        headerName: "Acciones", width: 200, sortable: false, filter: false,
        cellRenderer: p => {
            const id     = p.data.id;
            const nombre = p.data.nombre;
            const estado = p.data.estado;
            let btns = `<button class="btn btn-sm btn-outline-primary rounded-3 py-0 me-1 btn-editar" data-id="${id}" data-nombre="${nombre}">
                <i class="bi bi-pencil"></i>
            </button>`;
            if (estado !== "Inactivo") {
                btns += `<button class="btn btn-sm btn-outline-danger rounded-3 py-0 me-1 btn-inactivar" data-id="${id}" data-nombre="${nombre}">
                    <i class="bi bi-person-x"></i>
                </button>`;
            } else {
                btns += `<button class="btn btn-sm btn-outline-success rounded-3 py-0 btn-reactivar" data-id="${id}" data-nombre="${nombre}">
                    <i class="bi bi-person-check"></i>
                </button>`;
            }
            return btns;
        }
    }
];

const gridOptions = {
    columnDefs: colDefs,
    rowData: [],
    defaultColDef: { resizable: true, filter: true },
    pagination: true,
    paginationPageSize: 20,
    onCellClicked: (e) => {
        const el = e.event.target.closest("button");
        if (!el) return;
        const id     = parseInt(el.dataset.id);
        const nombre = el.dataset.nombre;

        if (el.classList.contains("btn-editar"))    abrirEditar(id, nombre);
        if (el.classList.contains("btn-inactivar")) confirmarInactivar(id, nombre);
        if (el.classList.contains("btn-reactivar")) confirmarReactivar(id, nombre);
    }
};

function cargarUsuarios() {
    $.get(`${API_URL_BASE}/Admin/UsersData`, data => gridOptions.api?.setRowData(data))
        .fail(() => showAlert("alertUsuarios", "Error al cargar usuarios.", "danger"));
}

function abrirEditar(id, nombreCompleto) {
    const partes = nombreCompleto.split(" ");
    $("#editId").val(id);
    $("#editNombre").val(partes[0] || "");
    $("#editApellido1").val(partes[1] || "");
    $("#editApellido2").val(partes.slice(2).join(" ") || "");
    new bootstrap.Modal("#modalEditar").show();
}

function confirmarInactivar(id, nombre) {
    Swal.fire({
        title: `¿Inactivar a ${nombre}?`,
        text:  "Si es Ingeniero, sus fincas en revisión volverán a la cola FIFO.",
        icon:  "warning",
        showCancelButton: true,
        confirmButtonText: "Sí, inactivar",
        confirmButtonColor: "#dc3545"
    }).then(r => {
        if (!r.isConfirmed) return;
        $.post(`${API_URL_BASE}/Admin/DeactivateUser/${id}`)
            .done(res => {
                if (res.success) { Swal.fire({ icon: "success", text: res.message }); cargarUsuarios(); }
                else showAlert("alertUsuarios", res.message, "danger");
            });
    });
}

function confirmarReactivar(id, nombre) {
    Swal.fire({
        title: `¿Reactivar a ${nombre}?`,
        icon:  "question",
        showCancelButton: true,
        confirmButtonText: "Sí, reactivar"
    }).then(r => {
        if (!r.isConfirmed) return;
        $.post(`${API_URL_BASE}/Admin/ReactivateUser/${id}`)
            .done(res => {
                if (res.success) { Swal.fire({ icon: "success", text: res.message }); cargarUsuarios(); }
                else showAlert("alertUsuarios", res.message, "danger");
            });
    });
}

$(document).ready(() => {
    const el = document.getElementById("gridUsuarios");
    agGrid.createGrid(el, gridOptions);
    cargarUsuarios();

    $("#btnNuevoIngeniero").on("click", () => new bootstrap.Modal("#modalNuevoIngeniero").show());
    $("#btnNuevoAdmin").on("click", () => new bootstrap.Modal("#modalNuevoAdmin").show());

    $("#btnGuardarIngeniero").on("click", () => {
        clearAlert("alertModalIngeniero");
        const payload = {
            nombre:     $("#ingNombre").val().trim(),
            apellido1:  $("#ingApellido1").val().trim(),
            apellido2:  $("#ingApellido2").val().trim(),
            cedula:     $("#ingCedula").val().trim(),
            correo:     $("#ingCorreo").val().trim(),
            contrasena: $("#ingContrasena").val()
        };
        if (!payload.nombre || !payload.apellido1 || !payload.cedula || !payload.correo || !payload.contrasena) {
            showAlert("alertModalIngeniero", "Completa todos los campos.", "warning");
            return;
        }
        setLoading("#btnGuardarIngeniero", true);
        $.ajax({
            url: `${API_URL_BASE}/Admin/CreateEngineer`, method: "POST",
            contentType: "application/json", data: JSON.stringify(payload),
            success: (res) => {
                setLoading("#btnGuardarIngeniero", false);
                if (res.success) {
                    bootstrap.Modal.getInstance("#modalNuevoIngeniero").hide();
                    Swal.fire({ icon: "success", text: res.message });
                    cargarUsuarios();
                } else { showAlert("alertModalIngeniero", res.message, "danger"); }
            },
            error: () => { setLoading("#btnGuardarIngeniero", false); showAlert("alertModalIngeniero", "Error de conexión.", "danger"); }
        });
    });

    $("#btnGuardarAdmin").on("click", () => {
        clearAlert("alertModalAdmin");
        const payload = {
            nombre:     $("#adNombre").val().trim(),
            apellido1:  $("#adApellido1").val().trim(),
            apellido2:  $("#adApellido2").val().trim(),
            cedula:     $("#adCedula").val().trim(),
            correo:     $("#adCorreo").val().trim(),
            contrasena: $("#adContrasena").val()
        };
        if (!payload.nombre || !payload.apellido1 || !payload.cedula || !payload.correo || !payload.contrasena) {
            showAlert("alertModalAdmin", "Completa todos los campos.", "warning");
            return;
        }
        setLoading("#btnGuardarAdmin", true);
        $.ajax({
            url: `${API_URL_BASE}/Admin/CreateAdmin`, method: "POST",
            contentType: "application/json", data: JSON.stringify(payload),
            success: (res) => {
                setLoading("#btnGuardarAdmin", false);
                if (res.success) {
                    bootstrap.Modal.getInstance("#modalNuevoAdmin").hide();
                    Swal.fire({ icon: "success", text: res.message });
                    cargarUsuarios();
                } else { showAlert("alertModalAdmin", res.message, "danger"); }
            },
            error: () => { setLoading("#btnGuardarAdmin", false); showAlert("alertModalAdmin", "Error de conexión.", "danger"); }
        });
    });

    $("#btnGuardarEditar").on("click", () => {
        clearAlert("alertModalEditar");
        const id = $("#editId").val();
        const payload = {
            nombre:    $("#editNombre").val().trim(),
            apellido1: $("#editApellido1").val().trim(),
            apellido2: $("#editApellido2").val().trim()
        };
        setLoading("#btnGuardarEditar", true);
        $.ajax({
            url: `${API_URL_BASE}/Admin/EditUser/${id}`, method: "POST",
            contentType: "application/json", data: JSON.stringify(payload),
            success: (res) => {
                setLoading("#btnGuardarEditar", false);
                if (res.success) {
                    bootstrap.Modal.getInstance("#modalEditar").hide();
                    Swal.fire({ icon: "success", text: res.message });
                    cargarUsuarios();
                } else { showAlert("alertModalEditar", res.message, "danger"); }
            },
            error: () => { setLoading("#btnGuardarEditar", false); showAlert("alertModalEditar", "Error.", "danger"); }
        });
    });
});
