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

  var modalEl = document.getElementById("modalDetalleDaf");
  var modal = modalEl ? new bootstrap.Modal(modalEl) : null;
  var detalleTitulo = document.getElementById("dafDetalleTitulo");
  var detalleSubtitulo = document.getElementById("dafDetalleSubtitulo");
  var detalleContenido = document.getElementById("dafDetalleContenido");
  var observacionesInput = document.getElementById("dafObservaciones");
  var accionesFooter = document.getElementById("dafAccionesFooter");
  var fechaSeleccionada = "";
  var tabActiva = "principal";
  var filasPorPagina = 10;
  var paginaPorTab = { principal: 1, autorizadas: 1, rechazadas: 1 };
  var contextoActual = { id: null, esConsolidada: false, estatusId: null, row: null };

  var tabConfig = {
    principal: {
      panel: document.getElementById("tab-principal"),
      tbody: document.getElementById("tbodyDafPrincipal"),
      paginacion: document.getElementById("paginacionDafPrincipal"),
      count: document.getElementById("countTabPrincipal"),
      emptyText: "No hay requisiciones pendientes en DAF"
    },
    autorizadas: {
      panel: document.getElementById("tab-autorizadas"),
      tbody: document.getElementById("tbodyDafAutorizadas"),
      paginacion: document.getElementById("paginacionDafAutorizadas"),
      count: document.getElementById("countTabAutorizadas"),
      emptyText: "No hay requisiciones autorizadas en DAF"
    },
    rechazadas: {
      panel: document.getElementById("tab-rechazadas"),
      tbody: document.getElementById("tbodyDafRechazadas"),
      paginacion: document.getElementById("paginacionDafRechazadas"),
      count: document.getElementById("countTabRechazadas"),
      emptyText: "No hay requisiciones rechazadas en DAF"
    }
  };

  function esc(value) {
    return String(value == null ? "" : value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function getConfig(tab) {
    return tabConfig[tab] || tabConfig.principal;
  }

  function getMainRows(tbody) {
    if (!tbody) return [];
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
    container.querySelectorAll(".fila-detalle.expanded").forEach(function (row) {
      row.classList.remove("expanded");
      row.classList.add("collapsed");
    });
  }

  function updateCount(tab) {
    var config = getConfig(tab);
    if (!config.count || !config.tbody) return;
    config.count.textContent = String(getMainRows(config.tbody).length);
  }

  function updateAllCounts() {
    Object.keys(tabConfig).forEach(updateCount);
  }

  function showEmptyRow(tab, show, message) {
    var config = getConfig(tab);
    if (!config.tbody) return;

    var existing = config.tbody.querySelector(".fila-vacia-daf");
    if (show) {
      if (!existing) {
        var tr = document.createElement("tr");
        tr.className = "fila-vacia fila-vacia-daf";
        tr.innerHTML = '<td colspan="8" class="text-center">' + esc(message || config.emptyText) + "</td>";
        config.tbody.appendChild(tr);
      } else if (message) {
        existing.innerHTML = '<td colspan="8" class="text-center">' + esc(message) + "</td>";
      }
    } else if (existing) {
      existing.remove();
    }
  }

  function renumberRows(tab) {
    var rows = getMainRows(getConfig(tab).tbody);
    rows.forEach(function (row, index) {
      var firstCell = row.querySelector("td:first-child");
      if (firstCell) firstCell.textContent = String(index + 1);
    });
  }

  function buildPaginationButton(tab, label, page, active) {
    var btn = document.createElement("button");
    btn.type = "button";
    btn.className = "almacen-paginacion-btn" + (active ? " activo" : "");
    btn.textContent = label;
    btn.addEventListener("click", function () {
      paginaPorTab[tab] = page;
      applyFilters();
    });
    return btn;
  }

  function applyPagination(tab, visibleRows) {
    var config = getConfig(tab);
    if (!config.paginacion) return;

    var totalPages = Math.max(1, Math.ceil(visibleRows.length / filasPorPagina));
    if (paginaPorTab[tab] > totalPages) paginaPorTab[tab] = totalPages;

    visibleRows.forEach(function (row, index) {
      var inPage =
        index >= (paginaPorTab[tab] - 1) * filasPorPagina &&
        index < paginaPorTab[tab] * filasPorPagina;
      row.style.display = inPage ? "" : "none";

      var detail = getDetailRow(row);
      if (detail) detail.style.display = inPage ? "" : "none";
    });

    config.paginacion.innerHTML = "";
    if (visibleRows.length <= filasPorPagina) return;

    for (var page = 1; page <= totalPages; page++) {
      config.paginacion.appendChild(buildPaginationButton(tab, String(page), page, page === paginaPorTab[tab]));
    }
  }

  function applyFilters() {
    var config = getConfig(tabActiva);
    if (!config.tbody) return;

    var folio = (document.getElementById("filtroNumReq") && document.getElementById("filtroNumReq").value || "").trim().toLowerCase();
    var depto = (document.getElementById("filtroDepartamento") && document.getElementById("filtroDepartamento").value || "").trim().toLowerCase();
    var estado = (document.getElementById("filtroEstado") && document.getElementById("filtroEstado").value || "").trim().toLowerCase();
    var rows = getMainRows(config.tbody);
    var visibles = [];

    rows.forEach(function (row) {
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
      if (ok) visibles.push(row);
    });

    showEmptyRow(tabActiva, visibles.length === 0, "Sin resultados para los filtros aplicados");
    applyPagination(tabActiva, visibles);
  }

  function activateTab(tab) {
    tabActiva = tab;
    collapseAll();

    container.querySelectorAll(".almacen-tabs-btn").forEach(function (btn) {
      btn.classList.toggle("activo", btn.getAttribute("data-tab") === tab);
    });

    container.querySelectorAll(".almacen-tab-panel").forEach(function (panel) {
      panel.classList.toggle("activo", panel.id === "tab-" + tab);
    });

    applyFilters();
  }

  function buildEstadoBadge(idEstatus, nombreEstatus) {
    var colorClass = "badge-estado-info";
    var iconClass = idEstatus === 1 ? "fa-solid fa-file-signature" : "fa-solid fa-spinner fa-spin-pulse";

    if (idEstatus === 7 || idEstatus === 12 || idEstatus === 17) {
      colorClass = "badge-estado-success";
      iconClass = "fa-solid fa-check-double";
    } else if (idEstatus === 4 || idEstatus === 10 || idEstatus === 15 || idEstatus === 16) {
      colorClass = "badge-estado-purple";
      iconClass = "fa-solid fa-user-check";
    } else if (idEstatus === 5 || idEstatus === 6) {
      colorClass = "badge-estado-danger";
      iconClass = "fa-solid fa-ban";
    } else if (idEstatus === 3 || idEstatus === 18) {
      colorClass = "badge-estado-warning";
      iconClass = "fa-solid fa-triangle-exclamation";
    }

    return '<div class="badge-estado-premium ' + colorClass + '" title="' + esc(nombreEstatus) + '">' +
      '<i class="' + iconClass + '"></i><span class="badge-text">' + esc(nombreEstatus) + "</span></div>";
  }

  function findRowByContext(id, esConsolidada) {
    var selector = esConsolidada
      ? 'tr.fila-requi[data-consolidada-id="' + id + '"]'
      : 'tr.fila-requi[data-requi-id="' + id + '"]';
    return container.querySelector(selector);
  }

  function updateModalActions() {
    var pendiente = Number(contextoActual.estatusId) === 16;
    if (accionesFooter) accionesFooter.style.display = pendiente ? "flex" : "none";
    if (observacionesInput) observacionesInput.disabled = !pendiente;
  }

  function moveRowToTab(row, destinationTab, idEstatus, nombreEstatus) {
    var detail = getDetailRow(row);
    var targetConfig = getConfig(destinationTab);
    if (!targetConfig.tbody) return;

    var estadoCell = row.querySelector(".celda-estado");
    if (estadoCell) estadoCell.innerHTML = buildEstadoBadge(idEstatus, nombreEstatus);
    row.setAttribute("data-estatus-id", String(idEstatus));
    row.dataset.match = "1";

    if (detail) {
      detail.classList.remove("expanded");
      detail.classList.add("collapsed");
      detail.style.display = "none";
    }

    targetConfig.tbody.appendChild(row);
    if (detail) targetConfig.tbody.appendChild(detail);

    ["principal", "autorizadas", "rechazadas"].forEach(function (tab) {
      renumberRows(tab);
      updateCount(tab);
      showEmptyRow(tab, getMainRows(getConfig(tab).tbody).length === 0, getConfig(tab).emptyText);
    });

    applyFilters();
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
    contextoActual.row = findRowByContext(id, esConsolidada);
    contextoActual.estatusId = contextoActual.row ? Number(contextoActual.row.getAttribute("data-estatus-id") || "0") : null;
    updateModalActions();
    if (observacionesInput) observacionesInput.value = "";
    if (!modal || !detalleContenido) return;

    detalleContenido.innerHTML =
      '<div style="text-align:center;padding:2rem;color:#888;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando...</div>';
    modal.show();

    var url = esConsolidada
      ? urlDetalleConsolidada + "?idConsolidada=" + encodeURIComponent(id)
      : urlObtenerDetalles + "?idMaestro=" + encodeURIComponent(id);

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
    var value = (observacionesInput && observacionesInput.value || "").trim();
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

      var tStr = s.time !== “—“ ? “ · “ + s.time : “”;
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

  var modalHistorial = null;

  function abrirModalHistorial(id, titulo, urlProgreso, urlPdf, queryParam, downloadParam) {
    try {
      var subtitle = document.getElementById("historialSubtitle");
      var summary = document.getElementById("historialSummary");
      var timeline = document.getElementById("historialTl");
      var historialModalEl = document.getElementById("modalHistorial");

      if (!subtitle || !summary || !timeline || !historialModalEl) return;

      subtitle.textContent = titulo || "Historial";
      configurarDescargaHistorial(id, urlPdf, downloadParam);
      summary.innerHTML =
        '<div style="text-align:center;color:#888;padding:1rem;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando historial...</div>';
      timeline.innerHTML = "";

      if (!modalHistorial) modalHistorial = new bootstrap.Modal(historialModalEl);
      modalHistorial.show();

      var payload = {};
      payload[queryParam] = id;

      $.ajax({
        url: urlProgreso,
        data: payload,
        method: "GET",
        dataType: "json",
        timeout: 30000,
        success: function (data) {
          var steps = (data || []).map(function (s) {
            return {
              dept: s.dept || s.Dept || "",
              date: s.date || s.Date || "â€”",
              state: s.state || s.State || "pending",
              by: s.by || s.By || "â€”",
              time: s.time || s.Time || "â€”",
              action: s.action || s.Action || "",
              comment: s.comment || s.Comment || "",
            };
          });

          if (steps.length === 0) {
            summary.innerHTML =
              '<div style="color:#b45309;text-align:center;padding:1rem;"><i class="fa-solid fa-info-circle"></i> No hay registros historicos para este elemento</div>';
            timeline.innerHTML = "";
            return;
          }

          renderHistorialSteps(steps);
        },
        error: function (xhr, textStatus) {
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
      console.error("abrirModalHistorial", e);
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
    if (!contextoActual.id || Number(contextoActual.estatusId) !== 16) return;

    var payload = contextoActual.esConsolidada
      ? { idConsolidada: contextoActual.id, observaciones: (observacionesInput && observacionesInput.value || "").trim() }
      : { idRequi: contextoActual.id, observaciones: (observacionesInput && observacionesInput.value || "").trim() };
    var url = contextoActual.esConsolidada ? urlAutorizarConsolidada : urlAutorizarRequi;

    postForm(url, payload)
      .then(function (res) {
        if (!res.success) throw new Error("No se pudo autorizar.");
        if (modal) modal.hide();
        if (contextoActual.row) moveRowToTab(contextoActual.row, "autorizadas", 17, "ENVIADA A PAGO");
        contextoActual.estatusId = 17;
        updateModalActions();
        Swal.fire({
          icon: "success",
          title: "Requisicion autorizada",
          text: "Se movio a la pestaña de autorizadas.",
          confirmButtonText: "Aceptar",
        });
      })
      .catch(function (err) {
        Swal.fire({ icon: "error", title: esc(err.message) });
      });
  };

  window.rechazarDaf = function () {
    if (!contextoActual.id || Number(contextoActual.estatusId) !== 16) return;
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
        if (contextoActual.row) moveRowToTab(contextoActual.row, "rechazadas", 5, "REQUISICION RECHAZADA");
        contextoActual.estatusId = 5;
        updateModalActions();
        Swal.fire({
          icon: "success",
          title: "Requisicion rechazada",
          text: "Se movio a la pestaña de rechazadas.",
          confirmButtonText: "Aceptar",
        });
      })
      .catch(function (err) {
        Swal.fire({ icon: "error", title: esc(err.message) });
      });
  };

  window.solicitarModificacionDaf = function () {
    if (!contextoActual.id || Number(contextoActual.estatusId) !== 16) return;
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
        if (contextoActual.row) {
          var detail = getDetailRow(contextoActual.row);
          contextoActual.row.remove();
          if (detail) detail.remove();
          renumberRows("principal");
          updateCount("principal");
          showEmptyRow("principal", getMainRows(getConfig("principal").tbody).length === 0, getConfig("principal").emptyText);
          applyFilters();
        }
        Swal.fire({
          icon: "success",
          title: "Solicitud de modificacion enviada",
          text: "La requisicion salio de la bandeja principal de DAF.",
          confirmButtonText: "Aceptar",
        });
      })
      .catch(function (err) {
        Swal.fire({ icon: "error", title: esc(err.message) });
      });
  };

  container.addEventListener("click", function (event) {
    var tabBtn = event.target.closest(".almacen-tabs-btn");
    if (tabBtn) {
      activateTab(tabBtn.getAttribute("data-tab"));
      return;
    }

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
    if (!event.target.closest("table.tabla-requisiciones")) collapseAll();
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
        paginaPorTab[tabActiva] = 1;
        applyFilters();
      },
    });
  }

  document.getElementById("btnLimpiarFecha") && document.getElementById("btnLimpiarFecha").addEventListener("click", function () {
    var input = document.getElementById("filtroFecha");
    if (input) input.value = "";
    fechaSeleccionada = "";
    this.style.display = "none";
    paginaPorTab[tabActiva] = 1;
    applyFilters();
  });

  ["filtroNumReq", "filtroDepartamento"].forEach(function (id) {
    var input = document.getElementById(id);
    if (!input) return;
    input.addEventListener("input", function () {
      paginaPorTab[tabActiva] = 1;
      applyFilters();
    });
  });

  var filtroEstado = document.getElementById("filtroEstado");
  if (filtroEstado) {
    filtroEstado.addEventListener("change", function () {
      paginaPorTab[tabActiva] = 1;
      applyFilters();
    });
  }

  if (filtroEstado && window.SelectRosaBuscable) {
    window.SelectRosaBuscable.destruir(filtroEstado);
    window.SelectRosaBuscable.inicializar(filtroEstado, {
      placeholder: "Buscar estatus...",
      defaultText: false,
    });
  }

  Object.keys(tabConfig).forEach(function (tab) {
    getMainRows(getConfig(tab).tbody).forEach(function (row) {
      row.dataset.match = "1";
    });
    showEmptyRow(tab, getMainRows(getConfig(tab).tbody).length === 0, getConfig(tab).emptyText);
  });

  updateAllCounts();
  activateTab("principal");
})();
