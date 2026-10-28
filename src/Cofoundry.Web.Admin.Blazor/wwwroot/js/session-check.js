// Checks the admin sign in when the tab is shown again or the user moves to another admin page, and reloads
// the page if it has gone (signed out in another tab, expired, or the password was changed elsewhere). A
// circuit keeps the user it started with, so without this an open tab would keep working until it
// reconnects. The reload sends the user to sign in, with the page they were on (or moving to) as the return
// url. The AngularJS admin found out on its next api call (a 401). Only interactive pages load this (see
// App.razor).
(function () {
    var url = document.currentScript.getAttribute('data-url');
    var minInterval = 15000;
    var lastCheck = 0;
    var checking = false;

    function check() {
        if (checking || document.visibilityState !== 'visible' || Date.now() - lastCheck < minInterval) return;
        checking = true;
        lastCheck = Date.now();

        fetch(url, { credentials: 'same-origin', cache: 'no-store' })
            .then(function (response) {
                if (response.status === 401) location.reload();
            })
            .catch(function () { /* offline: Blazor's reconnect UI covers this */ })
            .finally(function () { checking = false; });
    }

    document.addEventListener('visibilitychange', check);
    window.addEventListener('focus', check);

    // The interactive router moves between pages with pushState (links) and popstate (back/forward). The
    // address already shows the new page when check runs, so the reload goes there.
    var pushState = history.pushState;
    history.pushState = function () {
        var result = pushState.apply(this, arguments);
        check();
        return result;
    };
    window.addEventListener('popstate', check);
})();
