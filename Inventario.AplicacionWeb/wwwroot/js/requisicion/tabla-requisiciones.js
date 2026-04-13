
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
        { clave: "CompDomicilio", label: "Comprobante de Domicilio" }
    ];

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
            title: "¿Enviar a financieros?",
            text: "Se enviarán todos los documentos para revisión.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            confirmButtonText: "Sí, enviar",
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
                    title: 'Enviada a modificación'
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
        var observaciones = document.getElementById("txtObservaciones").value.trim();
        var idpp = parseInt($("#actividadSeleccionada").val()) || 0;
        var ff = $("#ffSelect").find("option:selected").text().trim();
        var tipoPrograma = $("#tipoProgramaSelect").find("option:selected").text().trim();
        var claveRegion = parseInt($("#municipio").val()) || 0;

        if (!observaciones) { alert("Debe escribir una observación."); return; }
        if (!idpp) { alert("Debe seleccionar una actividad."); return; }
        if (!$("#ffSelect").val()) { alert("Debe seleccionar una fuente de financiamiento."); return; }
        if (!$("#tipoProgramaSelect").val()) { alert("Debe seleccionar un tipo de programa."); return; }
        if (!claveRegion) { alert("Debe seleccionar un municipio."); return; }

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
                        alert("La atención se guardó, pero hubo un error al subir los archivos.");
                        location.reload();
                    }
                });
            },
            error: function () {
                alert("Error al atender la requisición.");
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
    var MAX_FILAS_PROVEEDORES = 6;
    var FILAS_INICIALES_PROVEEDORES = 2;
    var CATALOGO_PROVEEDORES_MODAL = [];
    for (var c = 1; c <= 10; c++) {
      CATALOGO_PROVEEDORES_MODAL.push({ v: String(c), t: "Proveedor " + c });
    }

    function $modalProveedores() {
      return $("#modalProveedoresRequisicion");
    }

    function $contenedorFilas() {
      return $("#modalProveedoresFilas");
    }

    function destruirSelect2ProveedoresEn($root) {
      $root.find(".modal-proveedores-select").each(function () {
        var $s = $(this);
        if ($s.data("select2")) $s.select2("destroy");
      });
    }

    function buildFullOptionsHtml() {
      var html = '<option value="">-- Seleccione proveedor --</option>';
      CATALOGO_PROVEEDORES_MODAL.forEach(function (p) {
        html += "<option value=\"" + p.v + "\">" + p.t + "</option>";
      });
      return html;
    }

    function initSelect2Proveedor($sel) {
      if (!$sel.length || typeof $.fn.select2 === "undefined") return;
      $sel.select2({
        dropdownParent: $modalProveedores(),
        width: "100%",
        language: "es",
      });
    }

    function sincronizarOpcionesProveedores() {
      var $c = $contenedorFilas();
      if (!$c.length) return;
      var $filas = $c.find(".modal-proveedores-fila");
      var values = $filas
        .map(function () {
          return $(this).find(".modal-proveedores-select").val() || "";
        })
        .get();

      $filas.each(function (idx) {
        var $sel = $(this).find(".modal-proveedores-select");
        var current = values[idx] || "";
        if ($sel.data("select2")) $sel.select2("destroy");

        var tomadosPorOtros = {};
        values.forEach(function (v, j) {
          if (j !== idx && v) tomadosPorOtros[v] = true;
        });

        var html = '<option value="">-- Seleccione proveedor --</option>';
        CATALOGO_PROVEEDORES_MODAL.forEach(function (p) {
          if (!tomadosPorOtros[p.v] || p.v === current) {
            html += "<option value=\"" + p.v + "\">" + p.t + "</option>";
          }
        });
        $sel.html(html);
        if (current && $sel.find('option[value="' + current + '"]').length) {
          $sel.val(current);
        } else {
          $sel.val("");
        }
        initSelect2Proveedor($sel);
      });
    }

    function actualizarBotonAgregarProveedores() {
      var $c = $contenedorFilas();
      var $btn = $("#btnModalProveedoresAgregar");
      if (!$c.length || !$btn.length) return;
      var n = $c.find(".modal-proveedores-fila").length;
      $btn.prop("disabled", n >= MAX_FILAS_PROVEEDORES);
    }

    function resetModalProveedores() {
      var $m = $modalProveedores();
      var $c = $contenedorFilas();
      if (!$m.length || !$c.length) return;

      destruirSelect2ProveedoresEn($m);
      while ($c.find(".modal-proveedores-fila").length > FILAS_INICIALES_PROVEEDORES) {
        $c.find(".modal-proveedores-fila").last().remove();
      }
      $c.find(".modal-proveedores-fila").each(function () {
        $(this).find(".modal-proveedores-input-precio").val("");
        var $sel = $(this).find(".modal-proveedores-select");
        $sel.html(buildFullOptionsHtml());
        $sel.val("");
      });
      sincronizarOpcionesProveedores();
      actualizarBotonAgregarProveedores();
    }

    $(document).on("shown.bs.modal", "#modalProveedoresRequisicion", function () {
      resetModalProveedores();
    });

    $(document).on("hidden.bs.modal", "#modalProveedoresRequisicion", function () {
      destruirSelect2ProveedoresEn($(this));
    });

    $(document).on(
      "select2:select select2:clear",
      "#modalProveedoresRequisicion .modal-proveedores-select",
      function () {
        sincronizarOpcionesProveedores();
      },
    );

    $(document).on("click", "#btnModalProveedoresAgregar", function () {
      var $c = $contenedorFilas();
      var tpl = document.getElementById("tplModalProveedorFila");
      if (!$c.length || !tpl || !tpl.content) return;
      if ($c.find(".modal-proveedores-fila").length >= MAX_FILAS_PROVEEDORES) return;
      $c[0].appendChild(tpl.content.cloneNode(true));
      sincronizarOpcionesProveedores();
      actualizarBotonAgregarProveedores();
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
                if (!window.ModalAdjuntos) { contenedor.innerHTML = ""; return; }

                var html = "";
                if ((data.cotizaciones || []).length)
                    html += window.ModalAdjuntos.renderGrupoHtml("Cotizaciones", data.cotizaciones);
                if ((data.cuadroComparativo || []).length)
                    html += window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                if ((data.anexos || []).length)
                    html += window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);

                contenedor.innerHTML = html;
                window.ModalAdjuntos.enlazarEventosContenedor(contenedor);
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
            title: "¿Aceptar requisición?",
            text: "Se enviará a proceso de pago.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            cancelButtonColor: "var(--slate-500)",
            confirmButtonText: "Sí, aceptar",
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
                    Swal.fire({ icon: "error", title: "Error al procesar la requisición." });
                }
            });
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

})();
