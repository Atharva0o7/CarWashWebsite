/* PitStop Control Room — small enhancements only. Every page works without this. */
(function () {
    "use strict";

    // Auto-dismiss the flash message so it does not linger over the data.
    var flash = document.querySelector(".adm-flash");
    if (flash) {
        setTimeout(function () {
            flash.style.transition = "opacity .3s ease";
            flash.style.opacity = "0";
            setTimeout(function () { flash.remove(); }, 320);
        }, 4000);
    }

    // "/" focuses the bookings search box, the one shortcut worth having.
    document.addEventListener("keydown", function (e) {
        if (e.key !== "/" || e.ctrlKey || e.metaKey || e.altKey) return;

        var tag = (e.target.tagName || "").toLowerCase();
        if (tag === "input" || tag === "textarea" || tag === "select") return;

        var search = document.querySelector('input[name="q"]');
        if (search) {
            e.preventDefault();
            search.focus();
            search.select();
        }
    });
})();
