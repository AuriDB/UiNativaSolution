function HomeView() {

    this.InitView = () => {
        this.LoadStats();
        this.LoadFincasRecientes();
    };

    this.LoadStats = () => {
        $.ajax({
            url:     `${API_URL_BASE}/Dueno/Dashboard`,
            method:  "GET",
            success: (res) => {
                if (res.success && res.data) {
                    const d = res.data;
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
                }
            },
            error: () => {
                // Silencioso — el backend aún no está conectado
                $("#statFincas, #statActivas, #statEnRevision").text("0");
                $("#statProximoPago").text("—");
            }
        });
    };

    this.LoadFincasRecientes = () => {
        $.ajax({
            url:     `${API_URL_BASE}/Fincas/Recientes`,
            method:  "GET",
            success: (res) => {
                if (res.success && res.data && res.data.length > 0) {
                    this.RenderTablaFincas(res.data);
                } else {
                    $("#tableFincas").html(`
                        <div class="text-center py-4 text-muted">
                            <i class="bi bi-map fs-3 mb-2 d-block text-primary opacity-50"></i>
                            <p class="mb-2">No tienes fincas registradas aún.</p>
                            <a href="/Fincas/Create" class="btn btn-sm btn-primary rounded-3">
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
            "Pendiente":    "secondary",
            "En Revisión":  "info",
            "Aprobada":     "success",
            "Plan Activo":  "primary",
            "Devuelta":     "warning",
            "Rechazada":    "danger",
            "Vencida":      "dark",
            "Cancelada":    "secondary"
        };

        let rows = fincas.map(f => `
            <tr class="align-middle" style="cursor:pointer"
                onclick="window.location='/Fincas/${f.id}'">
                <td><span class="fw-semibold">${f.nombre}</span></td>
                <td class="text-muted small">${f.provincia}, ${f.canton}</td>
                <td>${f.hectareas} ha</td>
                <td>
                    <span class="badge text-bg-${estadoBadge[f.estado] ?? 'secondary'} rounded-pill px-2">
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
}

$(document).ready(() => {
    const view = new HomeView();
    view.InitView();
});
