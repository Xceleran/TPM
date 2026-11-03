/*
 * Draggable splitter between a page's list panel and its details panel.
 *
 * Markup: <div class="cust-resizer" data-storage-key="..."> placed between
 * .cust-list-container and .cust-details-container inside .cust-section.
 * The list's width is remembered per browser under data-storage-key; double-click
 * (or Home) puts it back to the default 50/50. Arrow keys move it 20px.
 */
(function () {
    'use strict';

    var MIN_LIST = 320;
    var MIN_DETAILS = 400;

    function adjustTables(section) {
        // Header cells are sized by DataTables; re-measure after the panel width changes.
        if (!window.jQuery || !jQuery.fn.dataTable) return;
        jQuery(section).find('table.dataTable').each(function () {
            if (jQuery.fn.dataTable.isDataTable(this)) jQuery(this).DataTable().columns.adjust();
        });
    }

    function init(resizer) {
        var section = resizer.closest('.cust-section');
        var list = section && section.querySelector('.cust-list-container');
        if (!list) return;

        var key = resizer.getAttribute('data-storage-key') || 'panelListWidth';
        var isSideBySide = function () { return window.matchMedia('(min-width: 768px)').matches; };

        var clamp = function (w) {
            var max = section.clientWidth - resizer.offsetWidth - MIN_DETAILS;
            return Math.max(MIN_LIST, Math.min(w, max));
        };

        var apply = function (w) {
            if (w == null || !isSideBySide()) {
                list.style.flex = '';
                return;
            }
            list.style.flex = '0 0 ' + clamp(w) + 'px';
        };

        var save = function (w) {
            try {
                if (w == null) localStorage.removeItem(key);
                else localStorage.setItem(key, String(Math.round(w)));
            } catch (e) { /* storage unavailable: width just isn't remembered */ }
        };

        var saved = null;
        try { saved = parseInt(localStorage.getItem(key), 10) || null; } catch (e) { }
        apply(saved);

        var startX = 0;
        var startW = 0;

        resizer.addEventListener('pointerdown', function (e) {
            if (e.button !== 0 || !isSideBySide()) return;
            e.preventDefault();
            startX = e.clientX;
            startW = list.getBoundingClientRect().width;
            resizer.setPointerCapture(e.pointerId);
            resizer.classList.add('is-dragging');
            document.body.classList.add('cust-resizing');
        });

        resizer.addEventListener('pointermove', function (e) {
            if (!resizer.hasPointerCapture(e.pointerId)) return;
            apply(startW + (e.clientX - startX));
        });

        var endDrag = function (e) {
            if (!resizer.hasPointerCapture(e.pointerId)) return;
            resizer.releasePointerCapture(e.pointerId);
            resizer.classList.remove('is-dragging');
            document.body.classList.remove('cust-resizing');
            saved = list.getBoundingClientRect().width;
            save(saved);
            adjustTables(section);
        };
        resizer.addEventListener('pointerup', endDrag);
        resizer.addEventListener('pointercancel', endDrag);

        var reset = function () {
            saved = null;
            save(null);
            apply(null);
            adjustTables(section);
        };
        resizer.addEventListener('dblclick', reset);

        resizer.addEventListener('keydown', function (e) {
            if (!isSideBySide()) return;
            var current = list.getBoundingClientRect().width;
            var next = null;
            if (e.key === 'ArrowLeft') next = current - 20;
            else if (e.key === 'ArrowRight') next = current + 20;
            else if (e.key === 'Home') {
                e.preventDefault();
                reset();
                return;
            }
            if (next == null) return;
            e.preventDefault();
            apply(next);
            saved = list.getBoundingClientRect().width;
            save(saved);
            adjustTables(section);
        });

        // Keep a saved width inside the limits when the window shrinks or grows.
        var resizeTimer = null;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                apply(saved);
                adjustTables(section);
            }, 100);
        });
    }

    function initAll() {
        document.querySelectorAll('.cust-resizer').forEach(init);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initAll);
    else initAll();
})();
