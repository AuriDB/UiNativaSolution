function ProjectLandingView() {

    this.InitView = () => {
        this.AnimateCounters();
    };

    this.AnimateCounters = () => {
        const counters = $(".psa-counter");
        if (!counters.length) return;

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el     = $(entry.target);
                    const target = parseInt(el.data("target")) || 0;
                    const dur    = 2000;
                    const step   = Math.ceil(target / (dur / 16));
                    let current  = 0;

                    const iv = setInterval(() => {
                        current += step;
                        if (current >= target) {
                            current = target;
                            clearInterval(iv);
                        }
                        el.text(current.toLocaleString("es-CR"));
                    }, 16);

                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.3 });

        counters.each(function () { observer.observe(this); });
    };
}

$(document).ready(() => {
    const view = new ProjectLandingView();
    view.InitView();
});
