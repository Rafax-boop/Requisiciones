(function () {
  var container = document.querySelector(".tabla-requi-page");
  if (!container) return;

  var urlObtenerDetalles = container.getAttribute("data-url-obtener-detalles") || "/Requisicion/ObtenerDetalles";
  var urlDetalleConsolidada = container.getAttribute("data-url-detalle-consolidada") || "/Consolidada/ObtenerDetalleConsolidada";
  var urlObtenerProgreso = container.getAttribute("data-url-obtener-progreso") || "/Requisicion/ObtenerProgresoRequisicion";
  var urlObtenerProgresoConsolidada = container.getAttribute("data-url-obtener-progreso-consolidada") || "/DAF/ObtenerProgresoConsolidada";
  var urlHistorialPdfMateriales = container.getAttribute("data-url-historial-pdf-materiales") || "/Requisicion/HistorialParaPdf";
  var urlHistorialPdfServicios = container.getAttribute("data-url-historial-pdf-servicios") || "/Servicios/HistorialParaPdf";
  var urlHistorialPdfConsolidada = container.getAttribute("data-url-historial-pdf-consolidada") || "/DAF/HistorialConsolidadaParaPdf";
  var urlObtenerTodosArchivos = container.getAttribute("data-url-obtener-todos-archivos") || "";
  var urlDescargarTodosArchivosZip = container.getAttribute("data-url-descargar-todos-archivos-zip") || "";
  var urlObtenerArchivosConsolidada = container.getAttribute("data-url-obtener-archivos-consolidada") || "";
  var urlDescargarArchivosConsolidadaZip = container.getAttribute("data-url-descargar-archivos-consolidada-zip") || "";
  var urlAutorizarRequi = container.getAttribute("data-url-autorizar-requi") || "";
  var urlAutorizarConsolidada = container.getAttribute("data-url-autorizar-consolidada") || "";
  var urlRechazarRequi = container.getAttribute("data-url-rechazar-requi") || "";
  var urlRechazarConsolidada = container.getAttribute("data-url-rechazar-consolidada") || "";
  var urlModificarRequi = container.getAttribute("data-url-modificar-requi") || "";
  var urlModificarConsolidada = container.getAttribute("data-url-modificar-consolidada") || "";

  var tbody = document.getElementById("tbodyDaf");
  var paginacion = document.getElementById("paginacionDaf");
  var modalEl = document.getElementById("modalDetalleDaf");
  var modal = modalEl ? new bootstrap.Modal(modalEl) : null;
  var detalleTitulo = document.getElementById("dafDetalleTitulo");
  var detalleSubtitulo = document.getElementById("dafDetalleSubtitulo");
  var detalleContenido = document.getElementById("dafDetalleContenido");
  var observacionesInput = document.getElementById("dafObservaciones");
  var contextoActual = { id: null, esConsolidada: false };
  var fechaSeleccionada = "";
  var paginaActual = 1;
  var filasPorPagina = 10;

  function esc(value) {
    return String(value ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function getMainRows() {
    return Array.prototype.slice.call(tbody.querySelectorAll("tr.fila-requi"));
  }

  function getDetailRow(row) {
    return row && row.nextElementSibling && row.nextElementSibling.classList.contains("fila-detalle")
      ? row.nextElementSibling
      : null;
  }

  function getRowField(row, selector) {
    var cell = row.querySelector(selector);
    return cell ? cell.textContent.trim().toLowerCase() : "";
  }

  function parseDateText(text) {
    if (!text) return "";
    var value = text.trim();
    if (/^\d{2}\/\d{2}\/\d{4}$/.test(value)) return value;
    if (/^\d{1,2}\/\d{1,2}\/\d{4}$/.test(value)) {
      var parts = value.split("/");
      return parts[0].padStart(2, "0") + "/" + parts[1].padStart(2, "0") + "/" + parts[2];
    }
    return value;
  }

  function collapseAll() {
    tbody.querySelectorAll(".fila-detalle.expanded").forEach(function (row) {
      row.classList.remove("expanded");
      row.classList.add("collapsed");
    });
  }

  function showEmptyRow(show) {
    var existing = tbody.querySelector(".fila-vacia-daf");
    if (show) {
      if (!existing) {
        var tr = document.createElement("tr");
        tr.className = "fila-vacia fila-vacia-daf";
        tr.innerHTML = '<td colspan="8" class="text-center">Sin resultados para los filtros aplicados</td>';
        tbody.appendChild(tr);
      }
    } else if (existing) {
      existing.remove();
    }
  }

  function buildPaginationButton(label, page, active) {
    var btn = document.createElement("button");
    btn.type = "button";
    btn.className = "almacen-paginacion-btn" + (active ? " activo" : "");
    btn.textContent = label;
    btn.addEventListener("click", function () {
      paginaActual = page;
      applyPagination();
    });
    return btn;
  }

  function applyPagination() {
    if (!paginacion) return;

    var rows = getMainRows().filter(function (row) {
      return row.dataset.match !== "0" && row.style.display !== "none";
    });
    var totalPages = Math.max(1, Math.ceil(rows.length / filasPorPagina));
    if (paginaActual > totalPages) paginaActual = totalPages;

    getMainRows().forEach(function (row) {
      var detail = getDetailRow(row);
      row.style.display = row.dataset.match === "0" ? "none" : "";
      if (detail) detail.style.display = row.dataset.match === "0" ? "none" : "";
    });

    rows.forEach(function (row, index) {
      var inPage =
        index >= (paginaActual - 1) * filasPorPagina &&
        index < paginaActual * filasPorPagina;
      row.style.display = inPage ? "" : "none";

      var detail = getDetailRow(row);
      if (detail) {
        detail.style.display = inPage ? "" : "none";
      }
    });

    paginacion.innerHTML = "";
    if (rows.length <= filasPorPagina) return;

    for (var page = 1; page <= totalPages; page++) {
      paginacion.appendChild(buildPaginationButton(String(page), page, page === paginaActual));
    }
  }

  function applyFilters() {
    var folio = (document.getElementById("filtroNumReq")?.value || "").trim().toLowerCase();
    var depto = (document.getElementById("filtroDepartamento")?.value || "").trim().toLowerCase();
    var estado = (document.getElementById("filtroEstado")?.value || "").trim().toLowerCase();
    var visibles = 0;

    getMainRows().forEach(function (row) {
      var detail = getDetailRow(row);
      var rowFolio = getRowField(row, "td:nth-child(2)");
      var rowDepto = getRowField(row, "td:nth-child(4)");
      var rowEstado = getRowField(row, "td:nth-child(7)");
      var rowFecha = parseDateText(getRowField(row, "td:nth-child(3)"));

      var ok =
        (!folio || rowFolio.indexOf(folio) >= 0) &&
        (!depto || rowDepto.indexOf(depto) >= 0) &&
        (!estado || rowEstado.indexOf(estado) >= 0) &&
        (!fechaSeleccionada || rowFecha === fechaSeleccionada);

      row.dataset.match = ok ? "1" : "0";
      row.style.display = ok ? "" : "none";
      if (detail) detail.style.display = ok ? "" : "none";
      if (ok) visibles++;
    });

    showEmptyRow(visibles === 0);
    paginaActual = 1;
    applyPagination();
  }

  function renderArticuloRow(item, includeRequi) {
    var detalleRaw = item.descripcionDetallada || "";
    var detalle = esc(detalleRaw);
    return (
      "<tr>" +
      (includeRequi ? "<td>" + esc(item.numRequi || "") + "</td>" : "") +
      "<td>" + esc(item.numPartida || "") + "</td>" +
      "<td>" + esc(item.cantidad || "") + "</td>" +
      "<td>" + esc(item.unidadMedida || "") + "</td>" +
      "<td>" + esc(item.descripcion || "") + "</td>" +
      '<td><div class="desc-preview-modal" data-full="' +
      detalle +
      '" onclick="window.verDescDetalleModal && window.verDescDetalleModal(this)">' +
      '<span class="desc-texto-preview' +
      (detalle ? " tiene-texto" : "") +
      '">' +
      esc(detalleRaw.length > 40 ? detalleRaw.substring(0, 40) + "..." : detalleRaw || "Sin descripcion") +
      '</span><i class="fa-solid fa-eye desc-icon"></i></div></td>' +
      "</tr>"
    );
  }

  function renderRequisicionDetalle(data) {
    var articulos = data.articulos || [];
    var rows = articulos.length
      ? articulos.map(function (item) { return renderArticuloRow(item, false); }).join("")
      : '<tr class="fila-vacia"><td colspan="5" class="text-center">Sin articulos</td></tr>';

    detalleTitulo.textContent = "Detalle de requisicion";
    detalleSubtitulo.textContent = "Vista previa de partidas";
    detalleContenido.innerHTML =
      '<div class="table-responsive-container">' +
      '<table class="tabla-requisiciones">' +
      "<thead><tr>" +
      "<th>No. Partida</th><th>Cantidad</th><th>Unidad</th><th>Descripcion</th><th>Descripcion Detallada</th>" +
      "</tr></thead>" +
      "<tbody>" + rows + "</tbody></table></div>";
  }

  function renderConsolidadaDetalle(data) {
    var hijas = data.requisiciones || [];
    var articulos = data.articulos || [];
    var cards = hijas.length
      ? hijas.map(function (item) {
          return (
            '<div style="padding:14px 16px;border:1px solid var(--color-border-tertiary);border-radius:12px;background:var(--color-background-secondary);min-width:220px;flex:1 1 240px;">' +
            '<div style="font-weight:700;color:var(--slate-700);margin-bottom:6px;">' + esc(item.numRequi || "") + "</div>" +
            '<div style="font-size:13px;color:var(--color-text-primary);margin-bottom:4px;"><i class="fa-solid fa-building"></i> ' + esc(item.departamento || "") + "</div>" +
            '<div style="font-size:13px;color:var(--color-text-primary);"><i class="fa-solid fa-boxes-stacked"></i> ' + esc(item.cantidadPartidas || 0) + " partidas</div>" +
            "</div>"
          );
        }).join("")
      : '<p style="color:#888;font-style:italic;">Sin requisiciones hijas</p>';

    var rows = articulos.length
      ? articulos.map(function (item) { return renderArticuloRow(item, true); }).join("")
      : '<tr class="fila-vacia"><td colspan="6" class="text-center">Sin articulos</td></tr>';

    detalleTitulo.textContent = "Detalle de consolidada";
    detalleSubtitulo.textContent = data.folioConsolidada || "Consolidada";
    detalleContenido.innerHTML =
      '<div style="margin-bottom:18px;">' +
      '<p style="font-size:13px;font-weight:700;color:var(--slate-700);margin-bottom:10px;"><i class="fa-solid fa-layer-group"></i> Requisiciones agrupadas</p>' +
      '<div style="display:flex;flex-wrap:wrap;gap:10px;">' + cards + "</div>" +
      "</div>" +
      '<div class="table-responsive-container">' +
      '<table class="tabla-requisiciones">' +
      "<thead><tr>" +
      "<th>Requisicion</th><th>No. Partida</th><th>Cantidad</th><th>Unidad</th><th>Descripcion</th><th>Descripcion Detallada</th>" +
      "</tr></thead>" +
      "<tbody>" + rows + "</tbody></table></div>";
  }

  function openDetalle(id, esConsolidada) {
    contextoActual.id = id;
    contextoActual.esConsolidada = esConsolidada;
    if (observacionesInput) observacionesInput.value = "";
    if (!modal || !detalleContenido) return;

    detalleContenido.innerHTML =
      '<div style="text-align:center;padding:2rem;color:#888;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando...</div>';
    modal.show();

    var url = esConsolidada
      ? urlDetalleConsolidada + "?idConsolidada=" + encodeURIComponent(id)
      : urlObtenerDetalles + "?idMaestro=" + encodeURIComponent(id) + "&soloCompra=true";

    fetch(url, { credentials: "same-origin" })
      .then(function (r) {
        if (!r.ok) throw new Error("No se pudo cargar el detalle.");
        return r.json();
      })
      .then(function (data) {
        if (esConsolidada) renderConsolidadaDetalle(data);
        else renderRequisicionDetalle(data);
      })
      .catch(function (err) {
        detalleContenido.innerHTML =
          '<div style="text-align:center;padding:2rem;color:#b91c1c;"><i class="fa-solid fa-circle-exclamation"></i> ' +
          esc(err.message) +
          "</div>";
      });
  }

  function postForm(url, payload) {
    return fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
      },
      body: new URLSearchParams(payload),
      credentials: "same-origin",
    }).then(function (r) {
      if (!r.ok) throw new Error("La solicitud no pudo completarse.");
      return r.json();
    });
  }

  function requireText(message) {
    var value = (observacionesInput?.value || "").trim();
    if (!value) {
      Swal.fire({
        icon: "warning",
        title: message,
        confirmButtonText: "Aceptar",
      });
      return null;
    }
    return value;
  }

  function configurarDescargaHistorial(id, urlBase, paramName) {
    var btn = document.getElementById("btnDescargarHistorialPdf");
    if (!btn) return;

    if (!urlBase || !id) {
      btn.setAttribute("href", "#");
      btn.classList.add("disabled");
      btn.setAttribute("aria-disabled", "true");
      return;
    }

    var separador = urlBase.indexOf("?") >= 0 ? "&" : "?";
    btn.setAttribute("href", urlBase + separador + paramName + "=" + encodeURIComponent(id));
    btn.classList.remove("disabled");
    btn.removeAttribute("aria-disabled");
  }

  function renderHistorialSteps(steps) {
    var done = 0;
    var active = 0;
    var cancelled = 0;
    var completed = 0;

    steps.forEach(function (s) {
      if (s.state === "done") done++;
      else if (s.state === "active") active++;
      else if (s.state === "cancelled") cancelled++;
      else if (s.state === "completed") completed++;
    });

    var summaryEl = document.getElementById("historialSummary");
    if (!summaryEl) return;

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
    if (!mtl) return;
    mtl.innerHTML = "";

    steps.forEach(function (s) {
      var badgeClass = "mbadge-pending";
      var badgeText = "Pendiente";

      switch (s.state) {
        case "done":
          badgeClass = "mbadge-done";
          badgeText = "Completado";
          break;
        case "active":
          badgeClass = "mbadge-active";
          badgeText = "En curso";
          break;
        case "completed":
          badgeClass = "mbadge-completed";
          badgeText = "Finalizado";
          break;
        case "cancelled":
          badgeClass = "mbadge-cancelled";
          badgeText = "Cancelada";
          break;
      }

      var tStr = s.time !== "—" ? " · " + s.time : "";
      var item = document.createElement("div");
      item.className = "mtl-item " + s.state;
      item.innerHTML =
        '<div class="mtl-dot-col"><div class="mtl-dot"></div></div>' +
        '<div class="mtl-content">' +
        '<div class="mtl-dept">' + esc(s.dept) + "</div>" +
        '<div class="mtl-meta">' +
        '<span class="mtl-badge ' + badgeClass + '">' + badgeText + "</span>" +
        '<span class="mtl-time">' + esc(s.date) + esc(tStr) + "</span>" +
        "</div>" +
        (s.comment
          ? '<div class="mtl-detail">' +
            '<div class="mtl-dr"><span class="dr-lbl">Responsable</span>' + esc(s.by) + "</div>" +
            '<div class="mtl-dr"><span class="dr-lbl">Accion</span>' + esc(s.action) + "</div>" +
            '<div class="mtl-dr"><span class="dr-lbl">Nota</span>' + esc(s.comment) + "</div>" +
            "</div>"
          : "") +
        "</div>";
      mtl.appendChild(item);
    });
  }

  var _modalHistorialInstance = null;

  function abrirModalHistorial(id, titulo, urlProgreso, urlPdf, queryParam, downloadParam) {
    try {
      var subtitle = document.getElementById("historialSubtitle");
      var summary = document.getElementById("historialSummary");
      var timeline = document.getElementById("historialTl");
      var historialModalEl = document.getElementById("modalHistorial");

      if (!subtitle || !summary || !timeline || !historialModalEl) {
        console.error("abrirModalHistorial: elementos del modal no encontrados", { subtitle: !!subtitle, summary: !!summary, timeline: !!timeline, modal: !!historialModalEl });
        return;
      }

      subtitle.textContent = titulo || "Historial";
      configurarDescargaHistorial(id, urlPdf, downloadParam);
      summary.innerHTML =
        '<div style="text-align:center;color:#888;padding:1rem;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando historial...</div>';
      timeline.innerHTML = "";

      if (!urlProgreso) {
        summary.innerHTML =
          '<div style="color:#b91c1c;text-align:center;padding:1rem;">URL de progreso no configurada</div>';
        console.error("abrirModalHistorial: urlProgreso vacio o nulo");
        return;
      }

      if (!_modalHistorialInstance) {
        _modalHistorialInstance = new bootstrap.Modal(historialModalEl);
      }
      _modalHistorialInstance.show();

      var payload = {};
      payload[queryParam] = id;

      $.ajax({
        url: urlProgreso,
        data: payload,
        method: "GET",
        dataType: "json",
        timeout: 30000,
        success: function (data) {
          try {
            var steps = (data || []).map(function (s) {
              return {
                dept: s.dept || s.Dept || "",
                date: s.date || s.Date || "—",
                state: s.state || s.State || "pending",
                by: s.by || s.By || "—",
                time: s.time || s.Time || "—",
                action: s.action || s.Action || "",
                comment: s.comment || s.Comment || "",
              };
            });
            if (steps.length === 0) {
              summary.innerHTML =
                '<div style="color:#b45309;text-align:center;padding:1rem;"><i class="fa-solid fa-info-circle"></i> No hay registros historicos para esta consolidada</div>';
              timeline.innerHTML = "";
              return;
            }
            renderHistorialSteps(steps);
          } catch (e) {
            console.error("abrirModalHistorial: error al procesar datos", e);
            summary.innerHTML =
              '<div style="color:#b91c1c;text-align:center;padding:1rem;"><i class="fa-solid fa-triangle-exclamation"></i> Error al procesar el historial: ' +
              esc(e.message) +
              "</div>";
          }
        },
        error: function (xhr, textStatus, errorThrown) {
          console.error("abrirModalHistorial: fallo la peticion", { status: xhr.status, statusText: textStatus, error: errorThrown, responseText: xhr.responseText });
          var detalle =
            (xhr.responseJSON && xhr.responseJSON.message) ||
            (xhr.status ? "Error " + xhr.status + " (" + textStatus + ")" : "Error al cargar el historial");
          summary.innerHTML =
            '<div style="color:#b91c1c;text-align:center;padding:1rem;"><i class="fa-solid fa-triangle-exclamation"></i> ' +
            esc(detalle) +
            "</div>";
        }
      });
    } catch (e) {
      console.error("abrirModalHistorial: error inesperado", e);
    }
  }

  window.verDetalleDaf = function (id, esConsolidada) {
    openDetalle(id, esConsolidada);
  };

  window.verDocumentosDaf = function (id, esConsolidada) {
    if (!window.DocumentosRequisicionModal) return;

    window.DocumentosRequisicionModal.open(
      esConsolidada
        ? {
            idConsolidada: id,
            fetchUrl: urlObtenerArchivosConsolidada,
            downloadZipUrl: urlDescargarArchivosConsolidadaZip,
          }
        : {
            idRequisicion: id,
            fetchUrl: urlObtenerTodosArchivos,
            downloadZipUrl: urlDescargarTodosArchivosZip,
          }
    );
  };

  window.verHistorialTimeline = function (idRequi, numRequi, esServicio) {
    abrirModalHistorial(
      idRequi,
      numRequi,
      urlObtenerProgreso,
      esServicio === true || esServicio === "true" ? urlHistorialPdfServicios : urlHistorialPdfMateriales,
      "idRequisicion",
      "id"
    );
  };

  window.verHistorialConsolidadaTimeline = function (idConsolidada, folioConsolidada) {
    abrirModalHistorial(
      idConsolidada,
      folioConsolidada,
      urlObtenerProgresoConsolidada,
      urlHistorialPdfConsolidada,
      "idConsolidada",
      "idConsolidada"
    );
  };

  window.autorizarDaf = function () {
    if (!contextoActual.id) return;

    var payload = contextoActual.esConsolidada
      ? { idConsolidada: contextoActual.id, observaciones: (observacionesInput?.value || "").trim() }
      : { idRequi: contextoActual.id, observaciones: (observacionesInput?.value || "").trim() };
    var url = contextoActual.esConsolidada ? urlAutorizarConsolidada : urlAutorizarRequi;

    postForm(url, payload)
      .then(function (res) {
        if (!res.success) throw new Error("No se pudo autorizar.");
        if (modal) modal.hide();
        Swal.fire({
          icon: "success",
          title: "Aprobada",
          confirmButtonText: "Aceptar",
        }).then(function () {
          location.reload();
        });
      })
      .catch(function (err) {
        Swal.fire({ icon: "error", title: esc(err.message) });
      });
  };

  window.rechazarDaf = function () {
    if (!contextoActual.id) return;
    var texto = requireText("Escribe el motivo del rechazo");
    if (!texto) return;

    var payload = contextoActual.esConsolidada
      ? { idConsolidada: contextoActual.id, motivo: texto }
      : { idRequi: contextoActual.id, motivo: texto };
    var url = contextoActual.esConsolidada ? urlRechazarConsolidada : urlRechazarRequi;

    postForm(url, payload)
      .then(function (res) {
        if (!res.success) throw new Error("No se pudo rechazar.");
        if (modal) modal.hide();
        Swal.fire({
          icon: "success",
          title: contextoActual.esConsolidada ? "Consolidada rechazada" : "Requisicion rechazada",
          confirmButtonText: "Aceptar",
        }).then(function () {
          location.reload();
        });
      })
      .catch(function (err) {
        Swal.fire({ icon: "error", title: esc(err.message) });
      });
  };

  window.solicitarModificacionDaf = function () {
    if (!contextoActual.id) return;
    var texto = requireText("Escribe la observacion para la modificacion");
    if (!texto) return;

    var payload = contextoActual.esConsolidada
      ? { idConsolidada: contextoActual.id, observaciones: texto }
      : { idRequi: contextoActual.id, observaciones: texto };
    var url = contextoActual.esConsolidada ? urlModificarConsolidada : urlModificarRequi;

    postForm(url, payload)
      .then(function (res) {
        if (!res.success) throw new Error("No se pudo solicitar la modificacion.");
        if (modal) modal.hide();
        Swal.fire({
          icon: "success",
          title: "Solicitud de modificacion enviada",
          text: "La requisicion regreso a estatus 18.",
          confirmButtonText: "Aceptar",
        }).then(function () {
          location.reload();
        });
      })
      .catch(function (err) {
        Swal.fire({ icon: "error", title: esc(err.message) });
      });
  };

  container.addEventListener("click", function (event) {
    var row = event.target.closest("tr.fila-requi");
    if (!row || event.target.closest(".acciones-grupo, button, a")) return;

    var detail = getDetailRow(row);
    if (!detail) return;

    var expanding = !detail.classList.contains("expanded");
    collapseAll();

    if (expanding) {
      detail.classList.remove("collapsed");
      detail.classList.add("expanded");
      if (row.style.display !== "none") detail.style.display = "";
    }

  });

  document.addEventListener("click", function (event) {
    if (!event.target.closest("table.tabla-requisiciones")) {
      collapseAll();
    }
  });

  if (window.flatpickr) {
    flatpickr("#filtroFecha", {
      locale: "es",
      dateFormat: "d/m/Y",
      allowInput: false,
      onChange: function (selectedDates, dateStr) {
        fechaSeleccionada = dateStr || "";
        var btn = document.getElementById("btnLimpiarFecha");
        if (btn) btn.style.display = fechaSeleccionada ? "inline-flex" : "none";
        applyFilters();
      },
    });
  }

  document.getElementById("btnLimpiarFecha")?.addEventListener("click", function () {
    var input = document.getElementById("filtroFecha");
    if (input) input.value = "";
    fechaSeleccionada = "";
    this.style.display = "none";
    applyFilters();
  });

  ["filtroNumReq", "filtroDepartamento"].forEach(function (id) {
    document.getElementById(id)?.addEventListener("input", applyFilters);
  });
  document.getElementById("filtroEstado")?.addEventListener("change", applyFilters);

  var filtroEstado = document.getElementById("filtroEstado");
  if (filtroEstado && window.SelectRosaBuscable) {
    window.SelectRosaBuscable.destruir(filtroEstado);
    window.SelectRosaBuscable.inicializar(filtroEstado, {
      placeholder: "Buscar estatus...",
      defaultText: false,
    });
  }

  getMainRows().forEach(function (row) {
    row.dataset.match = "1";
  });
  applyFilters();
})();
