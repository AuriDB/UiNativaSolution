// ═══════════════════════════════════════════════════
//  Sistema Nativa – site.js  (script base compartido)
// ═══════════════════════════════════════════════════

const API_URL_BASE = ""; // mismo origen — rutas relativas al host MVC

// Mostrar alerta Bootstrap en un contenedor
function showAlert(containerId, message, type = "danger") {
    const html = `
        <div class="alert alert-${type} alert-dismissible fade show rounded-3" role="alert">
            <i class="bi bi-${type === 'danger' ? 'exclamation-triangle' : type === 'success' ? 'check-circle' : 'info-circle'} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
    $(`#${containerId}`).html(html);
}

function clearAlert(containerId) {
    $(`#${containerId}`).html("");
}

// Spinner en botón
function setLoading(btn, loading = true, originalText = "") {
    if (loading) {
        $(btn).prop("disabled", true)
              .data("original", $(btn).html())
              .html('<span class="spinner-border spinner-border-sm me-2" role="status"></span>Procesando...');
    } else {
        $(btn).prop("disabled", false)
              .html($(btn).data("original") || originalText);
    }
}

// Toggle visibilidad contraseña
function bindTogglePassword(btnId, inputId, iconId) {
    $(`#${btnId}`).on("click", function () {
        const input = $(`#${inputId}`);
        const icon  = $(`#${iconId}`);
        const isPass = input.attr("type") === "password";
        input.attr("type", isPass ? "text" : "password");
        icon.toggleClass("bi-eye bi-eye-slash");
    });
}

// Evaluar fortaleza de contraseña
function evaluatePassword(pwd, nombre) {
    const checks = {
        len:     pwd.length >= 6,
        upper:   /[A-Z]/.test(pwd),
        lower:   /[a-z]/.test(pwd),
        num:     /[0-9]/.test(pwd),
        special: /[^A-Za-z0-9]/.test(pwd),
        noSpace: !/\s/.test(pwd),
        noName:  nombre ? !pwd.toLowerCase().includes(nombre.toLowerCase()) : true
    };

    const score = Object.values(checks).filter(Boolean).length;

    // Actualizar indicadores visuales si existen en el DOM
    Object.entries(checks).forEach(([key, ok]) => {
        const el = $(`#req-${key === 'noName' ? 'noname' : key === 'noSpace' ? 'space' : key}`);
        if (el.length) {
            el.html(ok
                ? `<i class="bi bi-check-circle-fill text-success me-1"></i>${el.text().trim().replace(/^[^\s]+\s/, '')}`
                : `<i class="bi bi-x-circle me-1 text-danger"></i>${el.text().trim().replace(/^[^\s]+\s/, '')}`)
              .toggleClass("text-success", ok).toggleClass("text-muted", !ok);
        }
    });

    // Barras de fortaleza
    const bars   = ["#str1","#str2","#str3","#str4"];
    const colors = ["bg-danger","bg-warning","bg-info","bg-success"];
    bars.forEach((b, i) => {
        $(b).removeClass("bg-danger bg-warning bg-info bg-success psa-str-active");
        if (score > i + 2) $(b).addClass(colors[Math.min(score - 3, 3)] + " psa-str-active");
    });

    const labels = ["", "Muy débil", "Débil", "Regular", "Fuerte", "Muy fuerte", "Excelente", "Perfecta"];
    if ($("#strengthLabel").length) {
        $("#strengthLabel").text(pwd.length === 0 ? "Ingresa tu contraseña" : (labels[score] || "Fuerte"));
    }

    return checks;
}
