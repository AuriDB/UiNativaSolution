function EngineerDashboardView() {

    this.InitView = () => {
        this.LoadStats();
        this.LoadFincasAsignadas();
    };

    this.LoadStats = () => {
        $.ajax({
            url: `${API_URL_BASE}/Ingeniero/Dashboard`,
            method: "GET",
            success: (res) => {
                if (res.success && res.data) {
                    const d = res.data;
                    $("#statCola").text(d.colaDisponible ?? "0");
                    $("#statAsignadas").text(d.fincasAsignadas ?? "0");
                    $("#statEvaluaciones").text(d.evaluacionesTotal ?? "0");
                    $("#statHectareas").text(d.hectareasAuditadas ?? "0");
                }
            },
            error: () => {
                $("#statCola, #statAsignadas, #statEvaluaciones, #statHectareas").text("0");
            }
        });
    };

    this.LoadFincasAsignadas = () => {
        $.ajax({
            url: `${API_URL_BASE}/Ingeniero/FincasAsignadas`,
            method: "GET",
            success: (res) => {
                if (res.success && res.data && res.data.length > 0) {
                    const rows = res.data.map(f => `
                        <tr class="align-middle" style="cursor:pointer"
                            onclick="window.location='/Engineer/Evaluation?id=${f.id}'">
                            <td class="fw-semibold">${f.cedula ?? "—"}</td>
                            <td class="text-muted small">${f.hectareas} ha</td>
                            <td class="text-muted small">${f.fechaAsignacion ?? "—"}</td>
                            <td>
                                <span class="badge text-bg-info rounded-pill px-2">En Revisión</span>
                            </td>
                        </tr>`).join("");
                    $("#tablaAsignadas").html(`
                        <div class="table-responsive">
                            <table class="table table-hover mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th class="small fw-semibold text-muted">Cédula finca</th>
                                        <th class="small fw-semibold text-muted">Área</th>
                                        <th class="small fw-semibold text-muted">Asignada</th>
                                        <th class="small fw-semibold text-muted">Estado</th>
                                    </tr>
                                </thead>
                                <tbody>${rows}</tbody>
                            </table>
                        </div>`);
                } else {
                    $("#tablaAsignadas").html(`
                        <div class="text-center py-4 text-muted">
                            <i class="bi bi-clipboard-check fs-3 mb-2 d-block text-primary opacity-50"></i>
                            <p class="mb-2">No tienes fincas asignadas actualmente.</p>
                            <a href="/Engineer/FifoQueue" class="btn btn-sm btn-primary rounded-3">
                                <i class="bi bi-list-task me-1"></i>Ver cola FIFO
                            </a>
                        </div>`);
                }
            },
            error: () => {
                $("#tablaAsignadas").html(`
                    <div class="text-center py-3 text-muted small">
                        <i class="bi bi-wifi-off me-1"></i>No se pudo cargar la información.
                    </div>`);
            }
        });
    };
}

$(document).ready(() => {
    const view = new EngineerDashboardView();
    view.InitView();
});
