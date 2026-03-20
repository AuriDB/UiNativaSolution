function AdminDashboardView() {

    this.InitView = () => {
        this.LoadStats();
        this.LoadEstados();
        this.LoadProvincia();
        this.LoadIngenieros();
    };

    this.LoadStats = () => {
        $.ajax({
            url: `${API_URL_BASE}/Admin/Dashboard`,
            method: "GET",
            success: (res) => {
                if (res.success && res.data) {
                    const d = res.data;
                    $("#statFincas").text(d.fincasActivas ?? "0");
                    $("#statPagos").text(d.pagosMes ?? "0");
                    $("#statUsuarios").text(d.usuariosActivos ?? "0");
                    $("#statCola").text(d.colaFifo ?? "0");
                }
            },
            error: () => {
                $("#statFincas, #statPagos, #statUsuarios, #statCola").text("0");
            }
        });
    };

    this.LoadEstados = () => {
        $.ajax({
            url: `${API_URL_BASE}/Admin/FincasPorEstado`,
            method: "GET",
            success: (res) => {
                if (res.success && res.data && res.data.length > 0) {
                    const colores = {
                        "Pendiente": "secondary", "En Revisión": "info",
                        "Aprobada": "success", "Devuelta": "warning",
                        "Rechazada": "danger", "Vencida": "dark"
                    };
                    const items = res.data.map(e => `
                        <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
                            <span class="badge text-bg-${colores[e.estado] ?? 'secondary'} rounded-pill px-2">${e.estado}</span>
                            <span class="fw-bold">${e.cantidad}</span>
                        </div>`).join("");
                    $("#listaEstados").html(items);
                } else {
                    $("#listaEstados").html(`<p class="text-muted text-center py-3 small">Sin datos disponibles.</p>`);
                }
            },
            error: () => {
                $("#listaEstados").html(`<p class="text-muted text-center py-3 small"><i class="bi bi-wifi-off me-1"></i>No se pudo cargar.</p>`);
            }
        });
    };

    this.LoadProvincia = () => {
        $.ajax({
            url: `${API_URL_BASE}/Admin/PagosPorProvincia`,
            method: "GET",
            success: (res) => {
                if (res.success && res.data && res.data.length > 0) {
                    const items = res.data.map(p => `
                        <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
                            <span class="text-muted small">${p.provincia}</span>
                            <span class="fw-bold small">₡${Number(p.monto).toLocaleString("es-CR")}</span>
                        </div>`).join("");
                    $("#listaProvincia").html(items);
                } else {
                    $("#listaProvincia").html(`<p class="text-muted text-center py-3 small">Sin datos disponibles.</p>`);
                }
            },
            error: () => {
                $("#listaProvincia").html(`<p class="text-muted text-center py-3 small"><i class="bi bi-wifi-off me-1"></i>No se pudo cargar.</p>`);
            }
        });
    };

    this.LoadIngenieros = () => {
        $.ajax({
            url: `${API_URL_BASE}/Admin/ProductividadIngenieros`,
            method: "GET",
            success: (res) => {
                if (res.success && res.data && res.data.length > 0) {
                    const rows = res.data.map(i => `
                        <tr class="align-middle">
                            <td class="fw-semibold">${i.nombre}</td>
                            <td class="text-center">${i.evaluacionesMes}</td>
                            <td class="text-center">${i.evaluacionesTotal}</td>
                            <td class="text-end text-muted small">${i.ultimaEvaluacion ?? "—"}</td>
                        </tr>`).join("");
                    $("#tablaIngenieros").html(`
                        <div class="table-responsive">
                            <table class="table table-hover mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th class="small fw-semibold text-muted">Ingeniero</th>
                                        <th class="small fw-semibold text-muted text-center">Este mes</th>
                                        <th class="small fw-semibold text-muted text-center">Total</th>
                                        <th class="small fw-semibold text-muted text-end">Última eval.</th>
                                    </tr>
                                </thead>
                                <tbody>${rows}</tbody>
                            </table>
                        </div>`);
                } else {
                    $("#tablaIngenieros").html(`<p class="text-muted text-center py-3 small">Sin ingenieros registrados.</p>`);
                }
            },
            error: () => {
                $("#tablaIngenieros").html(`
                    <div class="text-center py-3 text-muted small">
                        <i class="bi bi-wifi-off me-1"></i>No se pudo cargar la información.
                    </div>`);
            }
        });
    };
}

$(document).ready(() => {
    const view = new AdminDashboardView();
    view.InitView();
});
