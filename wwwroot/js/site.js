// Header: the search field gets focus when it opens, and the search panel
// and the phone menu close when you click anywhere else.
(function () {
    'use strict';

    var search = document.querySelector('details.hd-search');
    if (search) {
        search.addEventListener('toggle', function () {
            if (search.open) {
                var input = search.querySelector('input[type="search"]');
                if (input) { input.focus(); }
            }
        });
    }

    document.addEventListener('click', function (e) {
        Array.prototype.forEach.call(document.querySelectorAll('details.hd-search[open], details.hd-menu[open]'), function (d) {
            if (!d.contains(e.target)) { d.open = false; }
        });
    });
})();

// "Print or save as PDF" on the withdrawal form. A button rather than an
// inline onclick, so the page works under a strict Content-Security-Policy.
document.addEventListener('click', function (event) {
    if (event.target.closest('[data-print]')) {
        window.print();
    }
});
