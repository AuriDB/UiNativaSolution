// ── Dashboard — branching por rol ────────────────────────────────────────────
// DASHBOARD_ROLE se inyecta desde Index.cshtml @section Scripts

function HomeView() {

    this.InitView = () => {
        const role = (typeof DASHBOARD_ROLE !== "undefined") ? DASHBOARD_ROLE : "";
        if      (role === "Dueno")     { this.LoadDuenoDashboard(); this.LoadFincasRecientes(); }
        else if (role === "Ingeniero") { this.LoadIngDashboard(); }
        else if (role === "Admin")     { this.LoadAdminDashboard(); }
    };

    // ── Dueño ─────────────────────────────────────────────────────────────────
    this.LoadDuenoDashboard = () => {
        $.ajax({
            url:    `${API_URL_BASE}/Dueno/Dashboard`,
            method: "GET",
            success: (d) => {
                $("#statFincas").text(d.totalFincas ?? "0");
                $("#statActivas").text(d.fincasActivas ?? "0");
                $("#statEnRevision").text(d.fincasEnRevision ?? "0");
                if (d.proximoPago) {
                    $("#statProximoPago").text(`₡${Number(d.proximoPago.monto).toLocaleString("es-CR")}`);
                    $("#statFechaPago").text(d.proximoPago.fecha ?? "—");
                } else {
                    $("#statProximoPago").text("Sin pagos");
                    $("#statFechaPago").text("—");
                }
            },
            error: () => {
                $("#statFincas, #statActivas, #statEnRevision").text("0");
                $("#statProximoPago").text("—");
            }
        });
    };

    this.LoadFincasRecientes = () => {
        $.ajax({
            url:    `${API_URL_BASE}/Dueno/Fincas/Recientes`,
            method: "GET",
            success: (data) => {
                if (data && data.length > 0) {
                    this.RenderTablaFincas(data);
                } else {
                    $("#tableFincas").html(`
                        <div class="text-center py-4 text-muted">
                            <i class="bi bi-map fs-3 mb-2 d-block text-primary opacity-50"></i>
                            <p class="mb-2">No tienes fincas registradas aún.</p>
                            <a href="/Dueno/Fincas/Nueva" class="btn btn-sm btn-primary rounded-3">
                                <i class="bi bi-plus me-1"></i>Registrar mi primera finca
                            </a>
                        </div>`);
                }
            },
            error: () => {
                $("#tableFincas").html(`
                    <div class="text-center py-3 text-muted small">
                        <i class="bi bi-wifi-off me-1"></i>No se pudo cargar la información.
                    </div>`);
            }
        });
    };

    this.RenderTablaFincas = (fincas) => {
        const estadoBadge = {
            "Pendiente":   "secondary",
            "EnRevision":  "info",
            "Aprobada":    "success",
            "Devuelta":    "warning",
            "Rechazada":   "danger",
            "Vencida":     "dark"
        };

        const rows = fincas.map(f => `
            <tr class="align-middle" style="cursor:pointer"
                onclick="window.location='/Dueno/Fincas/${f.id}'">
                <td><span class="fw-semibold">${f.nombre}</span></td>
                <td class="text-muted small">${f.coordenadas}</td>
                <td>${f.hectareas} ha</td>
                <td>
                    <span class="badge text-bg-${estadoBadge[f.estado] ?? "secondary"} rounded-pill px-2">
                        ${f.estado}
                    </span>
                </td>
            </tr>`).join("");

        $("#tableFincas").html(`
            <div class="table-responsive">
                <table class="table table-hover mb-0">
                    <thead class="table-light">
                        <tr>
                            <th class="small fw-semibold text-muted">Nombre</th>
                            <th class="small fw-semibold text-muted">Ubicación</th>
                            <th class="small fw-semibold text-muted">Área</th>
                            <th class="small fw-semibold text-muted">Estado</th>
                        </tr>
                    </thead>
                    <tbody>${rows}</tbody>
                </table>
            </div>`);
    };

    // ── Ingeniero ─────────────────────────────────────────────────────────────
    this.LoadIngDashboard = () => {
        $.ajax({
            url:    `${API_URL_BASE}/Ingeniero/Dashboard`,
            method: "GET",
            success: (d) => {
                $("#statCola").text(d.enCola ?? "0");
                $("#statEnRevisionIng").text(d.enRevision ?? "0");
                $("#statPlanes").text(d.planes ?? "0");
            },
            error: () => {
                $("#statCola, #statEnRevisionIng, #statPlanes").text("0");
            }
        });
    };

    // ── Admin ─────────────────────────────────────────────────────────────────
    this.LoadAdminDashboard = () => {
        $.ajax({
            url:    `${API_URL_BASE}/Admin/Dashboard`,
            method: "GET",
            success: (d) => {
                $("#statUsuarios").text(d.totalUsuarios ?? "0");
                $("#statIngenieros").text(d.ingenieros ?? "0");
                $("#statFincasAdmin").text(d.fincasEnCola ?? "0");
                $("#statParams").text(d.precioBase != null
                    ? `₡${Number(d.precioBase).toLocaleString("es-CR")}/ha`
                    : "No configurado");
            },
            error: () => {
                $("#statUsuarios, #statIngenieros, #statFincasAdmin").text("0");
                $("#statParams").text("—");
            }
        });
    };
}

$(document).ready(() => {
    const view = new HomeView();
    view.InitView();
});
