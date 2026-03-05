
(function () {
  var container = document.querySelector(".tabla-requi-page");
  var obtenerDetallesUrl = container
    ? container.getAttribute("data-url-obtener-detalles")
    : "";
  var verPdfUrl = container ? container.getAttribute("data-url-ver-pdf") : "";
  var urlUsuariosMateriales = container
    ? container.getAttribute("data-url-usuarios-materiales")
    : "";
  var urlAsignar = container ? container.getAttribute("data-url-asignar") : "";
  const contenedor = document.querySelector(".tabla-requi-page");
  const atenderUrl = contenedor.dataset.urlAtender;
  var urlBuscarCogs = container ? container.getAttribute("data-url-buscar-cogs") : "";
  var _idRequiAsignar = null;

  document.addEventListener("click", function (e) {
    if (!e.target.closest(".filtro-dropdown")) {
      document
        .querySelectorAll(".filtro-dropdown")
        .forEach((d) => d.classList.remove("open"));
    }
  });

  var fechaSeleccionada = "";

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
    },
  });

  window.limpiarFecha = function () {
    fpInstance.clear();
    fechaSeleccionada = "";
    var btnLimpiar = document.getElementById("btnLimpiarFecha");
    if (btnLimpiar) btnLimpiar.style.display = "none";
    filtrarTabla();
  };

  const TAMANO_PAGINA_REQUISICIONES = 7;
  let paginaRequisicionActual = 1;
  let paginacionRequisicionesContainer = null;

  var modoTabs =
    container &&
    container.querySelectorAll(".almacen-tabs-btn").length > 0 &&
    container.querySelectorAll(".almacen-tab-panel").length > 0;
  var paginaPorTab = { principal: 1, autorizadas: 1 };

  function getActiveTableContext() {
    if (!modoTabs || !container) {
      var tabla = document.querySelector(
        ".tabla-requisiciones:not(#tablaModalDetalle)",
      );
      var pag = document.getElementById("paginacionRequisiciones");
      return {
        tbody: tabla ? tabla.querySelector("tbody") : null,
        paginationContainer: pag,
        tabKey: "principal",
      };
    }
    var panel = container.querySelector(".almacen-tab-panel.activo");
    if (!panel) return null;
    var tabla = panel.querySelector(".tabla-requisiciones");
    var pag = panel.querySelector(".almacen-paginacion");
    var tabKey =
      panel.id === "tab-principal"
        ? "principal"
        : panel.id === "tab-autorizadas"
          ? "autorizadas"
          : "principal";
    return {
      tbody: tabla ? tabla.querySelector("tbody") : null,
      paginationContainer: pag,
      tabKey: tabKey,
    };
  }

  function filtrarTabla() {
    var ctx = getActiveTableContext();
    if (ctx) paginaPorTab[ctx.tabKey] = 1;
    paginaRequisicionActual = 1;
    aplicarPaginacionRequisiciones();
  }

  function aplicarPaginacionRequisiciones() {
    var ctx = getActiveTableContext();
    if (!ctx || !ctx.tbody) return;

    paginacionRequisicionesContainer = ctx.paginationContainer;
    paginaRequisicionActual = paginaPorTab[ctx.tabKey] || 1;

    var textoNumReq = (
      (document.getElementById("filtroNumReq") &&
        document.getElementById("filtroNumReq").value) ||
      ""
    )
      .toLowerCase()
      .trim();
    var textoDepto = (
      (document.getElementById("filtroDepartamento") &&
        document.getElementById("filtroDepartamento").value) ||
      ""
    )
      .toLowerCase()
      .trim();

    var todasLasFilas = [].slice.call(ctx.tbody.querySelectorAll("tr"));
    var filasVisibles = [];

    todasLasFilas.forEach(function (fila) {
      if (fila.classList.contains("fila-vacia")) return;
      var celdas = fila.querySelectorAll("td");
      if (!celdas.length) return;

      var folio = (celdas[1] && celdas[1].textContent.toLowerCase()) || "";
      var fecha = (celdas[2] && celdas[2].textContent.trim()) || "";
      var depto = (celdas[3] && celdas[3].textContent.toLowerCase()) || "";

      var pasaNumReq = !textoNumReq || folio.indexOf(textoNumReq) !== -1;
      var pasaFecha = !fechaSeleccionada || fecha === fechaSeleccionada;
      var pasaDepto = !textoDepto || depto.indexOf(textoDepto) !== -1;

      if (pasaNumReq && pasaFecha && pasaDepto) {
        filasVisibles.push(fila);
      } else {
        fila.style.display = "none";
      }
    });

    const total = filasVisibles.length;
    const totalPaginas = Math.max(
      1,
      Math.ceil(total / TAMANO_PAGINA_REQUISICIONES),
    );
    if (paginaRequisicionActual > totalPaginas)
      paginaRequisicionActual = totalPaginas;
    paginaPorTab[ctx.tabKey] = paginaRequisicionActual;
    const inicio = (paginaRequisicionActual - 1) * TAMANO_PAGINA_REQUISICIONES;
    const fin = inicio + TAMANO_PAGINA_REQUISICIONES;

    filasVisibles.forEach(function (fila, i) {
      fila.style.display = i >= inicio && i < fin ? "" : "none";
    });

    mostrarMensajeVacio(total, ctx.tbody);
    renderizarControlesPaginacion(total, inicio, fin, totalPaginas);
  }

  function renderizarControlesPaginacion(total, inicio, fin, totalPaginas) {
    if (!paginacionRequisicionesContainer) return;

    if (total === 0) {
      paginacionRequisicionesContainer.innerHTML = "";
      return;
    }

    var resFinal = Math.min(fin, total);
    var info =
      "Mostrando " +
      (inicio + 1) +
      "-" +
      resFinal +
      " de " +
      total +
      " requisiciones";
    var html = '<div class="almacen-paginacion-info">' + info + "</div>";
    html += '<div class="almacen-paginacion-btns">';
    html +=
      '<button type="button" class="almacen-paginacion-btn" data-pagina="prev" ' +
      (paginaRequisicionActual <= 1 ? "disabled" : "") +
      ">Anterior</button>";
    html += ' <span class="almacen-paginacion-nums">';

    var PRIMEROS = 3,
      ULTIMOS = 3;
    var actual = paginaRequisicionActual;
    var set = {};
    for (var i = 1; i <= Math.min(PRIMEROS, totalPaginas); i++) set[i] = true;
    if (actual > 0 && actual <= totalPaginas) {
      set[actual] = true;
      if (actual - 1 >= 1) set[actual - 1] = true;
      if (actual + 1 <= totalPaginas) set[actual + 1] = true;
    }
    for (
      var j = Math.max(1, totalPaginas - ULTIMOS + 1);
      j <= totalPaginas;
      j++
    )
      set[j] = true;
    var nums = Object.keys(set)
      .map(Number)
      .sort(function (a, b) {
        return a - b;
      });
    var prev = 0;
    for (var n = 0; n < nums.length; n++) {
      var p = nums[n];
      if (prev !== 0 && p > prev + 1)
        html += '<span class="almacen-paginacion-ellipsis">…</span>';
      html +=
        '<button type="button" class="almacen-paginacion-btn almacen-paginacion-num ' +
        (p === paginaRequisicionActual ? "activo" : "") +
        '" data-pagina="' +
        p +
        '">' +
        p +
        "</button>";
      prev = p;
    }
    html += "</span> ";
    html +=
      '<button type="button" class="almacen-paginacion-btn" data-pagina="next" ' +
      (paginaRequisicionActual >= totalPaginas ? "disabled" : "") +
      ">Siguiente</button>";
    html += "</div>";

    paginacionRequisicionesContainer.innerHTML = html;

    var botones = paginacionRequisicionesContainer.querySelectorAll(
      ".almacen-paginacion-btn",
    );
    var ctxPag = getActiveTableContext();
    for (var k = 0; k < botones.length; k++) {
      botones[k].addEventListener("click", function () {
        if (this.disabled) return;
        var pg = this.getAttribute("data-pagina");
        if (pg === "prev") {
          paginaRequisicionActual = Math.max(1, paginaRequisicionActual - 1);
        } else if (pg === "next") {
          paginaRequisicionActual = Math.min(
            totalPaginas,
            paginaRequisicionActual + 1,
          );
        } else {
          paginaRequisicionActual = parseInt(pg, 10);
        }
        if (ctxPag) paginaPorTab[ctxPag.tabKey] = paginaRequisicionActual;
        aplicarPaginacionRequisiciones();
      });
    }
  }

  window.abrirModalAsignar = function (idRequi) {
    _idRequiAsignar = idRequi;
    var select = document.getElementById("selectUsuarioAsignar");
    var $select = $("#selectUsuarioAsignar");
    if ($select.data("select2")) $select.select2("destroy");
    select.innerHTML = '<option value="">Cargando...</option>';

    $.get(urlUsuariosMateriales, function (data) {
      select.innerHTML =
        '<option value="">-- Seleccionar responsable --</option>';
      data.forEach(function (u) {
        select.innerHTML +=
          '<option value="' + u.id + '">' + u.nombre + "</option>";
      });
      $select.select2({
        language: "es",
        placeholder: "-- Seleccionar responsable --",
        allowClear: false,
        minimumResultsForSearch: Infinity,
        width: "100%"
      });
    });

    var modal = new bootstrap.Modal(document.getElementById("modalAsignar"));
    modal.show();
  };

  window.confirmarAsignacion = function () {
    var idUsuario = $("#selectUsuarioAsignar").val();
    if (!idUsuario) {
      Swal.fire({
        icon: "warning",
        title: "Selecciona un responsable",
        confirmButtonText: "Ok",
      });
      return;
    }

    $.post(
      urlAsignar,
      { idRequi: _idRequiAsignar, idUsuario: idUsuario },
      function (res) {
        if (res.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalAsignar"),
          ).hide();
          Swal.fire({
            icon: "success",
            title: "Asignado correctamente",
            confirmButtonColor: "#fe6291",
            confirmButtonText: "Aceptar",
            fontfamily: "Plus Jakarta Sans",
            timer: 3500,
            timerProgressBar: true,
          }).then(() => location.reload());
        }
      },
    );
  };

    window.enviarAtencion = function () {
        const observaciones = document.getElementById("txtObservaciones").value.trim();
        const requiereModificacion = document.getElementById("chkRequiereModificacion").checked;

        //Recoger los nuevos campos
        const idpp = parseInt($("#actividadSeleccionada").val()) || 0;
        const ff = $("#ffSelect").find("option:selected").text().trim();
        const tipoPrograma = $("#tipoProgramaSelect").find("option:selected").text().trim();
        const claveRegion = parseInt($("#municipio").val()) || 0;

        if (!observaciones) {
            alert("Debe escribir una observación.");
            return;
        }

        //Validar que los campos requeridos tengan valor
        if (!idpp) {
            alert("Debe seleccionar una actividad.");
            return;
        }
        if (!$("#ffSelect").val()) {
            alert("Debe seleccionar una fuente de financiamiento.");
            return;
        }
        if (!$("#tipoProgramaSelect").val()) {
            alert("Debe seleccionar un tipo de programa.");
            return;
        }
        if (!claveRegion) {
            alert("Debe seleccionar un municipio.");
            return;
        }

        const cogsEditados = [];
        document.querySelectorAll("#tablaDetalle tr").forEach(function (tr) {
            const inputCog = tr.querySelector(".select-cog-editable");
            const idArticuloTd = tr.querySelectorAll("td")[1];
            if (inputCog && idArticuloTd) {
                cogsEditados.push({
                    idArticulo: parseInt(idArticuloTd.textContent.trim()) || 0,
                    cog: parseInt($(inputCog).val()) || 0
                });
            }
        });

        $.ajax({
            url: atenderUrl,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                IdRequisicion: requisicionActual,
                Observaciones: observaciones,
                RequiereModificacion: requiereModificacion,
                CogsEditados: cogsEditados,
                IdPp: idpp,
                FF: ff,
                TipoPrograma: tipoPrograma,
                ClaveRegion: claveRegion
            }),
            success: function () {
                const modal = bootstrap.Modal.getInstance(document.getElementById("modalDetalle"));
                modal.hide();
                document.getElementById("txtObservaciones").value = "";
                document.getElementById("chkRequiereModificacion").checked = false;
                location.reload();
            },
            error: function () {
                alert("Error al atender la requisición.");
            }
        });
    };

  function mostrarMensajeVacio(totalVisibles, tbodyOptional) {
    var tbody =
      tbodyOptional ||
      document.querySelector(
        ".tabla-requisiciones:not(#tablaModalDetalle) tbody",
      );
    if (!tbody) return;

    var filaVacia = tbody.querySelector(".fila-vacia");
    if (totalVisibles === 0) {
      if (!filaVacia) {
        filaVacia = document.createElement("tr");
        filaVacia.className = "fila-vacia";
        filaVacia.innerHTML =
          '<td colspan="8" class="text-center">Sin resultados para los filtros aplicados</td>';
        tbody.appendChild(filaVacia);
      }
    } else {
      if (filaVacia) filaVacia.remove();
    }
  }

  let requisicionActual = null;

  window.atenderRequisicion = function (idMaestro) {
    requisicionActual = idMaestro;
    verDetalle(idMaestro, "atender");
  };

  if (modoTabs && container) {
    var tabBtns = container.querySelectorAll(".almacen-tabs-btn");
    var tabPanels = container.querySelectorAll(".almacen-tab-panel");
    tabBtns.forEach(function (btn) {
      btn.addEventListener("click", function () {
        var tab = this.getAttribute("data-tab");
        tabBtns.forEach(function (b) {
          b.classList.remove("activo");
        });
        tabPanels.forEach(function (p) {
          p.classList.remove("activo");
          if (p.id === "tab-" + tab) p.classList.add("activo");
        });
        this.classList.add("activo");
        aplicarPaginacionRequisiciones();
      });
    });
  }

  // Inicializar paginación al cargar el script
  aplicarPaginacionRequisiciones();

  var filtroNumReq = document.getElementById("filtroNumReq");
  var filtroDepto = document.getElementById("filtroDepartamento");
  if (filtroNumReq) filtroNumReq.addEventListener("input", filtrarTabla);
  if (filtroDepto) filtroDepto.addEventListener("input", filtrarTabla);

  window.verPdf = function (id) {
    var url = (verPdfUrl || "").replace(/\/$/, "") + "/" + id;
    window.open(url, "_blank");
  };

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

  $(document).on("select2:open", function () {
    document.querySelector(".select2-search__field")?.focus();
  });

  $(document).on("mousedown", function (e) {
    if (!_descPanelModalTrigger) return;
    var panel = document.getElementById("desc-panel-modal");
    if (
      !_descPanelModalTrigger.contains(e.target) &&
      panel &&
      !panel.contains(e.target)
    ) {
      cerrarDescPanelModal();
    }
  });

  window.verDetalle = function (idMaestro, modo = "ver") {
    const seccionesAtender = document.querySelectorAll(".seccionAtender");
      if (seccionesAtender.length > 0) {
          const isAtender = modo === "atender";

          seccionesAtender.forEach(function (sec) {
              sec.style.display = isAtender ? "block" : "none";
          });

          if (isAtender) {
              $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").select2({
                  dropdownParent: $("#modalDetalle"),
                  width: "100%",
                  language: "es",
                  placeholder: function () {
                      return $(this).data('placeholder');
                  }
              });
          }
      }
    if (!obtenerDetallesUrl) return;

      $.get(obtenerDetallesUrl, { idMaestro: idMaestro }, function (data) {
          var articulos = data.articulos || [];
          var esDonativo = data.donativo === true;

          var thCog = document.querySelector("#tablaModalDetalle thead tr th:last-child");
          if (thCog) thCog.style.display = esDonativo ? "" : "none";

          var contenido = "";
          if (articulos.length === 0) {
              contenido = '<tr><td colspan="' + (esDonativo ? 7 : 6) + '" class="text-center">Sin artículos</td></tr>';
          } else {
              articulos.forEach(function (item) {
                  var textoCompleto = item.descripcionDetallada || "";
                  var textoCorto = textoCompleto.length > 28
                      ? textoCompleto.substring(0, 28) + "…"
                      : textoCompleto || "Sin descripción...";
                  var tieneTexto = textoCompleto ? "tiene-texto" : "";
                  var fullEscapado = (textoCompleto || "").replace(/"/g, "&quot;");
                  var tdCog = esDonativo
                      ? '<td><select class="select-cog-editable" style="width:120px;"></select></td>'
                      : "";
                  contenido +=
                      "<tr>" +
                      "<td>" + (item.numPartida || "") + "</td>" +
                      "<td>" + (item.idArticulo || "") + "</td>" +
                      "<td>" + (item.cantidad || "") + "</td>" +
                      "<td>" + (item.unidadMedida || "") + "</td>" +
                      "<td>" + (item.descripcion || "") + "</td>" +
                      '<td><div class="desc-preview-modal" data-full="' + fullEscapado + '" onclick="verDescDetalleModal(this)">' +
                      '<span class="desc-texto-preview ' + tieneTexto + '">' + textoCorto + "</span>" +
                      '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                      tdCog +
                      "</tr>";
              });
          }

          $("#tablaDetalle").html(contenido);

          if (esDonativo) {
              $("#tablaDetalle .select-cog-editable").each(function () {
                  $(this).select2({
                      dropdownParent: $("#modalDetalle"),
                      width: "resolve",
                      placeholder: "COG...",
                      minimumInputLength: 1,
                      language: "es",
                      ajax: {
                          url: urlBuscarCogs,
                          dataType: "json",
                          delay: 250,
                          data: function (params) {
                              return { term: params.term };
                          },
                          processResults: function (data) {
                              return { results: data };
                          },
                          cache: true
                      }
                  });
              });
          }

          var subtitulo = document.querySelector("#modalDetalle .modal-subtitulo-premium");
          if (subtitulo)
              subtitulo.textContent = "Detalle de partidas solicitadas · Total: " + articulos.length + " partidas";

          // ✅ Precargar selects si la requisición ya tiene datos previos
          if (modo === "atender") {
              if (data.idPp) {
                  $("#actividadSeleccionada").val(data.idPp).trigger("change");
              }
              if (data.ff) {
                  $("#ffSelect option").filter(function () {
                      return $(this).text().trim() === data.ff;
                  }).prop("selected", true);
                  $("#ffSelect").trigger("change");
              }
              if (data.tipoPrograma) {
                  $("#tipoProgramaSelect option").filter(function () {
                      return $(this).text().trim() === data.tipoPrograma;
                  }).prop("selected", true);
                  $("#tipoProgramaSelect").trigger("change");
              }
              if (data.claveRegion) {
                  $("#municipio").val(data.claveRegion).trigger("change");
              }
          }

          var modal = new bootstrap.Modal(document.getElementById("modalDetalle"));
          modal.show();
      });
  };
})();
