// Checkout — reveals the billing and invoice blocks when they are asked for.
// Both are rendered with the hidden attribute already correct for the current
// state, so without scripting they simply stay as the server drew them.
(function () {
    'use strict';

    var toggles = document.querySelectorAll('[data-toggle]');

    Array.prototype.forEach.call(toggles, function (input) {
        var target = document.querySelector(input.getAttribute('data-toggle'));
        if (!target) { return; }

        function sync() { target.hidden = !input.checked; }

        input.addEventListener('change', sync);
        sync();
    });
})();
