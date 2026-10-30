// Client-side rules for validation attributes MVC does not ship an adapter
// for. Loaded by _ValidationScriptsPartial straight after the unobtrusive
// library, so the rules exist before it scans the page on DOM ready.
(function ($) {
    'use strict';
    if (!$ || !$.validator || !$.validator.unobtrusive) { return; }

    // MustBeTrueAttribute: the box has to be ticked. Checked directly on the
    // element because jQuery Validate reads a checkbox's value attribute,
    // which is "true" whether or not it is ticked.
    $.validator.addMethod('mustbetrue', function (value, element) {
        return element.checked === true;
    });
    $.validator.unobtrusive.adapters.addBool('mustbetrue');
})(window.jQuery);
