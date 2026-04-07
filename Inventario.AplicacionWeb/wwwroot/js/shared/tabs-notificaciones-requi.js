/**
 * Notificaciones por pestaña (contador en tab, parpadeo al abrir).
 * Requiere mount() desde la vista; opcional sondeo vía data-url-notif-snapshot en .tabla-requi-page.
 */
(function () {
    var PARPADEO_FILA_NUEVA_MS = 2000;
    var POLL_INTERVAL_MS = 22000;
    var _cfg = null;
    var _pollTimer = null;
    var _snapshotAnterior = null;

    function rootEl() {
        return _cfg && _cfg.container && _cfg.container.nodeType === 1 ? _cfg.container : document;
    }

    function prepararBadgeNotificacionVisible(badge) {
        if (!badge) return;
        badge.classList.remove("badge-almacen-tab--oculto");
        badge.removeAttribute("hidden");
        badge.style.removeProperty("display");
        badge.style.removeProperty("visibility");
    }

    function obtenerBadgeNotificacionTab(btn) {
        if (!btn) return null;
        var badge = btn.querySelector(".badge-almacen-tab");
        if (!badge) {
            badge = document.createElement("span");
            badge.className = "badge-almacen-tab badge-almacen-tab--oculto";
            badge.setAttribute("aria-hidden", "true");
            badge.textContent = "0";
            btn.appendChild(badge);
        }
        return badge;
    }

    function limpiarBadgeNotificacionTab(btn) {
        if (!btn) return;
        btn.querySelectorAll(".badge-almacen-tab").forEach(function (badge) {
            badge.textContent = "0";
            badge.classList.add("badge-almacen-tab--oculto");
            badge.setAttribute("hidden", "");
            badge.style.setProperty("display", "none", "important");
            badge.style.setProperty("visibility", "hidden", "important");
        });
    }

    function findRowDefault(idRequi) {
        var root = rootEl();
        if (_cfg && typeof _cfg.findRow === "function") {
            var r = _cfg.findRow(idRequi, root);
            if (r) return r;
        }
        return (
            root.querySelector('tr.fila-requi[data-requi-id="' + idRequi + '"]') ||
            document.querySelector('tr.fila-requi[data-requi-id="' + idRequi + '"]') ||
            root.querySelector('tr[data-id="' + idRequi + '"]') ||
            document.querySelector('tr[data-id="' + idRequi + '"]') ||
            root.querySelector('tr[data-requi="' + idRequi + '"]') ||
            document.querySelector('tr[data-requi="' + idRequi + '"]')
        );
    }

    function recibirNotificacionRequiImpl(idRequi, tabDestino) {
        var root = rootEl();
        var fila = findRowDefault(idRequi);
        if (fila) {
            fila.classList.add("fila-nueva");
        }

        var panelDestino = root.querySelector("#tab-" + tabDestino);
        if (!panelDestino) {
            panelDestino = document.getElementById("tab-" + tabDestino);
        }

        if (panelDestino && !panelDestino.classList.contains("activo")) {
            var btnTab =
                root.querySelector('.almacen-tabs-btn[data-tab="' + tabDestino + '"]') ||
                document.querySelector('.almacen-tabs-btn[data-tab="' + tabDestino + '"]');
            if (btnTab) {
                var badge = obtenerBadgeNotificacionTab(btnTab);
                if (badge) {
                    var conteoActual = parseInt(badge.textContent || "0", 10);
                    prepararBadgeNotificacionVisible(badge);
                    badge.textContent = conteoActual + 1;
                    badge.style.setProperty("display", "flex", "important");
                    badge.style.animation = "none";
                    badge.offsetHeight;
                    badge.style.animation = null;
                }
            }
        } else if (panelDestino && panelDestino.classList.contains("activo")) {
            if (_cfg && typeof _cfg.onNovedadEnTabActivo === "function") {
                _cfg.onNovedadEnTabActivo(idRequi, tabDestino);
            }
        }
    }

    function iniciarParpadeoFilasNuevasEnPanel(panel) {
        if (!panel) return;
        var filas = panel.querySelectorAll("tr.fila-nueva");
        if (!filas.length) return;
        filas.forEach(function (tr, i) {
            tr.classList.add("fila-nueva-parpadeo");
            if (i === 0) {
                try {
                    tr.scrollIntoView({ behavior: "smooth", block: "nearest" });
                } catch (e) {
                    tr.scrollIntoView();
                }
            }
        });
        window.setTimeout(function () {
            filas.forEach(function (tr) {
                tr.classList.remove("fila-nueva", "fila-nueva-parpadeo");
            });
        }, PARPADEO_FILA_NUEVA_MS);
    }

    function clonarSnapshot(data) {
        var o = {};
        if (!data || typeof data !== "object") return o;
        Object.keys(data).forEach(function (k) {
            var arr = data[k];
            o[k] = Array.isArray(arr) ? arr.slice() : [];
        });
        return o;
    }

    function procesarDiffSnapshot(data) {
        if (!_snapshotAnterior) {
            _snapshotAnterior = clonarSnapshot(data);
            return;
        }
        var prev = _snapshotAnterior;
        Object.keys(data).forEach(function (tab) {
            var prevSet = new Set(prev[tab] || []);
            var curr = data[tab] || [];
            curr.forEach(function (id) {
                if (!prevSet.has(id)) {
                    recibirNotificacionRequiImpl(id, tab);
                }
            });
        });
        _snapshotAnterior = clonarSnapshot(data);
    }

    function iniciarPollingSiAplica() {
        if (_pollTimer) {
            window.clearInterval(_pollTimer);
            _pollTimer = null;
        }
        _snapshotAnterior = null;
        var el = _cfg && _cfg.container;
        if (!el || !el.getAttribute) return;
        var url = (el.getAttribute("data-url-notif-snapshot") || "").trim();
        if (!url) return;

        function pollOnce() {
            fetch(url, {
                credentials: "same-origin",
                headers: { Accept: "application/json" },
            })
                .then(function (r) {
                    if (!r.ok) return null;
                    return r.json();
                })
                .then(function (data) {
                    if (!data || typeof data !== "object") return;
                    procesarDiffSnapshot(data);
                })
                .catch(function () {});
        }

        pollOnce();
        _pollTimer = window.setInterval(pollOnce, POLL_INTERVAL_MS);
    }

    window.TabsNotificacionesRequi = {
        limpiarBadgeNotificacionTab: limpiarBadgeNotificacionTab,
        iniciarParpadeoFilasNuevasEnPanel: iniciarParpadeoFilasNuevasEnPanel,

        /**
         * @param {object} config
         * @param {HTMLElement} config.container — .tabla-requi-page
         * @param {function(number, HTMLElement): HTMLElement} [config.findRow]
         * @param {function(number, string)} [config.onNovedadEnTabActivo]
         */
        mount: function (config) {
            _cfg = config || {};
            window.recibirNotificacionRequi = recibirNotificacionRequiImpl;
            iniciarPollingSiAplica();
        },
    };
})();
