// Checks the admin sign in when the tab is shown again, and reloads the page if it has gone (signed out
// in another tab, expired, or the password was changed elsewhere). A circuit keeps the user it started
// with, so without this an open tab would keep working until it reconnects. The reload sends the user
// to sign in. The AngularJS admin found out on its next api call (a 401). Only interactive pages load
// this (see App.razor).
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
})();
