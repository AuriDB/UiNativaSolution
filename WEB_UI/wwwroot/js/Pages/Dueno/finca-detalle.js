// ── Dueno/Fincas/Detalle ────────────────────────────────────────────────────

$(document).ready(() => {
    // ── Cargar adjuntos ──────────────────────────────────────────────────────
    $.get(`${API_URL_BASE}/Dueno/Fincas/${FINCA_ID}/Adjuntos`, (data) => {
        if (!data || data.length === 0) {
            $("#listaAdjuntos").html('<p class="text-muted small mb-0">No hay adjuntos para esta finca.</p>');
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
        $("#listaAdjuntos").html('<p class="text-danger small mb-0">Error al cargar adjuntos.</p>');
    });

    // ── Reenviar ─────────────────────────────────────────────────────────────
    if (PUEDE_REENVIAR) {
        $("#btnReenviar").on("click", () => {
            Swal.fire({
                title: "¿Reenviar a evaluación?",
                text:  "Tu finca volverá a la cola FIFO para ser revisada por un ingeniero.",
                icon:  "question",
                showCancelButton: true,
                confirmButtonText: "Sí, reenviar",
                cancelButtonText:  "Cancelar",
                confirmButtonColor: "#ffc107"
            }).then(result => {
                if (!result.isConfirmed) return;
                setLoading("#btnReenviar", true);
                $.post(`${API_URL_BASE}/Dueno/Fincas/Reenviar/${FINCA_ID}`)
                    .done(() => window.location.reload())
                    .fail(() => {
                        setLoading("#btnReenviar", false);
                        Swal.fire({ icon: "error", title: "Error", text: "No se pudo reenviar la finca." });
                    });
            });
        });
    }

    // ── Editar (solo si Devuelta) ────────────────────────────────────────────
    const frmEditar = document.getElementById("frmEditar");
    if (frmEditar) {
        $("#frmEditar").on("submit", (e) => {
            e.preventDefault();
            setLoading("#btnEditar", true);

            const formData = new FormData(frmEditar);
            formData.set("esNacional", $("#esNacEdit").is(":checked") ? "true" : "false");

            $.ajax({
                url:         `${API_URL_BASE}/Dueno/Fincas/Editar/${FINCA_ID}`,
                method:      "POST",
                data:        formData,
                processData: false,
                contentType: false,
                complete: () => window.location.reload()
            });
        });
    }
});
