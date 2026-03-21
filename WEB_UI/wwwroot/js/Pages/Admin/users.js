function UsersView() {

    const ROLES = { 1: "Administrador", 2: "Ingeniero", 3: "Due\u00f1o" };

    this.InitView = () => {
        this.LoadUsuarios();
        this.BindEvents();
    };

    this.BindEvents = () => {
        $("#filtroRol").on("change", () => this.LoadUsuarios());
        $("#btnRefresh").on("click", () => this.LoadUsuarios());
    };

    this.LoadUsuarios = () => {
        const idRol = $("#filtroRol").val();
        const url   = idRol
            ? `${API_URL_BASE}/Usuario/GetByRol/${idRol}`
            : `${API_URL_BASE}/Usuario/GetAll`;

        $("#tbodyUsuarios").html(`
            <tr>
                <td colspan="7" class="text-center py-4 text-muted">
                    <span class="spinner-border spinner-border-sm me-2"></span>Cargando...
                </td>
            </tr>`);

        $.ajax({
            url:    url,
            method: "GET",
            success: (res) => {
                if (res.result === "ok") {
                    this.RenderTable(res.data);
                } else {
                    this.ShowError(res.message);
                }
            },
            error: () => this.ShowError("Error de conexion al cargar usuarios.")
        });
    };

    this.RenderTable = (usuarios) => {
        if (!usuarios || usuarios.length === 0) {
            $("#tbodyUsuarios").html(`
                <tr>
                    <td colspan="7" class="text-center py-4 text-muted">
                        <i class="bi bi-inbox fs-3 d-block mb-2"></i>No hay usuarios que mostrar.
                    </td>
                </tr>`);
            $("#totalUsuarios").text("0");
            return;
        }

        const rows = usuarios.map((u, i) => {
            const rolBadge  = this.RolBadge(u.idRol);
            const actBadge  = u.activo
                ? '<span class="badge bg-success rounded-pill">Activo</span>'
                : '<span class="badge bg-secondary rounded-pill">Inactivo</span>';
            const fecha = u.fechaRegistro
                ? new Date(u.fechaRegistro).toLocaleDateString("es-CR")
                : "—";
            return `
                <tr>
                    <td class="text-muted small">${i + 1}</td>
                    <td><strong>${u.nombre || "—"}</strong></td>
                    <td class="small">${u.email || "—"}</td>
                    <td class="small">${u.cedula || "—"}</td>
                    <td>${rolBadge}</td>
                    <td>${actBadge}</td>
                    <td class="small text-muted">${fecha}</td>
                </tr>`;
        }).join("");

        $("#tbodyUsuarios").html(rows);
        $("#totalUsuarios").text(usuarios.length);
    };

    this.RolBadge = (idRol) => {
        const map = {
            1: '<span class="badge bg-danger rounded-pill">Administrador</span>',
            2: '<span class="badge bg-primary rounded-pill">Ingeniero</span>',
            3: '<span class="badge bg-success rounded-pill">Due\u00f1o</span>'
        };
        return map[idRol] || `<span class="badge bg-secondary rounded-pill">Rol ${idRol}</span>`;
    };

    this.ShowError = (msg) => {
        $("#tbodyUsuarios").html(`
            <tr>
                <td colspan="7" class="text-center py-4 text-danger">
                    <i class="bi bi-exclamation-triangle fs-3 d-block mb-2"></i>${msg}
                </td>
            </tr>`);
    };
}

$(document).ready(() => {
    const view = new UsersView();
    view.InitView();
});
