// Variant picker.
//
// Every variant, its price and its stock are already in the page as JSON, so
// choosing a size is a local lookup rather than a round trip. Options that
// cannot be reached from the current selection are disabled rather than
// hidden, so the shopper can see that a size exists but is gone.
(function () {
    'use strict';

    var root = document.querySelector('.pdp[data-variants]');
    if (!root) { return; }

    var variants;
    try {
        variants = JSON.parse(root.getAttribute('data-variants'));
    } catch (e) {
        return;
    }
    if (!variants || !variants.length) { return; }

    var labels = {
        inStock: root.getAttribute('data-label-instock'),
        outOfStock: root.getAttribute('data-label-outofstock'),
        lowStock: root.getAttribute('data-label-lowstock'),
        unavailable: root.getAttribute('data-label-unavailable')
    };

    var pickers = Array.prototype.slice.call(root.querySelectorAll('[data-picker]'));
    var priceEl = root.querySelector('[data-price]');
    var wasEl = root.querySelector('[data-was]');
    var skuEl = root.querySelector('[data-sku]');
    var stockEl = root.querySelector('[data-stock]');
    var variantField = root.querySelector('[data-variant-id]');
    var addButton = root.querySelector('[data-add]');
    var qtyField = root.querySelector('.qty');

    var selection = {};
    pickers.forEach(function (picker) {
        var code = picker.getAttribute('data-picker');
        var chosen = picker.querySelector('.popt.on');
        selection[code] = chosen ? chosen.getAttribute('data-value') : null;
    });

    function money(amount) {
        // Group in threes with a dot, which is how RSD is written in Serbian.
        // Intl is avoided here so the page never disagrees with the server's
        // formatting, which is already culture-aware.
        return String(Math.round(amount)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }

    function matches(variant, against) {
        for (var code in against) {
            if (against[code] !== null && variant.attrs[code] !== against[code]) {
                return false;
            }
        }
        return true;
    }

    function findExact() {
        for (var i = 0; i < variants.length; i++) {
            if (matches(variants[i], selection)) { return variants[i]; }
        }
        return null;
    }

    // An option is reachable if some variant carries it alongside everything
    // chosen in the *other* groups.
    function reachable(code, value) {
        var probe = {};
        for (var other in selection) {
            if (other !== code) { probe[other] = selection[other]; }
        }
        probe[code] = value;

        for (var i = 0; i < variants.length; i++) {
            if (matches(variants[i], probe) && variants[i].stock > 0) { return true; }
        }
        return false;
    }

    function render() {
        pickers.forEach(function (picker) {
            var code = picker.getAttribute('data-picker');
            var chosenLabel = picker.querySelector('[data-chosen]');

            Array.prototype.forEach.call(picker.querySelectorAll('.popt'), function (button) {
                var value = button.getAttribute('data-value');
                var isOn = selection[code] === value;
                var canReach = reachable(code, value);

                button.classList.toggle('on', isOn);
                button.classList.toggle('gone', !canReach);
                button.setAttribute('aria-pressed', isOn ? 'true' : 'false');
                button.title = canReach
                    ? button.getAttribute('data-label')
                    : button.getAttribute('data-label') + ' \u2014 ' + labels.unavailable;

                if (isOn && chosenLabel) {
                    chosenLabel.textContent = button.getAttribute('data-label');
                }
            });
        });

        var variant = findExact();

        if (!variant) {
            if (stockEl) {
                stockEl.textContent = labels.unavailable;
                stockEl.className = 'pdp-stock out';
            }
            // Nothing sellable is selected, so the form must not be able to
            // post a stale variant id.
            if (variantField) { variantField.value = ''; }
            if (addButton) { addButton.disabled = true; }
            return;
        }

        if (variantField) { variantField.value = variant.id; }
        if (addButton) { addButton.disabled = variant.stock <= 0; }
        if (qtyField) {
            qtyField.max = Math.max(1, Math.min(20, variant.stock));
            if (Number(qtyField.value) > Number(qtyField.max)) { qtyField.value = qtyField.max; }
        }

        var price = (variant.sale !== undefined && variant.sale !== null) ? variant.sale : variant.price;
        var currency = root.getAttribute('data-currency') || '';

        if (priceEl) { priceEl.textContent = money(price) + ' ' + currency; }
        if (skuEl) { skuEl.textContent = variant.sku; }

        if (wasEl) {
            var discounted = variant.sale !== undefined && variant.sale !== null && variant.sale < variant.price;
            wasEl.hidden = !discounted;
            wasEl.textContent = discounted ? money(variant.price) : '';
        }

        if (stockEl) {
            if (variant.stock <= 0) {
                stockEl.textContent = labels.outOfStock;
                stockEl.className = 'pdp-stock out';
            } else if (variant.low) {
                stockEl.textContent = labels.lowStock.replace('{0}', variant.stock);
                stockEl.className = 'pdp-stock low';
            } else {
                stockEl.textContent = labels.inStock;
                stockEl.className = 'pdp-stock in';
            }
        }
    }

    pickers.forEach(function (picker) {
        var code = picker.getAttribute('data-picker');

        picker.addEventListener('click', function (event) {
            var button = event.target.closest('.popt');
            if (!button) { return; }

            selection[code] = button.getAttribute('data-value');

            // Picking a colour can strip the current size of any stock. Drop
            // the other selections that no longer lead anywhere rather than
            // leaving the page showing a combination that cannot be bought.
            pickers.forEach(function (other) {
                var otherCode = other.getAttribute('data-picker');
                if (otherCode === code || !selection[otherCode]) { return; }
                if (!reachable(otherCode, selection[otherCode])) { selection[otherCode] = null; }
            });

            render();
        });
    });

    render();
})();
