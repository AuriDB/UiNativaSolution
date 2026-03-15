// ── Ingeniero/Fincas/Evaluar — APIs externas + Dictamen ─────────────────────

$(document).ready(() => {
    // ── Cargar adjuntos ──────────────────────────────────────────────────────
    $.get(`${API_URL_BASE}/Ingeniero/Fincas/${FINCA_ID}/Adjuntos`, (data) => {
        if (!data || data.length === 0) {
            $("#listaAdjuntos").html('<p class="text-muted small mb-0">No hay adjuntos.</p>');
            return;
        }
        let html = '<div class="list-group list-group-flush">';
        data.forEach(a => {
            html += `<a href="${a.blobUrl}" target="_blank" class="list-group-item list-group-item-action d-flex align-items-center gap-2 rounded-3 mb-1">
                <i class="bi bi-file-earmark text-primary"></i>
                <span class="flex-fill small">${a.nombreArchivo}</span>
                <span class="text-muted small">${a.fecha}</span>
                <i class="bi bi-box-arrow-up-right text-muted"></i>
            </a>`;
        });
        html += '</div>';
        $("#listaAdjuntos").html(html);
    }).fail(() => {
        $("#listaAdjuntos").html('<p class="text-danger small">Error al cargar adjuntos.</p>');
    });

    // ── APIs externas ────────────────────────────────────────────────────────
    $("#btnCargarApis").on("click", () => {
        $("#panelApis").html('<div class="text-center py-3"><div class="spinner-border spinner-border-sm me-2"></div>Cargando datos ambientales...</div>');
        setLoading("#btnCargarApis", true);

        $.get(`${API_URL_BASE}/Ingeniero/Fincas/Evaluar/${FINCA_ID}/ApiData`, (res) => {
            setLoading("#btnCargarApis", false);
            let html = '<div class="row g-3">';

            if (res.clima) {
                html += `
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded-3 text-center">
                        <div class="text-muted small"><i class="bi bi-thermometer me-1"></i>Temperatura</div>
                        <div class="fw-bold fs-5">${res.clima.temperatura !== null ? res.clima.temperatura + "°C" : "N/D"}</div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded-3 text-center">
                        <div class="text-muted small"><i class="bi bi-speedometer2 me-1"></i>Presión</div>
                        <div class="fw-bold fs-5">${res.clima.presion !== null ? res.clima.presion + " hPa" : "N/D"}</div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded-3 text-center">
                        <div class="text-muted small"><i class="bi bi-cloud me-1"></i>Descripción</div>
                        <div class="fw-bold small">${res.clima.descripcion || "N/D"}</div>
                    </div>
                </div>`;
            }

            if (res.elevacion) {
                html += `
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded-3 text-center">
                        <div class="text-muted small"><i class="bi bi-mountains me-1"></i>Elevación</div>
                        <div class="fw-bold fs-5">${res.elevacion.elevacion !== null ? res.elevacion.elevacion + " m.s.n.m." : "N/D"}</div>
                    </div>
                </div>`;
            }

            html += '</div>';
            if (!res.clima && !res.elevacion) {
                html = '<p class="text-warning small mb-0"><i class="bi bi-exclamation-triangle me-1"></i>No se pudieron obtener datos externos.</p>';
            }
            $("#panelApis").html(html);
        }).fail(() => {
            setLoading("#btnCargarApis", false);
            $("#panelApis").html('<p class="text-danger small mb-0">Error al obtener datos de APIs externas.</p>');
        });
    });

    // ── Dictamen ─────────────────────────────────────────────────────────────
    function enviarDictamen(tipo) {
        const obs = $("#txtObservaciones").val().trim();
        if ((tipo === "Rechazar" || tipo === "Devolver") && !obs) {
            Swal.fire({ icon: "warning", title: "Observaciones requeridas", text: `Debes escribir observaciones para ${tipo.toLowerCase()} una finca.` });
            return;
        }

        const titles = { Aprobar: "¿Aprobar la finca?", Rechazar: "¿Rechazar la finca? (definitivo)", Devolver: "¿Devolver para corrección?" };
        const icons  = { Aprobar: "success", Rechazar: "error", Devolver: "warning" };

        Swal.fire({
            title: titles[tipo],
            text:  tipo === "Rechazar" ? "Esta acción es irreversible." : "",
            icon:  icons[tipo],
            showCancelButton: true,
            confirmButtonText: `Confirmar ${tipo}`,
            cancelButtonText:  "Cancelar"
        }).then(result => {
            if (!result.isConfirmed) return;

            const btn = tipo === "Aprobar" ? "#btnAprobar" : tipo === "Rechazar" ? "#btnRechazar" : "#btnDevolver";
            setLoading(btn, true);

            $.ajax({
                url:         `${API_URL_BASE}/Ingeniero/Fincas/Dictamen/${FINCA_ID}`,
                method:      "POST",
                contentType: "application/json",
                data:        JSON.stringify({ tipo, observaciones: obs }),
                success: (res) => {
                    setLoading(btn, false);
                    if (res.success) {
                        if (tipo === "Aprobar") {
                            // Mostrar botón activar plan
                            Swal.fire({ icon: "success", title: "¡Finca aprobada!", text: res.message });
                            $("#btnAprobar, #btnDevolver, #btnRechazar").prop("disabled", true);
                            $("#btnActivarPlan").removeClass("d-none");
                        } else {
                            Swal.fire({ icon: "success", title: "Dictamen aplicado", text: res.message })
                                .then(() => window.location.href = "/Ingeniero/Cola");
                        }
                    } else {
                        showAlert("alertEvaluar", res.message, "danger");
                    }
                },
                error: () => {
                    setLoading(btn, false);
                    showAlert("alertEvaluar", "Error al aplicar el dictamen.", "danger");
                }
            });
        });
    }

    $("#btnAprobar").on("click",  () => enviarDictamen("Aprobar"));
    $("#btnRechazar").on("click", () => enviarDictamen("Rechazar"));
    $("#btnDevolver").on("click", () => enviarDictamen("Devolver"));

    // ── Activar Plan de Pagos ────────────────────────────────────────────────
    $("#btnActivarPlan").on("click", () => {
        Swal.fire({
            title: "¿Activar plan de pagos?",
            text:  "Se generarán 12 pagos mensuales según los parámetros vigentes.",
            icon:  "question",
            showCancelButton: true,
            confirmButtonText: "Sí, activar",
            cancelButtonText:  "Cancelar",
            confirmButtonColor: "#198754"
        }).then(result => {
            if (!result.isConfirmed) return;
            setLoading("#btnActivarPlan", true);

            $.ajax({
                url:    `${API_URL_BASE}/Ingeniero/Fincas/ActivarPlan/${FINCA_ID}`,
                method: "POST",
                success: (res) => {
                    setLoading("#btnActivarPlan", false);
                    if (res.success) {
                        Swal.fire({ icon: "success", title: "¡Plan activado!", text: res.message })
                            .then(() => window.location.href = "/Ingeniero/Cola");
                    } else {
                        showAlert("alertEvaluar", res.message, "danger");
                    }
                },
                error: () => {
                    setLoading("#btnActivarPlan", false);
                    showAlert("alertEvaluar", "Error al activar el plan.", "danger");
                }
            });
        });
    });
});
