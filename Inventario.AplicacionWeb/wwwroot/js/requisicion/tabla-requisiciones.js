
(function () {
    var container = document.querySelector(".tabla-requi-page");
    var esTablaServicios = container?.getAttribute('data-tipo-tabla') === 'servicios';
  var obtenerDetallesUrl = container
    ? container.getAttribute("data-url-obtener-detalles")
    : "";
  var verPdfUrl = container ? container.getAttribute("data-url-ver-pdf") : "";
  var urlUsuariosMateriales = container
    ? container.getAttribute("data-url-usuarios-materiales")
        : "";
  var urlUsuariosServicios = container
      ? container.getAttribute("data-url-usuarios-servicios")
      : "";
  var urlAsignar = container ? container.getAttribute("data-url-asignar") : "";
  const contenedor = document.querySelector(".tabla-requi-page");
  const atenderUrl = contenedor.dataset.urlAtender;
    var urlBuscarCogs = container ? container.getAttribute("data-url-buscar-cogs") : "";
    var urlEnviarAlmacen = container ? container.getAttribute("data-url-enviar-almacen") : "";
    var urlRechazar = container ? container.getAttribute("data-url-rechazar") : "";
    var urlModificar = container ? container.getAttribute("data-url-modificar") : "";
  var _idRequiAsignar = null;

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
  var paginaPorTab = { principal: 1, autorizadas: 1, rechazadas: 1 };

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
          : panel.id === "tab-rechazadas"
            ? "rechazadas"
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

      var textoEstado = (
          (document.getElementById("filtroEstado") &&
              document.getElementById("filtroEstado").value) || ""
      ).toLowerCase().trim();

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

    var todasLasFilas = [].slice.call(ctx.tbody.querySelectorAll("tr")).filter(function (tr) {
      return !tr.classList.contains("fila-vacia") && !tr.classList.contains("fila-detalle");
    });
    var filasVisibles = [];

    todasLasFilas.forEach(function (fila) {
      var celdas = fila.querySelectorAll("td");
      if (!celdas.length) return;

      var folio = (celdas[1] && celdas[1].textContent.toLowerCase()) || "";
      var fecha = (celdas[2] && celdas[2].textContent.trim()) || "";
      var depto = (celdas[3] && celdas[3].textContent.toLowerCase()) || "";

      var pasaNumReq = !textoNumReq || folio.indexOf(textoNumReq) !== -1;
      var pasaFecha = !fechaSeleccionada || fecha === fechaSeleccionada;
        var pasaDepto = !textoDepto || depto.indexOf(textoDepto) !== -1;

        var estado = (celdas[6] && celdas[6].textContent.toLowerCase()) || "";
        var pasaEstado = !textoEstado || estado.indexOf(textoEstado) !== -1;

        if (pasaNumReq && pasaFecha && pasaDepto && pasaEstado) {
        filasVisibles.push(fila);
      } else {
        fila.style.display = "none";
        var siguiente = fila.nextElementSibling;
        if (siguiente && siguiente.classList.contains("fila-detalle")) siguiente.style.display = "none";
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
      var visible = i >= inicio && i < fin;
      fila.style.display = visible ? "" : "none";
      var siguiente = fila.nextElementSibling;
      if (siguiente && siguiente.classList.contains("fila-detalle")) siguiente.style.display = visible ? "" : "none";
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

    function ocultarTodosPasos() {
        ['pasoOpciones', 'pasoAsignar', 'pasoAlmacen', 'pasoRechazar', 'pasoModificar'].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.style.display = 'none';
        });
    }

    window.volverOpciones = function () {
        ocultarTodosPasos();
        document.getElementById('pasoOpciones').style.display = 'block';
    };

    window.mostrarPasoAsignar = function () {
        ocultarTodosPasos();
        document.getElementById('pasoAsignar').style.display = 'block';
    };

    window.mostrarPasoAlmacen = function () {
        ocultarTodosPasos();
        document.getElementById('pasoAlmacen').style.display = 'block';
    };

    window.mostrarPasoRechazar = function () {
        ocultarTodosPasos();
        document.getElementById('pasoRechazar').style.display = 'block';
    };

    window.mostrarPasoModificar = function () {
        ocultarTodosPasos();
        document.getElementById('pasoModificar').style.display = 'block';
    };

    // Abrir modal — siempre arranca en paso 1
    window.abrirModalAsignar = function (idRequi) {
        _idRequiAsignar = idRequi;

        if (esTablaServicios) {
            // Servicios: Asignar + Rechazar (sin Enviar a almacén)
            volverOpciones();
            document.getElementById('txtMotivoRechazo').value = '';

            //Ocultar el botón de enviar a almacén
            var btnAlmacen = document.getElementById('btnOpcionAlmacen');
            if (btnAlmacen) btnAlmacen.style.display = 'none';

            var $select = $("#selectUsuarioAsignar");
            if ($select.data("select2")) $select.select2("destroy");
            $select.html('<option value="">-- Seleccionar responsable --</option>');

            $.get(urlUsuariosServicios, function (data) {
                data.forEach(function (u) {
                    $select.append('<option value="' + u.id + '">' + u.nombre + '</option>');
                });
                $select.select2({
                    language: "es",
                    placeholder: "-- Seleccionar responsable --",
                    allowClear: false,
                    minimumResultsForSearch: Infinity,
                    width: "100%"
                });
            });

        } else {
            // Requisiciones: los 3 botones
            volverOpciones();
            document.getElementById('txtMotivoRechazo').value = '';

            //Asegurar que el botón de almacén esté visible
            var btnAlmacen = document.getElementById('btnOpcionAlmacen');
            if (btnAlmacen) btnAlmacen.style.display = '';

            var $select = $("#selectUsuarioAsignar");
            if ($select.data("select2")) $select.select2("destroy");
            $select.html('<option value="">-- Seleccionar responsable --</option>');

            $.get(urlUsuariosMateriales, function (data) {
                data.forEach(function (u) {
                    $select.append('<option value="' + u.id + '">' + u.nombre + '</option>');
                });
                $select.select2({
                    language: "es",
                    placeholder: "-- Seleccionar responsable --",
                    allowClear: false,
                    minimumResultsForSearch: Infinity,
                    width: "100%"
                });
            });
        }

        var modal = new bootstrap.Modal(document.getElementById("modalAsignar"));
        modal.show();
    };

    // Confirmar envío a almacén
    window.confirmarEnvioAlmacen = function () {
        $.post(urlEnviarAlmacen, { idRequi: _idRequiAsignar }, function (res) {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById("modalAsignar")).hide();
                Swal.fire({
                    icon: 'success',
                    title: 'Enviada a almacén',
                    confirmButtonText: 'Aceptar',
                    confirmButtonColor: '#fe6291'
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'No se pudo enviar a almacén',
                    text: res.mensaje || 'Error desconocido',
                    footer: res.detalle || '',
                    confirmButtonText: 'Aceptar',
                    confirmButtonColor: '#fe6291'
                });
            }
        });
    };

    // Confirmar rechazo
    window.confirmarRechazo = function () {
        var motivo = document.getElementById('txtMotivoRechazo').value.trim();
        if (!motivo) {
            Swal.fire({ icon: 'warning', title: 'Escribe el motivo del rechazo', confirmButtonText: 'Ok' });
            return;
        }

        $.post(urlRechazar, { idRequi: _idRequiAsignar, motivo: motivo }, function (res) {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById("modalAsignar")).hide();
                Swal.fire({
                    icon: 'success',
                    title: 'Requisición rechazada',
                    confirmButtonText: 'Aceptar',
                    confirmButtonColor: '#fe6291'
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({ icon: 'error', title: 'No se pudo rechazar la requisición' });
            }
        });
    };

    window.confirmarModificacion = function () {
        var observacion = document.getElementById('txtObservacionModificacion').value.trim();
        if (!observacion) {
            Swal.fire({ icon: 'warning', title: 'Escribe las observaciones de modificación', confirmButtonText: 'Ok' });
            return;
        }

        $.post(urlModificar, { idRequi: _idRequiAsignar, observaciones: observacion }, function (res) {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById("modalAsignar")).hide();
                Swal.fire({
                    icon: 'success',
                    title: 'Enviada a modificación',
                    timer: 2000,
                    showConfirmButton: false,
                    timerProgressBar: true
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({ icon: 'error', title: 'No se pudo enviar a modificación', text: res.mensaje || '' });
            }
        }).fail(function () {
            Swal.fire({ icon: 'error', title: 'Error al enviar la solicitud' });
        });
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

  /* Expandir/colapsar fila detalle al hacer clic en la fila de requisición */
  function getTablaFromRow(tr) {
    return tr ? tr.closest("table.tabla-requisiciones") : null;
  }

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

      var tabla = getTablaFromRow(tr);
      var filaDetalle = tr.nextElementSibling;
      if (!filaDetalle || !filaDetalle.classList.contains("fila-detalle")) return;

      var yaExpandida = filaDetalle.classList.contains("expanded");
      colapsarTodasDetalle(tabla);

      if (!yaExpandida) {
        filaDetalle.classList.remove("collapsed");
        filaDetalle.classList.add("expanded");
      }
    });

    // Colapsar fila-detalle al hacer clic fuera de cualquier tabla de requisiciones
    document.addEventListener("click", function (e) {
      if (!e.target.closest("table.tabla-requisiciones")) {
        const tablas = document.querySelectorAll("table.tabla-requisiciones");
        tablas.forEach(t => colapsarTodasDetalle(t));
      }
    });
  }

  var filtroNumReq = document.getElementById("filtroNumReq");
    var filtroDepto = document.getElementById("filtroDepartamento");
    var filtroEstado = document.getElementById("filtroEstado");
  if (filtroNumReq) filtroNumReq.addEventListener("input", filtrarTabla);
    if (filtroDepto) filtroDepto.addEventListener("input", filtrarTabla);
    if (filtroEstado) filtroEstado.addEventListener("change", filtrarTabla);

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
     var galeriaFotosDetalle = document.getElementById('galeriaFotosDetalle');
     var seccionFotosDetalle = document.getElementById('seccionFotosDetalle');
     if (galeriaFotosDetalle) galeriaFotosDetalle.innerHTML = '';
     if (seccionFotosDetalle) seccionFotosDetalle.style.display = 'none';
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
        contenido = '<tr><td colspan="' + (esDonativo ? 6 : 5) + '" class="text-center">Sin artículos</td></tr>';
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

        var seccionFotos = document.getElementById('seccionFotosDetalle');
        var galeriaFotos = document.getElementById('galeriaFotosDetalle');

        if (seccionFotos && galeriaFotos) {
            if (data.tipoServicio === 'Imprenta' && data.fotos && data.fotos.length > 0) {
                galeriaFotos.innerHTML = '';
                data.fotos.forEach(function (ruta) {
                    var wrapper = document.createElement('div');
                    wrapper.style.cssText = 'display:inline-block; text-align:center;';
                    var img = document.createElement('img');
                    img.src = ruta;
                    img.style.cssText = 'width:90px; height:90px; object-fit:cover; border-radius:8px; border:1px solid #ddd; cursor:pointer;';
                    img.title = 'Click para ver en tamaño completo';
                    img.addEventListener('click', function () { window.open(ruta, '_blank'); });
                    wrapper.appendChild(img);
                    galeriaFotos.appendChild(wrapper);
                });
                seccionFotos.style.display = 'block';
            } else {
                seccionFotos.style.display = 'none';
            }
        }

      var subtitulo = document.querySelector("#modalDetalle .modal-subtitulo-premium");
      if (subtitulo)
        subtitulo.textContent = "Detalle de partidas solicitadas · Total: " + articulos.length + " partidas";

      //Precargar selects si la requisición ya tiene datos previos
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

  /* ══════════════════════════════════════════════
     MODAL DE HISTORIAL COMPLETO
  ══════════════════════════════════════════════ */
  var urlObtenerProgreso = container ? container.getAttribute("data-url-obtener-progreso") : "";

  function renderHistorialSteps(steps) {
    var done = 0, active = 0, cancelled = 0, completed = 0;
    steps.forEach(function (s) {
      if (s.state === 'done') done++;
      else if (s.state === 'active') active++;
      else if (s.state === 'cancelled') cancelled++;
      else if (s.state === 'completed') completed++;
    });

    var summaryEl = document.getElementById('historialSummary');
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

    var mtl = document.getElementById('historialTl');
    mtl.innerHTML = '';

    steps.forEach(function (s) {
      var bCls, bTxt;
      switch (s.state) {
        case 'done':      bCls = 'mbadge-done';      bTxt = 'Completado'; break;
        case 'active':    bCls = 'mbadge-active';     bTxt = 'En curso';   break;
        case 'completed': bCls = 'mbadge-completed';  bTxt = 'Finalizado'; break;
        case 'cancelled': bCls = 'mbadge-cancelled';  bTxt = 'Cancelada';  break;
        default:          bCls = 'mbadge-pending';     bTxt = 'Pendiente';  break;
      }
      var tStr = s.time !== '—' ? ' · ' + s.time : '';

      var item = document.createElement('div');
      item.className = 'mtl-item ' + s.state;
      item.innerHTML =
        '<div class="mtl-dot-col"><div class="mtl-dot"></div></div>' +
        '<div class="mtl-content">' +
          '<div class="mtl-dept">' + s.dept + '</div>' +
          '<div class="mtl-meta">' +
            '<span class="mtl-badge ' + bCls + '">' + bTxt + '</span>' +
            '<span class="mtl-time">' + s.date + tStr + '</span>' +
          '</div>' +
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
    document.getElementById('historialSubtitle').textContent = numRequi;
    document.getElementById('historialSummary').innerHTML =
      '<div style="text-align:center;color:#888;padding:1rem;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando historial…</div>';
    document.getElementById('historialTl').innerHTML = '';

    var modal = new bootstrap.Modal(document.getElementById("modalHistorial"));
    modal.show();

    if (!urlObtenerProgreso) {
      document.getElementById('historialSummary').innerHTML =
        '<div style="color:#b91c1c;text-align:center;padding:1rem;">URL de progreso no configurada</div>';
      return;
    }

    $.get(urlObtenerProgreso, { idRequisicion: idRequi }, function (data) {
      var steps = (data || []).map(function (s) {
        return {
          dept:    s.dept    || s.Dept    || '',
          date:    s.date    || s.Date    || '—',
          state:   s.state   || s.State   || 'pending',
          by:      s.by      || s.By      || '—',
          time:    s.time    || s.Time    || '—',
          action:  s.action  || s.Action  || '',
          comment: s.comment || s.Comment || ''
        };
      });
      renderHistorialSteps(steps);
    }).fail(function () {
      document.getElementById('historialSummary').innerHTML =
        '<div style="color:#b91c1c;text-align:center;padding:1rem;">' +
        '<i class="fa-solid fa-triangle-exclamation"></i> Error al cargar el historial</div>';
    });
  };

  /* ══════════════════════════════════════════════
     VISOR DE IMÁGENES (LIGHTBOX) para la tabla
  ══════════════════════════════════════════════ */
  function abrirVisorImagenTabla(src) {
    var overlay = document.getElementById('visor-imagenes-tabla');
    var img = document.getElementById('visor-imagenes-tabla-img');
    if (overlay && img) {
      img.src = src;
      overlay.classList.add('activo');
    }
  }

  function cerrarVisorImagenTabla() {
    var overlay = document.getElementById('visor-imagenes-tabla');
    if (overlay) {
      overlay.classList.remove('activo');
      setTimeout(function () {
        var img = document.getElementById('visor-imagenes-tabla-img');
        if (img && !overlay.classList.contains('activo')) {
          img.src = '';
        }
      }, 300);
    }
  }

  // Cierre al hacer clic FUERA de la imagen (en el fondo oscuro)
  var visorTabla = document.getElementById('visor-imagenes-tabla');
  if (visorTabla) {
    visorTabla.addEventListener('click', function (e) {
      if (e.target === visorTabla) cerrarVisorImagenTabla();
    });
  }

})();
