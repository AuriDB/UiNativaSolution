// ═══════════════════════════════════════════════════════
//  Sistema Nativa – Dueno/finca.js
//  Formulario compartido para Registrar y Editar finca.
//  Usa Leaflet para selección de coordenadas en el mapa.
// ═══════════════════════════════════════════════════════

function FincaView(opts) {
    // opts = { modo: "crear"|"editar", lat?: number, lng?: number }
    const modo = opts.modo || "crear";

    // Centro por defecto: Costa Rica
    const CR_LAT = 9.748917;
    const CR_LNG = -83.753428;

    let map     = null;
    let marker  = null;

    this.InitView = () => {
        this.InitMapa(opts.lat || CR_LAT, opts.lng || CR_LNG);
        this.BindEvents();
    };

    // ── Mapa Leaflet ────────────────────────────────────
    this.InitMapa = (lat, lng) => {
        map = L.map("mapFinca").setView([lat, lng], 9);

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "© OpenStreetMap contributors"
        }).addTo(map);

        // Marcador inicial (solo en editar o si hay coords pre-cargadas)
        if (modo === "editar" || (opts.lat && opts.lng)) {
            marker = L.marker([lat, lng], { draggable: true }).addTo(map);
            marker.on("dragend", () => {
                const pos = marker.getLatLng();
                $("#lat").val(pos.lat.toFixed(6));
                $("#lng").val(pos.lng.toFixed(6));
            });
        }

        // Click en mapa para colocar/mover marcador
        map.on("click", (e) => {
            const { lat, lng } = e.latlng;
            if (marker) {
                marker.setLatLng([lat, lng]);
            } else {
                marker = L.marker([lat, lng], { draggable: true }).addTo(map);
                marker.on("dragend", () => {
                    const pos = marker.getLatLng();
                    $("#lat").val(pos.lat.toFixed(6));
                    $("#lng").val(pos.lng.toFixed(6));
                });
            }
            $("#lat").val(lat.toFixed(6));
            $("#lng").val(lng.toFixed(6));
        });
    };

    // ── Eventos ─────────────────────────────────────────
    this.BindEvents = () => {
        // Centrar en Costa Rica
        $("#btnCentrarCR").on("click", () => {
            map.setView([CR_LAT, CR_LNG], 9);
        });

        // Sincronizar inputs Lat/Lng con marcador
        $("#lat, #lng").on("change", () => {
            const lat = parseFloat($("#lat").val());
            const lng = parseFloat($("#lng").val());
            if (isNaN(lat) || isNaN(lng)) return;
            if (marker) {
                marker.setLatLng([lat, lng]);
            } else {
                marker = L.marker([lat, lng], { draggable: true }).addTo(map);
            }
            map.setView([lat, lng], 12);
        });

        // Envío del formulario
        $("#frmFinca").on("submit", (e) => {
            e.preventDefault();
            this.Guardar();
        });
    };

    // ── Guardar ──────────────────────────────────────────
    this.Guardar = () => {
        const hectareas = parseFloat($("#hectareas").val());
        const lat       = parseFloat($("#lat").val());
        const lng       = parseFloat($("#lng").val());

        if (!hectareas || hectareas <= 0) {
            showAlert("alertContainer", "Las hectáreas deben ser un número mayor a 0.", "warning");
            return;
        }
        if (!marker || isNaN(lat) || isNaN(lng)) {
            showAlert("alertContainer", "Haz clic en el mapa para establecer la ubicación.", "warning");
            return;
        }

        const payload = {
            id:         parseInt($("#fincaId").val()) || 0,
            hectareas:  hectareas,
            vegetacion: parseFloat($("#vegetacion").val()) || 0,
            hidrologia: parseFloat($("#hidrologia").val()) || 0,
            topografia: parseFloat($("#topografia").val()) || 0,
            esNacional: $("#esNacional").is(":checked"),
            lat:        lat,
            lng:        lng
        };

        const url = modo === "editar"
            ? `${API_URL_BASE}/Dueno/Editar`
            : `${API_URL_BASE}/Dueno/Registrar`;

        setLoading("#btnGuardar", true);
        clearAlert("alertContainer");

        $.ajax({
            url:         url,
            method:      "POST",
            contentType: "application/json",
            data:        JSON.stringify(payload),
            success: (res) => {
                if (res.success) {
                    Swal.fire({
                        icon:             "success",
                        title:            modo === "editar" ? "¡Finca actualizada!" : "¡Finca registrada!",
                        text:             res.message,
                        confirmButtonText: "Ver mis fincas",
                        confirmButtonColor: "#78c2ad"
                    }).then(() => {
                        window.location.href = "/Dueno";
                    });
                } else {
                    showAlert("alertContainer", res.message || "Error al guardar.", "danger");
                    setLoading("#btnGuardar", false);
                }
            },
            error: () => {
                showAlert("alertContainer", "Error de conexión. Intenta de nuevo.", "danger");
                setLoading("#btnGuardar", false);
            }
        });
    };
}
