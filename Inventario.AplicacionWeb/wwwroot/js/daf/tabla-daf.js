(function () {
    var container = document.querySelector(".tabla-requi-page");
    var obtenerDetallesUrl = container ? container.getAttribute("data-url-obtener-detalles") : "";
    var verPdfRequisicionUrl = container ? container.getAttribute("data-url-ver-pdf-requi") : "";
    var verPdfServicioUrl = container ? container.getAttribute("data-url-ver-pdf-servicio") : "";
    var atenderUrl = container ? container.dataset.urlAtender : "";
    var urlObtenerProgreso = container ? container.getAttribute("data-url-obtener-progreso") : "";

    var fechaSeleccionada = "";
    var requisicionActual = null;

    // ── Flatpickr ────────────────────────────────────────────────────────────
    var fpInstance = flatpickr("#filtroFecha", {
        locale: "es",
        dateFormat: "d/m/Y",
        allowInput: false,
        disableMobile: true,
        onChange: function (selectedDates, dateStr) {
            fechaSeleccionada = dateStr;
            var btnLimpiar = document.getElementById("btnLimpiarFecha");
            if (btnLimpiar) btnLimpiar.style.display = dateStr ? "inline" : "none";
            filtrarTabla();
        }
    });

    window.limpiarFecha = function () {
        fpInstance.clear();
        fechaSeleccionada = "";
        var btnLimpiar = document.getElementById("btnLimpiarFecha");
        if (btnLimpiar) btnLimpiar.style.display = "none";
        filtrarTabla();
    };

    // ── Paginación ───────────────────────────────────────────────────────────
    const TAMANO_PAGINA = 7;
    var paginaActual = 1;
    var paginacionContainer = document.getElementById("paginacionRequisiciones");

    function filtrarTabla() {
        paginaActual = 1;
        aplicarPaginacion();
    }

    function aplicarPaginacion() {
        var tbody = document.querySelector("#tablaRequisicionesPrincipal tbody");
        if (!tbody) return;

        var textoNumReq = (document.getElementById("filtroNumReq").value || "").toLowerCase().trim();
        var textoDepto = (document.getElementById("filtroDepartamento").value || "").toLowerCase().trim();
        var textoEstado = (document.getElementById("filtroEstado").value || "").toLowerCase().trim();

        var todasLasFilas = [].slice.call(tbody.querySelectorAll("tr")).filter(function (tr) {
            return !tr.classList.contains("fila-vacia") && !tr.classList.contains("fila-detalle");
        });

        var filasVisibles = [];
        todasLasFilas.forEach(function (fila) {
            var celdas = fila.querySelectorAll("td");
            if (!celdas.length) return;

            var folio = (celdas[1] && celdas[1].textContent.toLowerCase()) || "";
            var fecha = (celdas[2] && celdas[2].textContent.trim()) || "";
            var depto = (celdas[3] && celdas[3].textContent.toLowerCase()) || "";
            var estado = (celdas[6] && celdas[6].textContent.toLowerCase()) || "";

            var pasa = (!textoNumReq || folio.indexOf(textoNumReq) !== -1)
                && (!fechaSeleccionada || fecha === fechaSeleccionada)
                && (!textoDepto || depto.indexOf(textoDepto) !== -1)
                && (!textoEstado || estado.indexOf(textoEstado) !== -1);

            if (pasa) {
                filasVisibles.push(fila);
            } else {
                fila.style.display = "none";
                var sig = fila.nextElementSibling;
                if (sig && sig.classList.contains("fila-detalle")) sig.style.display = "none";
            }
        });

        var total = filasVisibles.length;
        var totalPags = Math.max(1, Math.ceil(total / TAMANO_PAGINA));
        if (paginaActual > totalPags) paginaActual = totalPags;

        var inicio = (paginaActual - 1) * TAMANO_PAGINA;
        var fin = inicio + TAMANO_PAGINA;

        filasVisibles.forEach(function (fila, i) {
            var visible = i >= inicio && i < fin;
            fila.style.display = visible ? "" : "none";
            var sig = fila.nextElementSibling;
            if (sig && sig.classList.contains("fila-detalle"))
                sig.style.display = visible ? "" : "none";
        });

        mostrarMensajeVacio(total, tbody);
        renderizarPaginacion(total, inicio, fin, totalPags);
    }

    function renderizarPaginacion(total, inicio, fin, totalPags) {
        if (!paginacionContainer) return;
        if (total === 0) { paginacionContainer.innerHTML = ""; return; }

        var resFinal = Math.min(fin, total);
        var html = '<div class="almacen-paginacion-info">Mostrando ' + (inicio + 1) + '-' + resFinal + ' de ' + total + ' requisiciones</div>';
        html += '<div class="almacen-paginacion-btns">';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="prev"' + (paginaActual <= 1 ? " disabled" : "") + '>Anterior</button>';
        html += '<span class="almacen-paginacion-nums">';

        var set = {};
        for (var i = 1; i <= Math.min(3, totalPags); i++) set[i] = true;
        set[paginaActual] = true;
        if (paginaActual > 1) set[paginaActual - 1] = true;
        if (paginaActual < totalPags) set[paginaActual + 1] = true;
        for (var j = Math.max(1, totalPags - 2); j <= totalPags; j++) set[j] = true;

        var nums = Object.keys(set).map(Number).sort(function (a, b) { return a - b; });
        var prev = 0;
        nums.forEach(function (p) {
            if (prev !== 0 && p > prev + 1) html += '<span class="almacen-paginacion-ellipsis">…</span>';
            html += '<button type="button" class="almacen-paginacion-btn almacen-paginacion-num' + (p === paginaActual ? " activo" : "") + '" data-pagina="' + p + '">' + p + '</button>';
            prev = p;
        });
        html += '</span>';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="next"' + (paginaActual >= totalPags ? " disabled" : "") + '>Siguiente</button>';
        html += '</div>';

        paginacionContainer.innerHTML = html;

        paginacionContainer.querySelectorAll(".almacen-paginacion-btn").forEach(function (btn) {
            btn.addEventListener("click", function () {
                if (this.disabled) return;
                var pg = this.getAttribute("data-pagina");
                if (pg === "prev") paginaActual = Math.max(1, paginaActual - 1);
                else if (pg === "next") paginaActual = Math.min(totalPags, paginaActual + 1);
                else paginaActual = parseInt(pg, 10);
                aplicarPaginacion();
            });
        });
    }

    function mostrarMensajeVacio(total, tbody) {
        var filaVacia = tbody.querySelector(".fila-vacia");
        if (total === 0) {
            if (!filaVacia) {
                filaVacia = document.createElement("tr");
                filaVacia.className = "fila-vacia";
                filaVacia.innerHTML = '<td colspan="8" class="text-center">Sin resultados para los filtros aplicados</td>';
                tbody.appendChild(filaVacia);
            }
        } else {
            if (filaVacia) filaVacia.remove();
        }
    }

    // ── Abrir modal revisión ─────────────────────────────────────────────────
    window.revisarRequisicion = function (idRequi) {
        requisicionActual = idRequi;

        // Limpiar
        document.getElementById("contenedorArchivosReadonly").innerHTML = "";
        document.getElementById("grupoArchivosSiaf").innerHTML = "";
        document.getElementById("grupoArchivosTablaApi").innerHTML = "";
        document.getElementById("grupoNumeroApi").innerHTML = "";
        document.getElementById("contenedorArchivosFinancieros").style.display = "none";
        document.getElementById("galeriaFotosDetalle").innerHTML = "";
        document.getElementById("seccionFotosDetalle").style.display = "none";
        document.getElementById("txtObservaciones").value = "";

        if (!obtenerDetallesUrl) return;

        $.get(obtenerDetallesUrl, { idMaestro: idRequi }, function (data) {

            // Tabla artículos
            var articulos = data.articulos || [];
            var contenido = "";
            if (!articulos.length) {
                contenido = '<tr><td colspan="5" class="text-center">Sin artículos</td></tr>';
            } else {
                articulos.forEach(function (item) {
                    var textoCompleto = item.descripcionDetallada || "";
                    var textoCorto = textoCompleto.length > 28
                        ? textoCompleto.substring(0, 28) + "…"
                        : textoCompleto || "Sin descripción...";
                    var fullEscapado = (textoCompleto || "").replace(/"/g, "&quot;");
                    contenido +=
                        "<tr>" +
                        "<td>" + (item.numPartida || "") + "</td>" +
                        "<td>" + (item.cantidad || "") + "</td>" +
                        "<td>" + (item.unidadMedida || "") + "</td>" +
                        "<td>" + (item.descripcion || "") + "</td>" +
                        '<td><div class="desc-preview-modal" data-full="' + fullEscapado + '" onclick="verDescDetalleModal(this)">' +
                        '<span class="desc-texto-preview' + (textoCompleto ? " tiene-texto" : "") + '">' + textoCorto + '</span>' +
                        '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                        "</tr>";
                });
            }
            $("#tablaDetalle").html(contenido);

            // Subtítulo
            var subtitulo = document.getElementById("modalSubtitulo");
            if (subtitulo) subtitulo.textContent = "Detalle de partidas · Total: " + articulos.length + " partidas";

            // Fotos
            if (data.tipoServicio === "Servicio Impresion" && data.fotos && data.fotos.length) {
                var galeria = document.getElementById("galeriaFotosDetalle");
                galeria.innerHTML = "";
                data.fotos.forEach(function (ruta) {
                    var img = document.createElement("img");
                    img.src = ruta;
                    img.className = "modal-galeria-foto-thumb";
                    img.addEventListener("click", function () { 
                        if (window.abrirVisorImagenTabla) {
                            window.abrirVisorImagenTabla(ruta);
                        } else {
                            window.open(ruta, "_blank");
                        }
                    });
                    galeria.appendChild(img);
                });
                document.getElementById("seccionFotosDetalle").style.display = "block";
            }

            // Archivos cotizaciones / cuadro
            renderizarArchivosReadonly(data.cotizaciones || [], data.cuadroComparativo || []);

            // Archivos financieros: SIAF, TablaApi, NumeroApi
            renderizarArchivosFinancieros(
                data.archivosSiaf || [],
                data.archivosTablaApi || [],
                data.numeroApi || null
            );

            // Selects PP / FF / Programa / Municipio (readonly)
            $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").each(function () {
                if ($(this).data("select2")) $(this).select2("destroy");
            });
            $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").select2({
                dropdownParent: $("#modalDetalle"),
                width: "100%",
                language: "es"
            }).prop("disabled", true);

            if (data.idPp) $("#actividadSeleccionada").val(data.idPp).trigger("change");
            if (data.ff) {
                $("#ffSelect option").filter(function () { return $(this).text().trim() === data.ff; }).prop("selected", true);
                $("#ffSelect").trigger("change");
            }
            if (data.tipoPrograma) {
                $("#tipoProgramaSelect option").filter(function () { return $(this).text().trim() === data.tipoPrograma; }).prop("selected", true);
                $("#tipoProgramaSelect").trigger("change");
            }
            if (data.claveRegion) $("#municipio").val(data.claveRegion).trigger("change");

            // Observaciones financieros
            document.getElementById("txtObservaciones").value = data.observaciones || "";

            new bootstrap.Modal(document.getElementById("modalDetalle")).show();
        });
    };

    // ── Render archivos cotizaciones / cuadro ────────────────────────────────
    function renderizarArchivosReadonly(cotizaciones, cuadro) {
        if (window.ModalAdjuntos) {
            window.ModalAdjuntos.renderizarArchivosReadonly(cotizaciones, cuadro);
        } else {
            var contenedor = document.getElementById("contenedorArchivosReadonly");
            contenedor.innerHTML = "";
        }
    }

    // ── Render archivos financieros (SIAF, TablaApi, Nº API) ─────────────────
    function renderizarArchivosFinancieros(siaf, tablaApi, numeroApi) {
        var seccion = document.getElementById("contenedorArchivosFinancieros");
        var hayContenido = siaf.length || tablaApi.length || numeroApi;
        if (!hayContenido) { seccion.style.display = "none"; return; }

        seccion.style.display = "block";

        function renderGrupoFinanciero(containerId, titulo, archivos) {
            var el = document.getElementById(containerId);
            if (!archivos.length) { el.innerHTML = ""; return; }
            
            if (window.ModalAdjuntos) {
                el.innerHTML = window.ModalAdjuntos.renderGrupoHtml(titulo, archivos);
                window.ModalAdjuntos.enlazarEventosContenedor(el);
            } else {
                // ... fallback ...
                el.innerHTML = "";
            }
        }

        renderGrupoFinanciero("grupoArchivosSiaf", "Documento SIAF", siaf);
        renderGrupoFinanciero("grupoArchivosTablaApi", "Tabla de API", tablaApi);

        var numEl = document.getElementById("grupoNumeroApi");
        if (numeroApi) {
            numEl.innerHTML = '<label style="font-size:13px;font-weight:600;color:#555;margin-bottom:4px;display:block;">Nº API</label>'
                + '<span style="font-size:13px;padding:4px 10px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;color:#166534;">'
                + '<i class="fa-solid fa-hashtag" style="margin-right:4px;"></i>' + numeroApi + '</span>';
        } else {
            numEl.innerHTML = "";
        }
    }

    // ── Marcar como revisado ─────────────────────────────────────────────────
    window.marcarRevisado = function () {
        Swal.fire({
            title: "¿Marcar como revisado?",
            text: "Se registrará el visto bueno de esta requisición.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            cancelButtonColor: "var(--slate-500)",
            confirmButtonText: "Sí, marcar",
            cancelButtonText: "Cancelar"
        }).then(function (result) {
            if (!result.isConfirmed) return;
            $.ajax({
                url: atenderUrl,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(requisicionActual),
                success: function () {
                    bootstrap.Modal.getInstance(document.getElementById("modalDetalle")).hide();
                    Swal.fire({
                        icon: "success", title: "Requisición marcada como revisada",
                        timer: 2000, showConfirmButton: false
                    }).then(function () { location.reload(); });
                },
                error: function () {
                    Swal.fire({ icon: "error", title: "Error al registrar la revisión." });
                }
            });
        });
    };

    // ── Expandir / colapsar filas detalle ────────────────────────────────────
    function colapsarTodasDetalle(tabla) {
        if (!tabla) return;
        tabla.querySelectorAll(".fila-detalle.expanded").forEach(function (fila) {
            fila.classList.remove("expanded");
            fila.classList.add("collapsed");
        });
    }

    if (container) {
        container.addEventListener("click", function (e) {
            var tr = e.target.closest("tr");
            if (!tr || tr.classList.contains("fila-detalle") || tr.classList.contains("fila-vacia")) return;
            if (e.target.closest(".acciones-grupo, button, a.btn-accion")) return;
            if (!tr.classList.contains("fila-requi")) return;

            var tabla = tr.closest("table.tabla-requisiciones");
            var filaDetalle = tr.nextElementSibling;
            if (!filaDetalle || !filaDetalle.classList.contains("fila-detalle")) return;

            var yaExpandida = filaDetalle.classList.contains("expanded");
            colapsarTodasDetalle(tabla);
            if (!yaExpandida) {
                filaDetalle.classList.remove("collapsed");
                filaDetalle.classList.add("expanded");
            }
        });

        document.addEventListener("click", function (e) {
            if (!e.target.closest("table.tabla-requisiciones")) {
                document.querySelectorAll("table.tabla-requisiciones").forEach(function (t) {
                    colapsarTodasDetalle(t);
                });
            }
        });
    }

    // ── Filtros ──────────────────────────────────────────────────────────────
    var filtroNumReq = document.getElementById("filtroNumReq");
    var filtroDepto = document.getElementById("filtroDepartamento");
    var filtroEstado = document.getElementById("filtroEstado");
    if (filtroNumReq) filtroNumReq.addEventListener("input", filtrarTabla);
    if (filtroDepto) filtroDepto.addEventListener("input", filtrarTabla);
    if (filtroEstado) filtroEstado.addEventListener("change", filtrarTabla);

    aplicarPaginacion();

    // ── PDF ──────────────────────────────────────────────────────────────────
    window.verPdf = function (id) {
        var fila = document.querySelector('tr.fila-requi[data-requi-id="' + id + '"]');
        var esServicio = fila && fila.getAttribute("data-requi-servicio") === "true";
        var urlBase = esServicio ? verPdfServicioUrl : verPdfRequisicionUrl;
        window.open(urlBase.replace(/\/$/, "") + "/" + id, "_blank");
    };

    // ── Panel descripción detallada ──────────────────────────────────────────
    var _descPanelModalTrigger = null;

    window.verDescDetalleModal = function (el) {
        var panel = document.getElementById("desc-panel-modal");
        var textarea = document.getElementById("desc-textarea-modal");
        if (!panel || !textarea) return;
        textarea.value = el.dataset.full || "(Sin descripción detallada)";
        var rect = el.getBoundingClientRect();
        panel.style.top = rect.bottom + 4 + "px";
        panel.style.left = rect.left + "px";
        panel.style.minWidth = Math.max(rect.width, 320) + "px";
        panel.style.display = "block";
        _descPanelModalTrigger = el;
        textarea.focus();
    };

    window.cerrarDescPanelModal = function () {
        var panel = document.getElementById("desc-panel-modal");
        if (panel) panel.style.display = "none";
        _descPanelModalTrigger = null;
    };

    $(document).on("mousedown", function (e) {
        if (!_descPanelModalTrigger) return;
        var panel = document.getElementById("desc-panel-modal");
        if (!_descPanelModalTrigger.contains(e.target) && panel && !panel.contains(e.target))
            cerrarDescPanelModal();
    });

    // ── Historial ────────────────────────────────────────────────────────────
    function renderHistorialSteps(steps) {
        var done = 0, active = 0, cancelled = 0, completed = 0;
        steps.forEach(function (s) {
            if (s.state === "done") done++;
            else if (s.state === "active") active++;
            else if (s.state === "cancelled") cancelled++;
            else if (s.state === "completed") completed++;
        });

        var summaryEl = document.getElementById("historialSummary");
        if (cancelled > 0) {
            summaryEl.innerHTML =
                '<div class="summary-item"><div class="summary-num" style="color:#15803d">' + done + '</div><div class="summary-label">Registrados</div></div>' +
                '<div class="summary-item"><div class="summary-num" style="color:#b91c1c">1</div><div class="summary-label">Cancelada</div></div>' +
                '<div class="summary-item"><div class="summary-num" style="color:#64748b">' + (done + cancelled) + '</div><div class="summary-label">Total</div></div>';
        } else if (completed > 0) {
            summaryEl.innerHTML =
                '<div class="summary-item"><div class="summary-num" style="color:#15803d">' + done + '</div><div class="summary-label">Registrados</div></div>' +
                '<div class="summary-item"><div class="summary-num" style="color:#065f46">1</div><div class="summary-label">Finalizada</div></div>' +
                '<div class="summary-item"><div class="summary-num" style="color:#64748b">' + (done + completed) + '</div><div class="summary-label">Total</div></div>';
        } else {
            summaryEl.innerHTML =
                '<div class="summary-item"><div class="summary-num" style="color:#15803d">' + done + '</div><div class="summary-label">Completados</div></div>' +
                '<div class="summary-item"><div class="summary-num" style="color:#b45309">' + active + '</div><div class="summary-label">En proceso</div></div>' +
                '<div class="summary-item"><div class="summary-num" style="color:#64748b">' + (done + active) + '</div><div class="summary-label">Total</div></div>';
        }

        var mtl = document.getElementById("historialTl");
        mtl.innerHTML = "";
        steps.forEach(function (s) {
            var bCls, bTxt;
            switch (s.state) {
                case "done": bCls = "mbadge-done"; bTxt = "Completado"; break;
                case "active": bCls = "mbadge-active"; bTxt = "En curso"; break;
                case "completed": bCls = "mbadge-completed"; bTxt = "Finalizado"; break;
                case "cancelled": bCls = "mbadge-cancelled"; bTxt = "Cancelada"; break;
                default: bCls = "mbadge-pending"; bTxt = "Pendiente"; break;
            }
            var tStr = s.time !== "—" ? " · " + s.time : "";
            var item = document.createElement("div");
            item.className = "mtl-item " + s.state;
            item.innerHTML =
                '<div class="mtl-dot-col"><div class="mtl-dot"></div></div>' +
                '<div class="mtl-content">' +
                '<div class="mtl-dept">' + s.dept + '</div>' +
                '<div class="mtl-meta"><span class="mtl-badge ' + bCls + '">' + bTxt + '</span><span class="mtl-time">' + s.date + tStr + '</span></div>' +
                (s.comment ?
                    '<div class="mtl-detail">' +
                    '<div class="mtl-dr"><span class="dr-lbl">Responsable</span>' + s.by + '</div>' +
                    '<div class="mtl-dr"><span class="dr-lbl">Acción</span>' + s.action + '</div>' +
                    '<div class="mtl-dr"><span class="dr-lbl">Nota</span>' + s.comment + '</div>' +
                    '</div>' : '') +
                '</div>';
            mtl.appendChild(item);
        });
    }

    window.verHistorialTimeline = function (idRequi, numRequi) {
        document.getElementById("historialSubtitle").textContent = numRequi;
        document.getElementById("historialSummary").innerHTML =
            '<div style="text-align:center;color:#888;padding:1rem;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando historial…</div>';
        document.getElementById("historialTl").innerHTML = "";

        new bootstrap.Modal(document.getElementById("modalHistorial")).show();

        if (!urlObtenerProgreso) {
            document.getElementById("historialSummary").innerHTML =
                '<div style="color:#b91c1c;text-align:center;padding:1rem;">URL de progreso no configurada</div>';
            return;
        }

        $.get(urlObtenerProgreso, { idRequisicion: idRequi }, function (data) {
            var steps = (data || []).map(function (s) {
                return {
                    dept: s.dept || s.Dept || "",
                    date: s.date || s.Date || "—",
                    state: s.state || s.State || "pending",
                    by: s.by || s.By || "—",
                    time: s.time || s.Time || "—",
                    action: s.action || s.Action || "",
                    comment: s.comment || s.Comment || ""
                };
            });
            renderHistorialSteps(steps);
        }).fail(function () {
            document.getElementById("historialSummary").innerHTML =
                '<div style="color:#b91c1c;text-align:center;padding:1rem;"><i class="fa-solid fa-triangle-exclamation"></i> Error al cargar el historial</div>';
        });
    };

    if (container && window.TabsNotificacionesRequi) {
        window.TabsNotificacionesRequi.mount({
            container: container,
            onNovedadEnTabActivo: function () {
                aplicarPaginacion();
            }
        });
    }

    var tabsNavDaf = container ? container.querySelector(".almacen-tabs") : null;
    if (tabsNavDaf && container) {
        tabsNavDaf.addEventListener("click", function (e) {
            var btn = e.target.closest(".almacen-tabs-btn");
            if (!btn || !tabsNavDaf.contains(btn)) return;
            var tab = btn.getAttribute("data-tab");
            if (window.TabsNotificacionesRequi) {
                window.TabsNotificacionesRequi.limpiarBadgeNotificacionTab(btn);
            }
            tabsNavDaf.querySelectorAll(".almacen-tabs-btn").forEach(function (b) {
                b.classList.remove("activo");
            });
            btn.classList.add("activo");
            container.querySelectorAll(".almacen-tab-panel").forEach(function (p) {
                p.classList.remove("activo");
            });
            var panelDestinoClick = document.getElementById("tab-" + tab);
            if (panelDestinoClick) {
                panelDestinoClick.classList.add("activo");
            }
            aplicarPaginacion();
            window.requestAnimationFrame(function () {
                if (window.TabsNotificacionesRequi) {
                    window.TabsNotificacionesRequi.iniciarParpadeoFilasNuevasEnPanel(panelDestinoClick);
                }
            });
        });
    }

})();