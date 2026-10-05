/*
 * FSMLoading - the single branded loading overlay for FSM.
 *
 * Every page used to invent its own: Appointments carried four .loading-overlay divs,
 * BillableItems had #loadingOverlay, the CSL tabs wrote "Loading…" into a table cell, and the
 * appointment modals used showApptUpdateLoading. None of them looked like the product.
 *
 * Usage:
 *     FSMLoading.show();                      // full screen, default message
 *     FSMLoading.show('Loading invoices…');
 *     FSMLoading.hide();
 *     FSMLoading.showIn('#invoices', 'Loading invoices…');   // covers one panel
 *     FSMLoading.hideIn('#invoices');
 *
 * show()/hide() are reference counted, so two overlapping loads do not leave the screen stuck
 * behind the first one to finish.
 */
(function (window, document) {
    'use strict';

    var DEFAULT_TEXT = 'Loading…';
    var BACKDROP_ID = 'fsmLoadingBackdrop';
    var pending = 0;

    // The master page publishes the resolved logo URL, because a script cannot call ResolveUrl
    // and FSM pages sit under /fsm/ on mxp but at the root elsewhere. Fall back to a path
    // relative to this script's own src so the overlay still brands itself if that is missing.
    function logoUrl() {
        if (window.FSM_BRAND_LOGO) return window.FSM_BRAND_LOGO;
        try {
            var me = document.querySelector('script[src*="fsm-loading.js"]');
            if (me && me.src) {
                return me.src.replace(/Scripts\/fsm-loading\.js.*$/i, 'images/FMSXceleran.png');
            }
        } catch (e) { /* fall through */ }
        return 'images/FMSXceleran.png';
    }

    function buildCard(text) {
        var card = document.createElement('div');
        card.className = 'fsm-loading-card';

        var img = document.createElement('img');
        img.className = 'fsm-loading-logo';
        img.alt = 'Xceleran';
        img.src = logoUrl();
        // A missing logo must not leave a broken-image icon sitting in the overlay.
        img.onerror = function () { this.style.display = 'none'; };
        card.appendChild(img);

        var spinner = document.createElement('div');
        spinner.className = 'fsm-loading-spinner';
        spinner.setAttribute('aria-hidden', 'true');
        card.appendChild(spinner);

        var p = document.createElement('p');
        p.className = 'fsm-loading-text';
        p.textContent = text || DEFAULT_TEXT;   // textContent, so a message can never inject markup
        card.appendChild(p);

        return card;
    }

    function makeBackdrop(text, inline) {
        var el = document.createElement('div');
        el.className = 'fsm-loading-backdrop' + (inline ? ' fsm-loading-inline' : '');
        el.setAttribute('role', 'status');
        el.setAttribute('aria-live', 'polite');
        el.appendChild(buildCard(text));
        return el;
    }

    function show(text) {
        pending++;
        var el = document.getElementById(BACKDROP_ID);
        if (!el) {
            el = makeBackdrop(text, false);
            el.id = BACKDROP_ID;
            document.body.appendChild(el);
            // Next frame, so the opacity transition actually runs.
            window.requestAnimationFrame(function () { el.classList.add('is-visible'); });
        } else if (text) {
            setText(el, text);
        }
        return el;
    }

    function hide(force) {
        pending = force ? 0 : Math.max(0, pending - 1);
        if (pending > 0) return;
        var el = document.getElementById(BACKDROP_ID);
        if (!el) return;
        el.classList.remove('is-visible');
        window.setTimeout(function () {
            if (el.parentNode && !el.classList.contains('is-visible')) el.parentNode.removeChild(el);
        }, 200);
    }

    function setText(el, text) {
        var p = el.querySelector('.fsm-loading-text');
        if (p) p.textContent = text || DEFAULT_TEXT;
    }

    function hostFor(target) {
        return typeof target === 'string' ? document.querySelector(target) : target;
    }

    function showIn(target, text) {
        var host = hostFor(target);
        if (!host) return null;
        // The overlay is absolutely positioned, so the host has to establish a containing block.
        var pos = window.getComputedStyle(host).position;
        if (pos === 'static') host.style.position = 'relative';

        var existing = host.querySelector(':scope > .fsm-loading-inline');
        if (existing) {
            setText(existing, text);
            return existing;
        }
        var el = makeBackdrop(text, true);
        host.appendChild(el);
        window.requestAnimationFrame(function () { el.classList.add('is-visible'); });
        return el;
    }

    function hideIn(target) {
        var host = hostFor(target);
        if (!host) return;
        var el = host.querySelector(':scope > .fsm-loading-inline');
        if (!el) return;
        el.classList.remove('is-visible');
        window.setTimeout(function () {
            if (el.parentNode) el.parentNode.removeChild(el);
        }, 200);
    }

    window.FSMLoading = {
        show: show,
        hide: hide,
        hideAll: function () { hide(true); },
        showIn: showIn,
        hideIn: hideIn,
        isVisible: function () { return !!document.getElementById(BACKDROP_ID); }
    };
})(window, document);
