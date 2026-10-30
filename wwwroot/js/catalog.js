// Catalogue filter bar. Everything works without this file — the dropdowns
// are <details> and the form is a plain GET — it only makes the toggles and
// the sort order apply at once, and keeps one dropdown open at a time.
(function () {
    'use strict';

    var form = document.querySelector('form.fb');
    if (!form) { return; }

    form.addEventListener('change', function (e) {
        if (e.target.hasAttribute('data-instant')) { form.submit(); }
    });

    var drops = form.querySelectorAll('details.fb-drop');
    var closeAll = function (except) {
        Array.prototype.forEach.call(drops, function (d) { if (d !== except) { d.open = false; } });
    };

    Array.prototype.forEach.call(drops, function (d) {
        d.addEventListener('toggle', function () { if (d.open) { closeAll(d); } });
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest('details.fb-drop')) { closeAll(null); }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closeAll(null); }
    });
})();
