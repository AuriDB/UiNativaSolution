// ── Dueno/Fincas/Nueva — Leaflet map + form submit ──────────────────────────

$(document).ready(() => {
    // ── Mapa Leaflet (centrado en Costa Rica) ────────────────────────────────
    const map = L.map("mapNuevaFinca").setView([9.748917, -83.753428], 8);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: "© OpenStreetMap contributors",
        maxZoom: 18
    }).addTo(map);

    let marker = null;

    map.on("click", (e) => {
        const { lat, lng } = e.latlng;
        const latFixed = parseFloat(lat.toFixed(6));
        const lngFixed = parseFloat(lng.toFixed(6));

        $("#lat").val(latFixed);
        $("#lng").val(lngFixed);
        $("#coordsText").text(`Lat: ${latFixed}, Lng: ${lngFixed}`);
        $("#coordsInfo").removeClass("d-none");

        if (marker) {
            marker.setLatLng([latFixed, lngFixed]);
        } else {
            marker = L.marker([latFixed, lngFixed]).addTo(map);
        }
    });

    // ── Submit ────────────────────────────────────────────────────────────────
    $("#frmNuevaFinca").on("submit", (e) => {
        e.preventDefault();

        if (!$("#lat").val() || !$("#lng").val()) {
            Swal.fire({ icon: "warning", title: "Ubicación requerida", text: "Haz clic en el mapa para seleccionar la ubicación de tu finca." });
            return;
        }

        setLoading("#btnGuardar", true);

        const formData = new FormData(document.getElementById("frmNuevaFinca"));
        // checkbox: si no está marcado, el value no se envía — se lo agregamos explícito
        formData.set("esNacional", $("#esNacional").is(":checked") ? "true" : "false");

        $.ajax({
            url:         `${API_URL_BASE}/Dueno/Fincas/Nueva`,
            method:      "POST",
            data:        formData,
            processData: false,
            contentType: false,
            success: (html) => {
                // El controller hace redirect, jQuery sigue la redirección automáticamente
                // Si llegamos aquí con HTML, la página ya fue redirigida
                window.location.href = "/Dueno/Fincas";
            },
            error: (xhr) => {
                setLoading("#btnGuardar", false);
                const msg = xhr.responseJSON?.message || "Error al registrar la finca.";
                Swal.fire({ icon: "error", title: "Error", text: msg });
            }
        });
    });
});
