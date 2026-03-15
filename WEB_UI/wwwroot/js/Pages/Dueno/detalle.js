// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Dueno/detalle.js
//  Carga datos de la finca, mapa de solo lectura y
//  gestión de adjuntos (subir / eliminar).
// ═══════════════════════════════════════════════════════

function DetalleView(fincaId) {

    let map = null;

    this.InitView = () => {
        this.CargarDatos();
        this.BindSubirAdjunto();
    };

    // ── Cargar detalle desde servidor ───────────────────
    this.CargarDatos = () => {
        $.getJSON(`${API_URL_BASE}/Dueno/DatosDetalle/${fincaId}`, (d) => {

            // Badge estado
            const colorMap = {
                warning: "bg-warning text-dark", info: "bg-info text-white",
                success: "bg-success text-white", secondary: "bg-secondary text-white",
                danger:  "bg-danger text-white",  dark: "bg-dark text-white"
            };
            const cls = colorMap[d.estadoColor] || "bg-light";
            $("#badgeEstado").attr("class", `badge px-3 py-2 fs-6 ${cls}`)
                             .text(d.estado);

            // Datos
            $("#dHectareas").text(parseFloat(d.hectareas).toFixed(4) + " ha");
            $("#dVegetacion").text(parseFloat(d.vegetacion).toFixed(2) + "%");
            $("#dHidrologia").text(parseFloat(d.hidrologia).toFixed(2) + "%");
            $("#dTopografia").text(parseFloat(d.topografia).toFixed(2) + "%");
            $("#dEsNacional").text(d.esNacional ? "Sí" : "No");
            $("#dLat").text(parseFloat(d.lat).toFixed(6));
            $("#dLng").text(parseFloat(d.lng).toFixed(6));

            // Observaciones del ingeniero (estado Devuelta)
            if (d.estadoId === 4 && d.observaciones) {
                $("#txtObservaciones").text(d.observaciones);
                $("#panelObservaciones").removeClass("d-none");
            }

            // Botón editar (si puede modificar)
            if (d.puedeModificar) {
                $("#btnEditarWrap").html(
                    `<a href="/Dueno/Editar/${fincaId}" class="btn btn-outline-warning btn-sm rounded-3">
                         <i class="bi bi-pencil me-1"></i>Editar
                     </a>`
                );
                $("#panelSubirAdjunto").removeClass("d-none");
            }

            // Mapa solo lectura
            this.InitMapa(parseFloat(d.lat), parseFloat(d.lng));

            // Adjuntos
            this.RenderAdjuntos(d.adjuntos, d.puedeModificar);

        }).fail(() => {
            showAlert("alertContainer", "Error al cargar los datos de la finca.", "danger");
        });
    };

    // ── Mapa Leaflet solo lectura ────────────────────────
    this.InitMapa = (lat, lng) => {
        map = L.map("mapDetalle", { zoomControl: true }).setView([lat, lng], 13);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "© OpenStreetMap contributors"
        }).addTo(map);
        L.marker([lat, lng]).addTo(map)
            .bindPopup("Ubicación de la finca").openPopup();
    };

    // ── Renderizar lista de adjuntos ────────────────────
    this.RenderAdjuntos = (adjuntos, puedeModificar) => {
        if (!adjuntos || adjuntos.length === 0) {
            $("#listaAdjuntos").html(
                `<p class="text-muted text-center py-3">
                    <i class="bi bi-inbox fs-3 d-block mb-1"></i>
                    Sin adjuntos. ${puedeModificar ? "Sube el primero." : ""}
                 </p>`
            );
            return;
        }

        let html = '<div class="list-group list-group-flush">';
        adjuntos.forEach(a => {
            const ext  = (a.nombreArchivo || "").split(".").pop().toLowerCase();
            const icon = { pdf: "bi-file-pdf", jpg: "bi-file-image", jpeg: "bi-file-image",
                           png: "bi-file-image", doc: "bi-file-word", docx: "bi-file-word",
                           xls: "bi-file-excel", xlsx: "bi-file-excel" }[ext] || "bi-file-earmark";

            html += `
                <div class="list-group-item d-flex align-items-center gap-3 px-0" id="adj-${a.id}">
                    <i class="bi ${icon} fs-4 text-primary flex-shrink-0"></i>
                    <div class="flex-fill">
                        <a href="${a.blobUrl}" target="_blank" class="fw-semibold text-decoration-none">
                            ${a.nombreArchivo}
                        </a>
                        <div class="text-muted small">${a.fechaSubida}</div>
                    </div>
                    ${puedeModificar
                        ? `<button class="btn btn-outline-danger btn-sm rounded-3"
                               onclick="eliminarAdjunto(${a.id})">
                               <i class="bi bi-trash"></i>
                           </button>`
                        : ""}
                </div>`;
        });
        html += "</div>";
        $("#listaAdjuntos").html(html);
    };

    // ── Subir adjunto ────────────────────────────────────
    this.BindSubirAdjunto = () => {
        $("#inputAdjunto").on("change", function () {
            const archivo = this.files[0];
            if (!archivo) return;

            const formData = new FormData();
            formData.append("idActivo", fincaId);
            formData.append("archivo", archivo);

            setLoading("label[for='inputAdjunto']", true, '<i class="bi bi-upload me-1"></i>Subir archivo');

            $.ajax({
                url:         `${API_URL_BASE}/Dueno/SubirAdjunto`,
                method:      "POST",
                data:        formData,
                processData: false,
                contentType: false,
                success: (res) => {
                    if (res.success) {
                        // Recargar datos para actualizar lista
                        this.CargarDatos();
                        showAlert("alertContainer", "Archivo subido correctamente.", "success");
                    } else {
                        showAlert("alertContainer", res.message, "danger");
                    }
                    // Limpiar input
                    $("#inputAdjunto").val("");
                    setLoading("label[for='inputAdjunto']", false, '<i class="bi bi-upload me-1"></i>Subir archivo');
                }.bind(this),
                error: () => {
                    showAlert("alertContainer", "Error al subir el archivo.", "danger");
                    setLoading("label[for='inputAdjunto']", false, '<i class="bi bi-upload me-1"></i>Subir archivo');
                }
            });
        }.bind(this));
    };
}

// ── Función global para eliminar adjunto ─────────────
function eliminarAdjunto(idAdjunto) {
    Swal.fire({
        icon:              "warning",
        title:             "¿Eliminar adjunto?",
        text:              "Esta acción no se puede deshacer.",
        showCancelButton:  true,
        confirmButtonText: "Sí, eliminar",
        cancelButtonText:  "Cancelar",
        confirmButtonColor: "#ff7851"
    }).then((result) => {
        if (!result.isConfirmed) return;

        $.ajax({
            url:         `${API_URL_BASE}/Dueno/EliminarAdjunto`,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify({ idAdjunto }),
            success: (res) => {
                if (res.success) {
                    $(`#adj-${idAdjunto}`).fadeOut(300, function () { $(this).remove(); });
                } else {
                    showAlert("alertContainer", res.message, "danger");
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión.", "danger");
            }
        });
    });
}
