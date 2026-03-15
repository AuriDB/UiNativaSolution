// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Ingeniero/evaluar.js
//  Formulario de dictamen: Aprobar | Devolver | Rechazar
// ═══════════════════════════════════════════════════════

function EvaluarView(fincaId) {

    let map    = null;
    let marker = null;

    this.InitView = () => {
        this.CargarDatos();
        this.BindEvents();
    };

    // ── Cargar datos ──────────────────────────────────────
    this.CargarDatos = () => {
        $.getJSON(`${API_URL_BASE}/Ingeniero/DatosEvaluar/${fincaId}`, data => {
            // Datos básicos
            $("#dNombreDueno").text(data.nombreDueno);
            $("#dCorreo").text(data.correo);
            $("#dHectareas").text(data.hectareas?.toFixed(4));
            $("#dVegetacion").text((data.vegetacion ?? 0).toFixed(2) + " %");
            $("#dHidrologia").text((data.hidrologia ?? 0).toFixed(2) + " %");
            $("#dTopografia").text((data.topografia ?? 0).toFixed(2) + " %");
            $("#dEsNacional").text(data.esNacional ? "Sí" : "No");
            $("#dFechaRegistro").text(data.fechaRegistro);

            // Adjuntos
            this.RenderAdjuntos(data.adjuntos);

            // Mapa
            this.InitMapa(data.lat, data.lng);
        }).fail(() => {
            showAlert("alertContainer", "Error al cargar los datos de la finca.", "danger");
        });
    };

    // ── Mapa Leaflet (solo lectura) ───────────────────────
    this.InitMapa = (lat, lng) => {
        if (!lat || !lng) return;

        map = L.map("mapEvaluar").setView([lat, lng], 12);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "© OpenStreetMap contributors"
        }).addTo(map);

        marker = L.marker([lat, lng]).addTo(map);
        marker.bindPopup(`Lat: ${lat.toFixed(6)}<br>Lng: ${lng.toFixed(6)}`).openPopup();
    };

    // ── Renderizar adjuntos ───────────────────────────────
    this.RenderAdjuntos = (adjuntos) => {
        if (!adjuntos || adjuntos.length === 0) {
            $("#listaAdjuntos").html(
                `<p class="text-muted small mb-0">
                    <i class="bi bi-folder2-open me-1"></i>Sin adjuntos.
                 </p>`
            );
            return;
        }

        const items = adjuntos.map(a =>
            `<div class="d-flex align-items-center gap-2 mb-2">
                <i class="bi bi-file-earmark-text text-primary"></i>
                <a href="${a.blobUrl}" target="_blank" class="text-decoration-none small flex-fill">
                    ${a.nombreArchivo}
                </a>
                <span class="text-muted small">${a.fechaSubida}</span>
             </div>`
        ).join("");

        $("#listaAdjuntos").html(items);
    };

    // ── Eventos ───────────────────────────────────────────
    this.BindEvents = () => {
        // Mostrar/ocultar panel de observaciones según dictamen
        $("input[name='dictamen']").on("change", () => {
            const val = $("input[name='dictamen']:checked").val();
            if (val === "Devolver" || val === "Rechazar") {
                $("#panelObservaciones").slideDown(150);
            } else {
                $("#panelObservaciones").slideUp(150);
                $("#txtObservaciones").val("");
            }
        });

        // Emitir dictamen
        $("#btnDictamen").on("click", () => {
            this.EmitirDictamen();
        });
    };

    // ── Emitir dictamen ───────────────────────────────────
    this.EmitirDictamen = () => {
        const dictamen = $("input[name='dictamen']:checked").val();

        if (!dictamen) {
            showAlert("alertContainer", "Selecciona una decisión (Aprobar, Devolver o Rechazar).", "warning");
            return;
        }

        const observaciones = $("#txtObservaciones").val().trim();

        if ((dictamen === "Devolver" || dictamen === "Rechazar") && !observaciones) {
            showAlert("alertContainer", "Las observaciones son obligatorias para Devolver o Rechazar.", "warning");
            return;
        }

        const textos = {
            "Aprobar":  { icon: "success", title: "¿Aprobar la finca?",  btn: "Sí, aprobar" },
            "Devolver": { icon: "warning", title: "¿Devolver la finca?", btn: "Sí, devolver" },
            "Rechazar": { icon: "error",   title: "¿Rechazar la finca?", btn: "Sí, rechazar" }
        };
        const t = textos[dictamen];

        Swal.fire({
            title:              t.title,
            text:               observaciones ? `Motivo: "${observaciones}"` : "",
            icon:               t.icon,
            showCancelButton:   true,
            confirmButtonText:  t.btn,
            cancelButtonText:   "Cancelar",
            confirmButtonColor: dictamen === "Aprobar" ? "#78c2ad" : (dictamen === "Devolver" ? "#f0ad4e" : "#dc3545")
        }).then(result => {
            if (!result.isConfirmed) return;

            setLoading("#btnDictamen", true);
            clearAlert("alertContainer");

            $.ajax({
                url:         `${API_URL_BASE}/Ingeniero/Evaluar`,
                method:      "POST",
                contentType: "application/json",
                data:        JSON.stringify({ id: fincaId, dictamen, observaciones: observaciones || null }),
                success: (res) => {
                    if (res.success) {
                        Swal.fire({
                            icon:             "success",
                            title:            "Dictamen registrado",
                            text:             res.message,
                            confirmButtonText: "Ir a Mis Asignadas",
                            confirmButtonColor: "#78c2ad"
                        }).then(() => {
                            window.location.href = "/Ingeniero/MisAsignadas";
                        });
                    } else {
                        showAlert("alertContainer", res.message || "Error al registrar el dictamen.", "danger");
                        setLoading("#btnDictamen", false);
                    }
                },
                error: () => {
                    showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                    setLoading("#btnDictamen", false);
                }
            });
        });
    };
}
