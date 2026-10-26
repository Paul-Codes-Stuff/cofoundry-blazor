// Opens and closes the nav on small screens (see Styles/Blazor/_mobile.scss), by toggling "nav-open" on <html>,
// which Blazor doesn't render, so it survives re-renders. Listens on the document because the layout's
// markup is replaced as pages render.
(function () {
    var openClass = 'nav-open';

    function setOpen(open) {
        document.documentElement.classList.toggle(openClass, open);
        var toggle = document.querySelector('.mobile-nav-toggle');
        if (toggle) {
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    }

    document.addEventListener('click', function (e) {
        if (e.target.closest('.mobile-nav-toggle')) {
            setOpen(!document.documentElement.classList.contains(openClass));
        } else if (e.target.closest('.mobile-nav-backdrop, .main-nav .result a, .main-nav .logout a')) {
            // A category's button stays open: it shows that category's modules.
            setOpen(false);
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            setOpen(false);
        }
    });
})();
