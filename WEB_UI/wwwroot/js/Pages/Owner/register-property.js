$(document).ready(() => {
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

    $("#frmNuevaFinca").on("submit", (e) => {
        e.preventDefault();

        if (!$("#lat").val() || !$("#lng").val()) {
            Swal.fire({ icon: "warning", title: "Ubicación requerida", text: "Haz clic en el mapa para seleccionar la ubicación de tu finca." });
            return;
        }

        setLoading("#btnGuardar", true);

        const formData = new FormData(document.getElementById("frmNuevaFinca"));
        formData.set("esNacional", $("#esNacional").is(":checked") ? "true" : "false");

        $.ajax({
            url:         `${API_URL_BASE}/Owner/RegisterProperty`,
            method:      "POST",
            data:        formData,
            processData: false,
            contentType: false,
            success: (res) => {
                if (res && res.success) {
                    Swal.fire({ icon: "success", title: "¡Finca registrada!", text: res.message })
                        .then(() => { window.location.href = "/Owner/MyProperties"; });
                } else {
                    setLoading("#btnGuardar", false);
                    Swal.fire({ icon: "error", title: "Error", text: res?.message || "Error al registrar la finca." });
                }
            },
            error: (xhr) => {
                setLoading("#btnGuardar", false);
                const msg = xhr.responseJSON?.message || "Error al registrar la finca.";
                Swal.fire({ icon: "error", title: "Error", text: msg });
            }
        });
    });
});
