
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
    var urlSubirArchivosAtencion = container
        ? container.getAttribute("data-url-subir-archivos-atencion")
        : "";
    var _idRequiAsignar = null;
    var _idRequiCotizaciones = null;
    var expedienteActual = null;
    var expedienteEstatusActual = 0;
    var expedienteNotaActual = "";
    var urlAceptarExpediente = container
        ? container.getAttribute("data-url-aceptar-expediente")
        : "";

    var urlSubirDocProveedor = container
        ? container.getAttribute("data-url-subir-doc-proveedor") : "";
    var urlObtenerDocsProveedor = container
        ? container.getAttribute("data-url-obtener-docs-proveedor") : "";
    var urlEnviarFinancierosDocs = container
        ? container.getAttribute("data-url-enviar-financieros-docs") : "";
    var urlRebotarDocumentos = container
        ? container.getAttribute("data-url-rebotar-documentos") : "";
    var urlDescargarCuadroComparativo = container
        ? container.getAttribute("data-url-descargar-cuadro-comparativo") : "";

    var urlGuardarCotizaciones = container
        ? container.getAttribute("data-url-guardar-cotizaciones") : "";
    var urlObtenerCotizaciones = container
        ? container.getAttribute("data-url-obtener-cotizaciones") : "";
    var urlObtenerPartidas = container
        ? container.getAttribute("data-url-obtener-partidas")
        : "";

    var DOCUMENTOS_PROVEEDOR = [
        { clave: "CFDI_PDF", label: "Factura CFDI (PDF)" },
        { clave: "CFDI_XML", label: "Factura CFDI (XML)" },
        { clave: "ConstFiscal", label: "Constancia de Situación Fiscal" },
        { clave: "OpinionSAT", label: "Opinión de Cumplimiento SAT" },
        { clave: "INFONAVIT", label: "Constancia de No Adeudo INFONAVIT" },
        { clave: "Padron", label: "Alta en Padrón de Proveedores" },
        { clave: "CedulaRFC", label: "Cédula de Identificación Fiscal" },
        { clave: "IdOficial", label: "Identificación Oficial (INE/Pasaporte)" },
        { clave: "OrdenCompra", label: "Orden de Compra o Servicio" },
        { clave: "Evidencia", label: "Evidencia de Entrega (Visto Bueno)" },
        { clave: "EstadoCuenta", label: "Estado de Cuenta Bancario (CLABE)" },
        { clave: "ActaConst", label: "Acta Constitutiva" },
        { clave: "CompDomicilio", label: "Comprobante de Domicilio" },
        { clave: "MemoPago", label: "Memorandum Instrucción de Pago" }
    ];

    var CACHE_PARTIDAS = [];

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
    var paginaPorTab = { principal: 1, autorizadas: 1, rechazadas: 1, verificadas: 1 };

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
          panel.id === "tab-principal" ? "principal" :
              panel.id === "tab-autorizadas" ? "autorizadas" :
                  panel.id === "tab-rechazadas" ? "rechazadas" :
                      panel.id === "tab-verificadas" ? "verificadas" : "principal";
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

    function obtenerOpcionesPartidasHtml(callback) {
        var buildHtml = function (data) {
            var html = '<option value="">-- Seleccione partida --</option>';
            data.forEach(function (p) {
                html += '<option value="' + p.idRequiDetalle + '">' + p.nombrePartida + '</option>';
            });
            callback(html);
        };

        if (CACHE_PARTIDAS.length) {
            buildHtml(CACHE_PARTIDAS);
        } else {
            $.get(urlObtenerPartidas, { idRequisicion: _idRequiCotizaciones }, function (data) {
                CACHE_PARTIDAS = data;
                buildHtml(data);
            });
        }
    }

    // Llena SOLO el select de partidas de una fila específica (sin tocar las demás)
    function cargarPartidasEnFila($fila, valorSeleccionado) {
        obtenerOpcionesPartidasHtml(function (html) {
            var $sel = $fila.find(".modal-partida-select");
            $sel.html(html);
            if (valorSeleccionado) $sel.val(valorSeleccionado);
        });
    }

    // Llena TODOS los selects de partidas (solo al abrir el modal, antes de precargar valores)
    function cargarPartidasEnTodasLasFilas(callback) {
        obtenerOpcionesPartidasHtml(function (html) {
            $(".modal-partida-select").html(html);
            if (callback) callback();
        });
    }

    function renderChecklist(docsSubidos, idRequi, idEstatus, notaObservacion) {
        var notaEl = document.getElementById("expNotaObservacion");
        if (notaObservacion && idEstatus === 18) {
            notaEl.textContent = "Financieros observaron: " + notaObservacion;
            notaEl.style.display = "block";
        } else {
            notaEl.style.display = "none";
        }

        var clavesSubidas = docsSubidos.map(function (d) { return d.nombreArchivo; });
        var checklist = document.getElementById("expChecklistDocs");
        checklist.innerHTML = "";

        var todosSubidos = true;

        DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
            var subido = clavesSubidas.indexOf(doc.clave) !== -1;
            if (!subido) todosSubidos = false;

            var archivo = docsSubidos.find(function (d) { return d.nombreArchivo === doc.clave; });
            var esObservado = idEstatus === 18 &&
                notaObservacion && notaObservacion.indexOf(doc.label) !== -1;

            var fila = document.createElement("div");
            fila.style.cssText = "display:flex; align-items:center; gap:10px; padding:8px 12px;" +
                "border-radius:8px; border:1px solid " +
                (esObservado ? "#fecaca" : (subido ? "#bbf7d0" : "var(--color-border-tertiary)")) + ";" +
                "background:" + (esObservado ? "#fef2f2" : (subido ? "#f0fdf4" : "var(--color-background-secondary)")) + ";";

            var icono = subido
                ? '<i class="fa-solid fa-circle-check" style="color:#16a34a;font-size:16px;flex-shrink:0;"></i>'
                : '<i class="fa-regular fa-circle" style="color:#9ca3af;font-size:16px;flex-shrink:0;"></i>';

            if (esObservado) {
                icono = '<i class="fa-solid fa-circle-exclamation" style="color:#dc2626;font-size:16px;flex-shrink:0;"></i>';
            }

            var linkArchivo = subido && archivo
                ? '<a href="' + archivo.ruta + '" target="_blank" ' +
                'style="font-size:11px;color:var(--color-text-secondary);margin-left:auto;text-decoration:none;">' +
                '<i class="fa-solid fa-eye"></i> Ver</a>'
                : '';

            // Botón subir solo si estatus permite (16 o 18)
            var btnSubir = "";
            if (idEstatus === 15 || idEstatus === 18) {
                btnSubir = '<label style="margin-left:auto;cursor:pointer;">' +
                    '<input type="file" style="display:none;" ' +
                    'onchange="subirDocProveedor(' + idRequi + ', \'' + doc.clave + '\', this)">' +
                    '<span style="font-size:11px;color:var(--color-text-secondary);' +
                    'padding:3px 8px;border:1px solid var(--color-border-secondary);' +
                    'border-radius:6px;white-space:nowrap;">' +
                    (subido ? '<i class="fa-solid fa-arrow-rotate-right"></i> Reemplazar'
                        : '<i class="fa-solid fa-upload"></i> Subir') +
                    '</span></label>';
            }

            fila.innerHTML = icono +
                '<span style="font-size:13px;flex:1;">' + doc.label + '</span>' +
                linkArchivo + btnSubir;

            checklist.appendChild(fila);
        });

        var acciones = document.getElementById("expAccionesProveedor");
        if (idEstatus === 15 || idEstatus === 18) {
            acciones.style.display = todosSubidos ? "block" : "none";
        } else {
            acciones.style.display = "none";
        }
    }

    window.subirDocProveedor = function (idRequi, clave, inputEl) {
        var archivo = inputEl.files[0];
        if (!archivo) return;

        var formData = new FormData();
        formData.append("idRequisicion", idRequi);
        formData.append("tipoDocumento", clave);
        formData.append("archivo", archivo);

        $.ajax({
            url: urlSubirDocProveedor,
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function () {
                $.get(urlObtenerDocsProveedor, { idRequisicion: idRequi }, function (docs) {
                    var idEstatus = expedienteActual ? parseInt(
                        document.querySelector('[data-requi-id="' + idRequi + '"].fila-requi')
                            ?.querySelector("td:nth-child(7)")?.textContent || "0"
                    ) : 0;
                    renderChecklist(docs, idRequi, expedienteEstatusActual, expedienteNotaActual);
                });
            },
            error: function () {
                Swal.fire({ icon: "error", title: "Error al subir el archivo" });
            }
        });
    };

    window.enviarDocumentosAFinancieros = function () {
        Swal.fire({
            title: "\u00bfEnviar a financieros?",
            text: "Se enviar\u00e1n todos los documentos para revisi\u00f3n.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            confirmButtonText: "S\u00ed, enviar",
            cancelButtonText: "Cancelar"
        }).then(function (result) {
            if (!result.isConfirmed) return;
            $.post(urlEnviarFinancierosDocs, { idRequisicion: expedienteActual }, function (res) {
                if (res.success) {
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalExpediente")).hide();
                    Swal.fire({
                        icon: "success", title: "Enviado a financieros",
                        timer: 2000, showConfirmButton: false
                    }).then(function () { location.reload(); });
                }
            });
        });
    };

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
    window.abrirModalAsignar = function (idRequi, idEstatus) {
        _idRequiAsignar = idRequi;
        var esDeCompra = idEstatus === 11;

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
            if (btnAlmacen) btnAlmacen.style.display = esDeCompra ? 'none' : '';

            var btnModificar = document.querySelector('#pasoOpciones button[onclick="mostrarPasoModificar()"]');
            if (btnModificar) btnModificar.style.display = esDeCompra ? 'none' : '';

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
                    title: 'Enviada a almac\u00e9n',
                    confirmButtonText: 'Aceptar',
                    confirmButtonColor: '#fe6291'
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'No se pudo enviar a almac\u00e9n',
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
                    title: 'Requisici\u00f3n rechazada',
                    confirmButtonText: 'Aceptar',
                    confirmButtonColor: '#fe6291'
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({ icon: 'error', title: 'No se pudo rechazar la requisici\u00f3n' });
            }
        });
    };

    window.confirmarModificacion = function () {
        var observacion = document.getElementById('txtObservacionModificacion').value.trim();
        if (!observacion) {
            Swal.fire({ icon: 'warning', title: 'Escribe las observaciones de modificaci\u00f3n', confirmButtonText: 'Ok' });
            return;
        }

        $.post(urlModificar, { idRequi: _idRequiAsignar, observaciones: observacion }, function (res) {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById("modalAsignar")).hide();
                Swal.fire({
                    icon: 'success',
                    title: 'Enviada a modificaci\u00f3n'
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({ icon: 'error', title: 'No se pudo enviar a modificaci\u00f3n', text: res.mensaje || '' });
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
        var observaciones = document.getElementById("txtObservaciones").value.trim();
        var idpp = parseInt($("#actividadSeleccionada").val()) || 0;
        var ff = $("#ffSelect").find("option:selected").text().trim();
        var tipoPrograma = $("#tipoProgramaSelect").find("option:selected").text().trim();
        var claveRegion = parseInt($("#municipio").val()) || 0;

        if (!observaciones) { Swal.fire({ icon: 'warning', title: 'Debe escribir una observaci\u00f3n.' }); return; }
        if (!idpp) { Swal.fire({ icon: 'warning', title: 'Debe seleccionar una actividad.' }); return; }
        if (!$("#ffSelect").val()) { Swal.fire({ icon: 'warning', title: 'Debe seleccionar una fuente de financiamiento.' }); return; }
        if (!$("#tipoProgramaSelect").val()) { Swal.fire({ icon: 'warning', title: 'Debe seleccionar un tipo de programa.' }); return; }
        if (!claveRegion) { Swal.fire({ icon: 'warning', title: 'Debe seleccionar un municipio.' }); return; }

        var cogsEditados = [];
        document.querySelectorAll("#tablaDetalle tr").forEach(function (tr) {
            var inputCog = tr.querySelector(".select-cog-editable");
            var idArticuloTd = tr.querySelectorAll("td")[1];
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
                var inputCot = document.getElementById("inputCotizaciones");
                var inputCuadro = document.getElementById("inputCuadroComparativo");
                var inputAnexos = document.getElementById("inputAnexos");
                var tieneCot = inputCot && inputCot.files.length > 0;
                var tieneCuadro = inputCuadro && inputCuadro.files.length > 0;
                var tieneAnexos = inputAnexos && inputAnexos.files.length > 0;

                if (!tieneCot && !tieneCuadro) {
                    // Sin archivos, terminar directo
                    bootstrap.Modal.getInstance(document.getElementById("modalDetalle")).hide();
                    document.getElementById("txtObservaciones").value = "";
                    location.reload();
                    return;
                }

                var formData = new FormData();
                formData.append("IdRequisicion", requisicionActual);
                if (tieneCot) Array.from(inputCot.files).forEach(f => formData.append("Cotizaciones", f));
                if (tieneCuadro) Array.from(inputCuadro.files).forEach(f => formData.append("CuadroComparativo", f));
                if (tieneAnexos) Array.from(inputAnexos.files).forEach(f => formData.append("Anexos", f));

                $.ajax({
                    url: urlSubirArchivosAtencion,
                    type: "POST",
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function () {
                        bootstrap.Modal.getInstance(document.getElementById("modalDetalle")).hide();
                        document.getElementById("txtObservaciones").value = "";
                        location.reload();
                    },
                    error: function () {
                        Swal.fire({ icon: 'warning', title: 'La atenci\u00f3n se guard\u00f3, pero hubo un error al subir los archivos.' });
                        location.reload();
                    }
                });
            },
            error: function () {
                Swal.fire({ icon: 'error', title: 'Error al atender la requisici\u00f3n.' });
            }
        });
    };

    function renderizarArchivosReadonly(cotizaciones, cuadro) {
        if (window.ModalAdjuntos && typeof window.ModalAdjuntos.renderizarArchivosReadonly === "function") {
            window.ModalAdjuntos.renderizarArchivosReadonly(cotizaciones, cuadro);
        }
    }

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

        var panelActivoAnterior = container.querySelector(".almacen-tab-panel.activo");
        if (panelActivoAnterior && panelActivoAnterior.id !== "tab-" + tab) {
            var filasVistas = panelActivoAnterior.querySelectorAll("tr.fila-nueva");
            filasVistas.forEach(function(f) { f.classList.remove("fila-nueva"); });
        }

        if (window.TabsNotificacionesRequi) {
          window.TabsNotificacionesRequi.limpiarBadgeNotificacionTab(this);
        }
        var panelDestinoClick = document.getElementById("tab-" + tab);

        tabBtns.forEach(function (b) {
          b.classList.remove("activo");
        });
        tabPanels.forEach(function (p) {
          p.classList.remove("activo");
          if (p.id === "tab-" + tab) p.classList.add("activo");
        });
        this.classList.add("activo");
        aplicarPaginacionRequisiciones();
        window.requestAnimationFrame(function () {
          if (window.TabsNotificacionesRequi) {
            window.TabsNotificacionesRequi.iniciarParpadeoFilasNuevasEnPanel(panelDestinoClick);
          }
        });
      });
    });
  }

  // Inicializar paginación al cargar el script
  aplicarPaginacionRequisiciones();

  if (container && window.TabsNotificacionesRequi && modoTabs) {
    window.TabsNotificacionesRequi.mount({
      container: container,
      onNovedadEnTabActivo: function () {
        aplicarPaginacionRequisiciones();
      }
    });
  }

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

  window.descargarCuadroComparativo = function () {
    if (!requisicionActual) {
      Swal.fire({
        icon: "warning",
        title: "Sin requisicion",
        text: "Primero abre una requisicion en modo atender.",
        confirmButtonText: "Ok",
        confirmButtonColor: "#fe6291",
      });
      return;
    }

    var url = (urlDescargarCuadroComparativo || "").replace(/\/$/, "") +
      "?idRequisicion=" + requisicionActual;
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

  (function modalProveedoresRequisicion() {
    // ── Catálogo de proveedores (leído del content del <template>, que es un DocumentFragment) ──
    var CATALOGO_PROVEEDORES_MODAL = [];
    var _tplEl = document.getElementById("tplModalProveedorFila");
    if (_tplEl) {
        _tplEl.content.querySelectorAll(".modal-proveedores-select option").forEach(function (o) {
            if (o.value) CATALOGO_PROVEEDORES_MODAL.push({ v: o.value, t: o.text });
        });
    }

    // ── Estado del wizard ──
    // wizardDatos[i] = [ { idProveedor, importe }, ... ]  (filas de la partida i)
    var wizardPartidas = [];   // array de { idRequiDetalle, nombrePartida }
    var wizardDatos = [];      // datos por índice de partida
    var wizardIdx = 0;         // índice de la partida activa

    function $modal() { return $("#modalProveedoresRequisicion"); }
    function $contenedor() { return $("#modalProveedoresFilas"); }

    // ── Helpers Select2 ──
    function buildOpcionesHtml() {
        var html = '<option value="">-- Seleccione proveedor --</option>';
        CATALOGO_PROVEEDORES_MODAL.forEach(function (p) {
            html += '<option value="' + p.v + '">' + p.t + '</option>';
        });
        return html;
    }

    function initSelect2($sel) {
        if (!$sel.length || typeof $.fn.select2 === "undefined") return;
        $sel.select2({ dropdownParent: $modal(), width: "100%", language: "es" });
    }

    function destruirSelect2En($root) {
        $root.find(".modal-proveedores-select").each(function () {
            if ($(this).data("select2")) $(this).select2("destroy");
        });
    }

    function actualizarOpcionesProveedores() {
        var seleccionados = [];
        $contenedor().find(".modal-proveedores-select").each(function () {
            var val = $(this).val();
            if (val) seleccionados.push(val);
        });

        $contenedor().find(".modal-proveedores-select").each(function () {
            var $sel = $(this);
            var currentVal = $sel.val();
            
            // Reconstruir opciones ocultando los seleccionados en otras filas
            var newHtml = '<option value="">-- Seleccione proveedor --</option>';
            if (CATALOGO_PROVEEDORES_MODAL) {
                CATALOGO_PROVEEDORES_MODAL.forEach(function (c) {
                    var strId = String(c.v);
                    var isSelectedInOther = (strId !== String(currentVal) && seleccionados.indexOf(strId) !== -1);
                    if (!isSelectedInOther) {
                        newHtml += '<option value="' + c.v + '"' + (strId === String(currentVal) ? ' selected' : '') + '>' + c.t + '</option>';
                    }
                });
            }
            
            // Comprobar si el innerHTML ha cambiado para evitar reinicializaciones innecesarias
            // Validamos contando cuántos options tiene ahora vs los que tendría el nuevo HTML
            var currentOptionsCount = $sel.find("option").length;
            var newOptionsCount = (newHtml.match(/<option/g) || []).length;
            
            // También comprobamos si los values exactos cambiaron
            var currentVals = [];
            $sel.find("option").each(function() { currentVals.push($(this).val()); });
            var newVals = [];
            var match;
            var regex = /value="([^"]*)"/g;
            while ((match = regex.exec(newHtml)) !== null) {
                newVals.push(match[1]);
            }
            
            var changed = (currentVals.join(",") !== newVals.join(","));

            if (changed) {
                $sel.html(newHtml);
                if ($sel.data("select2")) {
                    $sel.select2("destroy");
                    initSelect2($sel);
                }
            }
        });
    }

    $(document).on("change", "#modalProveedoresFilas .modal-proveedores-select", function () {
        actualizarOpcionesProveedores();
    });

    // ── Guardar filas actuales al estado del wizard ──
    function guardarFilasActuales() {
        var filas = [];
        $contenedor().find(".modal-proveedores-fila").each(function () {
            filas.push({
                idProveedor: parseInt($(this).find(".modal-proveedores-select").val()) || 0,
                importe: $(this).find(".modal-proveedores-input-precio").val() || "",
                iva: $(this).find(".modal-proveedores-check-iva").prop("checked")
            });
        });
        wizardDatos[wizardIdx] = filas;
    }

    // ── Renderizar la pantalla de la partida activa ──
    function renderizarPartida(idx) {
        var $c = $contenedor();
        var partida = wizardPartidas[idx];
        var total = wizardPartidas.length;

        // Actualizar header
        document.getElementById("wizardProveedoresSubtitulo").textContent = "Partida " + (idx + 1) + " de " + total;
        var numLabel = partida.numPartida ? "Partida " + partida.numPartida : "";
        document.getElementById("wizardProveedoresNombrePartida").innerHTML =
            (numLabel ? '<span style="font-weight:700;">' + numLabel + '</span> ' : "") +
            '<span style="font-weight:400; color:var(--slate-500,#64748b);">' + (partida.nombrePartida || "") + '</span>';

        // Destruir select2 y limpiar filas
        destruirSelect2En($modal());
        $c.empty();

        // Determinar filas a renderizar
        // Si hay datos guardados para esta partida úsalos, si no precarga proveedores de la anterior
        var filasBase;
        if (wizardDatos[idx] && wizardDatos[idx].length) {
            filasBase = wizardDatos[idx];
        } else if (idx > 0 && wizardDatos[idx - 1] && wizardDatos[idx - 1].length) {
            // Precargar proveedores de la partida anterior (sin precio)
            filasBase = wizardDatos[idx - 1].map(function (f) {
                return { idProveedor: f.idProveedor, importe: "" };
            });
        } else {
            // Primera partida sin datos previos: 2 filas vacías
            filasBase = [{ idProveedor: 0, importe: "" }, { idProveedor: 0, importe: "" }];
        }

        // Crear filas en el DOM
        var tpl = document.getElementById("tplModalProveedorFila");
        filasBase.forEach(function (fila) {
            $c[0].appendChild(tpl.content.cloneNode(true));
            var $nueva = $c.find(".modal-proveedores-fila").last();
            var $sel = $nueva.find(".modal-proveedores-select");
            $sel.html(buildOpcionesHtml());
            if (fila.idProveedor) $sel.val(fila.idProveedor);
            if (fila.importe) $nueva.find(".modal-proveedores-input-precio").val(fila.importe);
            if (fila.iva) $nueva.find(".modal-proveedores-check-iva").prop("checked", true);
            initSelect2($sel);
        });

        // Botones de navegación
        var $btnAnterior = $("#btnWizardAnterior");
        var $btnSiguiente = $("#btnWizardSiguiente");

        $btnAnterior.toggle(idx > 0);

        if (idx === total - 1) {
            $btnSiguiente.html('Guardar <i class="fa-solid fa-floppy-disk"></i>');
        } else {
            $btnSiguiente.html('Siguiente <i class="fa-solid fa-chevron-right"></i>');
        }

        actualizarOpcionesProveedores();
    }

    // ── Abrir el wizard: cargar partidas y cotizaciones previas ──
    $(document).on("show.bs.modal", "#modalProveedoresRequisicion", function () {
        if (!_idRequiCotizaciones) return;

        wizardPartidas = [];
        wizardDatos = [];
        wizardIdx = 0;

        // 1. Obtener partidas de la requisición
        $.get(urlObtenerPartidas, { idRequisicion: _idRequiCotizaciones }, function (partidas) {
            if (!partidas || !partidas.length) return;
            wizardPartidas = partidas;
            wizardDatos = partidas.map(function () { return []; });

            // 2. Obtener cotizaciones previas para precargar
            $.get(urlObtenerCotizaciones, { idRequisicion: _idRequiCotizaciones }, function (cotizaciones) {
                if (cotizaciones && cotizaciones.length) {
                    // Agrupar cotizaciones por partida usando el índice de wizardPartidas
                    cotizaciones.forEach(function (cot) {
                        var idxPartida = partidas.findIndex(function (p) { return p.idRequiDetalle === cot.idPartida; });
                        if (idxPartida < 0) return;
                        if (!wizardDatos[idxPartida]) wizardDatos[idxPartida] = [];
                        wizardDatos[idxPartida].push({ idProveedor: cot.idProveedor, importe: cot.importe, iva: cot.iva });
                    });
                    // Rellenar partidas sin datos con 2 filas vacías
                    wizardDatos = wizardDatos.map(function (d) {
                        return (d && d.length) ? d : [{ idProveedor: 0, importe: "" }, { idProveedor: 0, importe: "" }];
                    });
                }
                renderizarPartida(0);
            }).fail(function () {
                renderizarPartida(0);
            });
        }).fail(function () {
            Swal.fire({ icon: "error", title: "No se pudieron cargar las partidas." });
        });
    });

    // ── Botón Siguiente / Guardar ──
    $(document).on("click", "#btnWizardSiguiente", function () {
        guardarFilasActuales();
        
        // Validación: Prevenir proveedores duplicados en la partida actual
        var filasActuales = wizardDatos[wizardIdx] || [];
        var provsUnicos = [];
        var hayDuplicado = false;
        
        for (var i = 0; i < filasActuales.length; i++) {
            var provId = filasActuales[i].idProveedor;
            if (provId && provId > 0) {
                if (provsUnicos.indexOf(provId) !== -1) {
                    hayDuplicado = true;
                    break;
                }
                provsUnicos.push(provId);
            }
        }
        
        if (hayDuplicado) {
            Swal.fire({
                icon: "warning",
                title: "Proveedor duplicado",
                text: "No puedes seleccionar el mismo proveedor m\u00e1s de una vez para la misma partida.",
                confirmButtonColor: "#fe6291"
            });
            return;
        }

        if (wizardIdx < wizardPartidas.length - 1) {
            // Avanzar a la siguiente partida
            wizardIdx++;
            renderizarPartida(wizardIdx);
        } else {
            // Última partida: construir payload y guardar
            var cotizaciones = [];
            wizardPartidas.forEach(function (partida, i) {
                var filas = wizardDatos[i] || [];
                filas.forEach(function (fila) {
                    if (fila.idProveedor > 0) {
                        cotizaciones.push({
                            idProveedor: fila.idProveedor,
                            importe: parseFloat(String(fila.importe).replace(/,/g, "")) || 0,
                            idPartida: partida.idRequiDetalle,
                            iva: fila.iva === true
                        });
                    }
                });
            });

            if (!cotizaciones.length) {
                Swal.fire({ icon: "warning", title: "Agrega al menos un proveedor." });
                return;
            }

            $.ajax({
                url: urlGuardarCotizaciones,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ idRequisicion: _idRequiCotizaciones, cotizaciones: cotizaciones }),
                success: function () {
                    window.cerrarSoloModalProveedores();
                    Swal.fire({ icon: "success", title: "Proveedores guardados.", timer: 2000, showConfirmButton: false });
                },
                error: function () {
                    Swal.fire({ icon: "error", title: "Error al guardar los proveedores." });
                }
            });
        }
    });

    // ── Botón Anterior ──
    $(document).on("click", "#btnWizardAnterior", function () {
        guardarFilasActuales();
        wizardIdx--;
        renderizarPartida(wizardIdx);
    });

    // ── Botón Agregar fila ──
    $(document).on("click", "#btnModalProveedoresAgregar", function () {
        var $c = $contenedor();
        var tpl = document.getElementById("tplModalProveedorFila");
        if (!$c.length || !tpl || !tpl.content) return;

        $c[0].appendChild(tpl.content.cloneNode(true));
        var $nueva = $c.find(".modal-proveedores-fila").last();
        var $sel = $nueva.find(".modal-proveedores-select");
        $sel.html(buildOpcionesHtml());
        initSelect2($sel);
        actualizarOpcionesProveedores();
    });

    // ── Botón eliminar fila ──
    $(document).on("click", ".btn-wizard-eliminar-fila", function () {
        var $fila = $(this).closest(".modal-proveedores-fila");
        var $c = $contenedor();
        if ($c.find(".modal-proveedores-fila").length <= 1) return; // mínimo 1 fila
        var $sel = $fila.find(".modal-proveedores-select");
        if ($sel.data("select2")) $sel.select2("destroy");
        $fila.remove();
        actualizarOpcionesProveedores();
    });

    // ── Limpiar select2 al cerrar ──
    $(document).on("hidden.bs.modal", "#modalProveedoresRequisicion", function () {
        destruirSelect2En($(this));
    });
  })();

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
        _idRequiCotizaciones = idMaestro;
        // Limpiar contenedores al abrir
        var archivosReadonly = document.getElementById("contenedorArchivosReadonly");
        if (archivosReadonly) archivosReadonly.innerHTML = "";
        var galeriaFotosDetalle = document.getElementById('galeriaFotosDetalle');
        var seccionFotosDetalle = document.getElementById('seccionFotosDetalle');
        if (galeriaFotosDetalle) galeriaFotosDetalle.innerHTML = '';
        if (seccionFotosDetalle) seccionFotosDetalle.style.display = 'none';

        // Mostrar/ocultar secciones de atender ANTES del $.get (esto no depende de data)
        const seccionesAtender = document.querySelectorAll(".seccionAtender");
        const isAtender = modo === "atender";
        const isReadonly = modo === "readonly";

        seccionesAtender.forEach(function (sec) {
            sec.style.display = (isAtender || isReadonly) ? "block" : "none";
        });

        if (!obtenerDetallesUrl) return;

        $.get(obtenerDetallesUrl, { idMaestro: idMaestro }, function (data) {
            var articulos = data.articulos || [];
            var esDonativo = data.donativo === true;

            // Tabla de artículos
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
                            data: function (params) { return { term: params.term }; },
                            processResults: function (data) { return { results: data }; },
                            cache: true
                        }
                    });
                });
            }

            // Fotos de diseño / Archivos adjuntos
            var seccionFotos = document.getElementById('seccionFotosDetalle');
            var galeriaFotos = document.getElementById('galeriaFotosDetalle');
            if (seccionFotos && galeriaFotos) {
                // Se muestran si hay fotos, sin restringir a Servicio Impresión para que funcione igual al enviar adjuntos comunes
                if (data.fotos && data.fotos.length > 0) {
                    var objsFotos = data.fotos.map(function(r) { return { ruta: r, nombreArchivo: r.split('/').pop() || 'archivo' }; });
                    
                    if (window.ModalAdjuntos && typeof window.ModalAdjuntos.renderGrupoHtml === 'function') {
                        galeriaFotos.innerHTML = window.ModalAdjuntos.renderGrupoHtml('', objsFotos);
                        window.ModalAdjuntos.enlazarEventosContenedor(galeriaFotos);
                    }
                    seccionFotos.style.display = 'block';
                } else {
                    seccionFotos.style.display = 'none';
                    galeriaFotos.innerHTML = '';
                }
            }

            // Subtítulo
            var subtitulo = document.querySelector("#modalDetalle .modal-subtitulo-premium");
            if (subtitulo)
                subtitulo.textContent = "Detalle de partidas solicitadas · Total: " + articulos.length + " partidas";

            // ── Sección atender/readonly: inicializar select2 y precargar valores ──
            if (isAtender || isReadonly) {
                // Destruir instancias previas si existen
                $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").each(function () {
                    if ($(this).data("select2")) $(this).select2("destroy");
                });

                // Inicializar select2
                $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").select2({
                    dropdownParent: $("#modalDetalle"),
                    width: "100%",
                    language: "es",
                });

                // Precargar valores (ahora sí data existe)
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

                // Si es readonly: deshabilitar todo y mostrar archivos
                if (isReadonly) {
                    $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio")
                        .prop("disabled", true);
                    $("#txtObservaciones").prop("readonly", true);
                    if (data.observaciones) {
                        $("#txtObservaciones").val(data.observaciones);
                    }
                    $("#inputCotizaciones, #inputCuadroComparativo").prop("disabled", true);

                    // Ocultar botón enviar
                    var btnEnviar = document.querySelector(".seccionAtender div[style*='text-align:right']");
                    if (btnEnviar) btnEnviar.style.display = "none";

                    document.querySelectorAll(".seccionAtender .row.mb-3").forEach(function (row) {
                        row.style.display = "none";
                    });

                    // Mostrar archivos subidos
                    renderizarArchivosReadonly(data.cotizaciones || [], data.cuadroComparativo || []);

                    (function () {
                        var c = document.getElementById("contenedorArchivosReadonly");
                        if (!c || !window.ModalAdjuntos) return;
                        if (!(data.anexos || []).length) return;
                        var div = document.createElement("div");
                        div.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
                        window.ModalAdjuntos.enlazarEventosContenedor(div);
                        c.appendChild(div);
                    })();
                }
            }

            var modal = new bootstrap.Modal(document.getElementById("modalDetalle"));
            modal.show();
        });
    };

    window.verExpediente = function (idRequi) {
        expedienteActual = idRequi;
        
        // Limpiar
        document.getElementById("expObservaciones").value = "";
        document.getElementById("expArchivosBase").innerHTML = "";
        document.getElementById("expGrupoSiaf").innerHTML = "";
        document.getElementById("expGrupoTablaApi").innerHTML = "";
        document.getElementById("expGrupoNumeroApi").innerHTML = "";
        document.getElementById("expArchivosFinancieros").style.display = "none";
        document.getElementById("expGaleriaFotos").innerHTML = "";
        document.getElementById("expSeccionFotos").style.display = "none";
        document.getElementById("tablaExpedienteBody").innerHTML = "";
        document.getElementById("expedienteSubtitulo").textContent = "Cargando...";

        $.get(obtenerDetallesUrl, { idMaestro: idRequi }, function (data) {

            // Subtítulo
            var articulos = data.articulos || [];
            document.getElementById("expedienteSubtitulo").textContent =
                "Expediente completo · " + articulos.length + " partidas";

            // Tabla artículos
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
            document.getElementById("tablaExpedienteBody").innerHTML = contenido;

            // Fotos
            if (data.tipoServicio === "Servicio Impresion" && data.fotos && data.fotos.length) {
                var galeria = document.getElementById("expGaleriaFotos");
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
                document.getElementById("expSeccionFotos").style.display = "block";
            }

            // Selects PP / FF / Programa / Municipio
            ["expActividad", "expFf", "expTipoPrograma", "expMunicipio"].forEach(function (id) {
                var el = $("#" + id);
                if (el.data("select2")) el.select2("destroy");
            });
            $("#expActividad, #expFf, #expTipoPrograma, #expMunicipio").select2({
                dropdownParent: $("#modalExpediente"),
                width: "100%",
                language: "es"
            }).prop("disabled", true);

            if (data.idPp) $("#expActividad").val(data.idPp).trigger("change");
            if (data.ff) {
                $("#expFf option").filter(function () {
                    return $(this).text().trim() === data.ff;
                }).prop("selected", true);
                $("#expFf").trigger("change");
            }
            if (data.tipoPrograma) {
                $("#expTipoPrograma option").filter(function () {
                    return $(this).text().trim() === data.tipoPrograma;
                }).prop("selected", true);
                $("#expTipoPrograma").trigger("change");
            }
            if (data.claveRegion) $("#expMunicipio").val(data.claveRegion).trigger("change");

            // Cotizaciones / cuadro
            (function () {
                var contenedor = document.getElementById("expArchivosBase");
                contenedor.innerHTML = "";

                // Botón cotizaciones
                var btnCot = document.createElement("div");
                btnCot.style.cssText = "margin-bottom:12px;";
                btnCot.innerHTML =
                    '<button type="button" class="btn boton-rosa" ' +
                    'onclick="verCotizaciones(' + idRequi + ')">' +
                    '<i class="fa-solid fa-file-invoice-dollar"></i> Ver cotizaciones' +
                    '</button>';
                contenedor.appendChild(btnCot);

                // Cuadro comparativo (se mantiene)
                if (window.ModalAdjuntos && (data.cuadroComparativo || []).length > 0) {
                    var divCuadro = document.createElement("div");
                    divCuadro.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                    window.ModalAdjuntos.enlazarEventosContenedor(divCuadro);
                    contenedor.appendChild(divCuadro);
                }

                // Anexos
                if (window.ModalAdjuntos && (data.anexos || []).length > 0) {
                    var divAnexos = document.createElement("div");
                    divAnexos.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
                    window.ModalAdjuntos.enlazarEventosContenedor(divAnexos);
                    contenedor.appendChild(divAnexos);
                }
            })();

            // Observaciones financieros
            document.getElementById("expObservaciones").value = data.observaciones || "";

            // Documentos financieros
            (function () {
                var siaf = data.archivosSiaf || [];
                var tablaApi = data.archivosTablaApi || [];
                var numApi = data.numeroApi || null;
                var hayContenido = siaf.length || tablaApi.length || numApi;
                if (!hayContenido) return;

                document.getElementById("expArchivosFinancieros").style.display = "block";

                function renderGrupoFin(elId, titulo, archivos) {
                    var el = document.getElementById(elId);
                    if (!archivos.length) { el.innerHTML = ""; return; }
                    
                    if (window.ModalAdjuntos) {
                        el.innerHTML = window.ModalAdjuntos.renderGrupoHtml(titulo, archivos);
                        window.ModalAdjuntos.enlazarEventosContenedor(el);
                    } else {
                        el.innerHTML = "";
                    }
                }

                renderGrupoFin("expGrupoSiaf", "Documento SIAF", siaf);
                renderGrupoFin("expGrupoTablaApi", "Tabla de API", tablaApi);

                if (numApi) {
                    document.getElementById("expGrupoNumeroApi").innerHTML =
                        '<label style="font-size:13px;font-weight:600;color:#555;margin-bottom:4px;display:block;">Nº API</label>'
                        + '<span style="font-size:13px;padding:4px 10px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;color:#166534;">'
                        + '<i class="fa-solid fa-hashtag" style="margin-right:4px;"></i>' + numApi + '</span>';
                }
            })();

            expedienteEstatusActual = data.idEstatus || 0;

            expedienteNotaActual = "";
            if (data.idEstatus === 18 && data.observaciones) {
                expedienteNotaActual = data.observaciones;
            }

            expedienteEstatusActual = data.idEstatus || 0;
            expedienteNotaActual = (data.idEstatus === 18 && data.observaciones)
                ? data.observaciones : "";

            $.get(urlObtenerDocsProveedor, { idRequisicion: idRequi }, function (docs) {
                renderChecklist(docs, idRequi, expedienteEstatusActual, expedienteNotaActual);
            });

            new bootstrap.Modal(document.getElementById("modalExpediente")).show();
        });
    };

    window.aceptarExpediente = function () {
        Swal.fire({
            title: "\u00bfAceptar requisici\u00f3n?",
            text: "Se enviar\u00e1 a proceso de pago.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            cancelButtonColor: "var(--slate-500)",
            confirmButtonText: "S\u00ed, aceptar",
            cancelButtonText: "Cancelar"
        }).then(function (result) {
            if (!result.isConfirmed) return;

            // necesitas guardar el id actual al abrir el modal
            $.ajax({
                url: urlAceptarExpediente,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ IdRequisicion: expedienteActual }),
                success: function () {
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalExpediente")
                    ).hide();
                    Swal.fire({
                        icon: "success",
                        title: "Enviado a proceso de pago",
                        timer: 2000,
                        showConfirmButton: false
                    }).then(function () { location.reload(); });
                },
                error: function () {
                    Swal.fire({ icon: "error", title: "Error al procesar la requisici\u00f3n." });
                }
            });
        });
    };

    window.verCotizaciones = function (idRequi) {
        $.get(urlObtenerCotizaciones, { idRequisicion: idRequi }, function (data) {
            var lista = document.getElementById("listaCotizacionesModal");
            lista.innerHTML = "";

            if (!data || !data.length) {
                lista.innerHTML =
                    '<p style="color:var(--color-text-secondary);font-style:italic;font-size:13px;">' +
                    'Sin cotizaciones registradas.</p>';
            } else {
                // Agrupar por partida
                var porPartida = {};
                data.forEach(function (c) {
                    var key = c.idPartida || 0;
                    var label = c.nombrePartida || "Partida #" + key;
                    if (!porPartida[key]) porPartida[key] = { label: label, items: [] };
                    porPartida[key].items.push(c);
                });

                Object.keys(porPartida).forEach(function (key) {
                    var grupo = porPartida[key];

                    // Encabezado de partida
                    var header = document.createElement("div");
                    header.style.cssText =
                        "font-size:12px;font-weight:600;color:var(--color-text-secondary);" +
                        "text-transform:uppercase;letter-spacing:.04em;" +
                        "padding:8px 4px 4px;border-bottom:1px solid var(--color-border-tertiary);" +
                        "margin-bottom:4px;" +
                        (Object.keys(porPartida).indexOf(key) > 0 ? "margin-top:12px;" : "");
                    header.innerHTML =
                        '<i class="fa-solid fa-tag" style="margin-right:5px;font-size:10px;"></i>' +
                        grupo.label;
                    lista.appendChild(header);

                    // Filas de proveedores de esa partida
                    grupo.items.forEach(function (c, i) {
                        var importe = parseFloat(c.importe || 0).toLocaleString("es-MX", {
                            style: "currency", currency: "MXN"
                        });
                        var fila = document.createElement("div");
                        fila.style.cssText =
                            "display:flex;align-items:center;gap:12px;padding:8px 14px;" +
                            "border-radius:8px;border:1px solid var(--color-border-tertiary);" +
                            "background:var(--color-background-secondary);margin-bottom:4px;";
                        fila.innerHTML =
                            '<span style="font-size:12px;color:var(--color-text-secondary);' +
                            'min-width:20px;text-align:center;">#' + (i + 1) + '</span>' +
                            '<span style="flex:1;font-size:13px;font-weight:500;">' +
                            (c.nombreProveedor || "Proveedor #" + c.idProveedor) +
                            '</span>' +
                            '<span style="font-size:13px;color:var(--color-text-success);font-weight:500;">' +
                            importe +
                            '</span>';
                        lista.appendChild(fila);
                    });
                });
            }

            new bootstrap.Modal(document.getElementById("modalVerCotizaciones")).show();
        }).fail(function () {
            Swal.fire({ icon: "error", title: "No se pudieron cargar las cotizaciones." });
        });
    };

    window.abrirModalProveedoresSinCerrar = function () {
        // Abre el modal de proveedores de forma manual sin que Bootstrap
        // interfiera con el modal de detalle que ya está abierto
        var el = document.getElementById("modalProveedoresRequisicion");
        if (!el) return;

        // Evitar doble instancia
        var instancia = bootstrap.Modal.getInstance(el);
        if (instancia) {
            instancia.show();
        } else {
            var modal = new bootstrap.Modal(el, {
                backdrop: false,   // sin backdrop extra para no apilar dos oscurecimientos
                keyboard: false
            });
            modal.show();
        }
    };

    window.cerrarSoloModalProveedores = function () {
        var el = document.getElementById("modalProveedoresRequisicion");
        var instancia = bootstrap.Modal.getInstance(el);
        if (instancia) instancia.hide();
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

})();