// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Dueno/historial.js
//  Historial de planes de pago PSA del Dueño.
// ═══════════════════════════════════════════════════════

$(document).ready(() => {
    $.getJSON(`${API_URL_BASE}/Dueno/HistorialPagosData`, planes => {
        if (!planes || planes.length === 0) {
            $("#contenedorPlanes").html(
                `<div class="alert alert-info rounded-3">
                    <i class="bi bi-info-circle me-2"></i>
                    Aún no tienes planes de pago activos. Una vez que el ingeniero apruebe tu finca
                    y el administrador active el plan, aparecerá aquí.
                 </div>`
            );
            return;
        }

        const html = planes.map(p => renderPlan(p)).join("");
        $("#contenedorPlanes").html(html);
    }).fail(() => {
        showAlert("alertContainer", "Error al cargar el historial de pagos.", "danger");
    });
});

function renderPlan(plan) {
    const pagados  = plan.pagos.filter(p => p.estado === "Ejecutado").length;
    const pctAvance = Math.round((pagados / plan.pagos.length) * 100);

    const filasPagos = plan.pagos.map(p =>
        `<tr>
            <td class="text-center">${p.numeroPago}</td>
            <td>${p.fechaPago}</td>
            <td class="fw-semibold">₡${p.monto.toLocaleString("es-CR", {minimumFractionDigits: 2})}</td>
            <td>
                <span class="badge bg-${p.estadoColor} px-2">${p.estado}</span>
            </td>
            <td class="text-muted small">${p.fechaEjecucion ?? "—"}</td>
        </tr>`
    ).join("");

    return `
    <div class="card border-0 shadow-sm rounded-4 mb-4">
        <div class="card-header bg-transparent border-0 pt-4 px-4 d-flex justify-content-between align-items-center">
            <div>
                <h6 class="fw-bold mb-0">
                    <i class="bi bi-file-earmark-check text-success me-2"></i>
                    Plan — Finca #${plan.activoId}
                </h6>
                <p class="text-muted small mb-0">
                    Activado el ${plan.fechaActivacion} &nbsp;·&nbsp;
                    Monto mensual: <strong>₡${plan.montoMensual.toLocaleString("es-CR", {minimumFractionDigits: 2})}</strong>
                </p>
            </div>
            <a href="/Dueno/Detalle/${plan.activoId}" class="btn btn-outline-primary btn-sm rounded-3">
                <i class="bi bi-eye me-1"></i>Ver finca
            </a>
        </div>
        <div class="card-body px-4 pb-4">

            <!-- Barra de progreso -->
            <div class="mb-3">
                <div class="d-flex justify-content-between small text-muted mb-1">
                    <span>Progreso</span>
                    <span>${pagados} / ${plan.pagos.length} pagos ejecutados</span>
                </div>
                <div class="progress rounded-pill" style="height:8px;">
                    <div class="progress-bar bg-success" style="width:${pctAvance}%;"></div>
                </div>
            </div>

            <!-- Tabla de cuotas -->
            <div class="table-responsive">
                <table class="table table-sm table-hover align-middle mb-0">
                    <thead class="table-light">
                        <tr>
                            <th class="text-center">#</th>
                            <th>Fecha de pago</th>
                            <th>Monto</th>
                            <th>Estado</th>
                            <th>Ejecutado el</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${filasPagos}
                    </tbody>
                </table>
            </div>
        </div>
    </div>`;
}
