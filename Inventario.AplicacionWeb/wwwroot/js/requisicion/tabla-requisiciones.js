(function () {
  var container = document.querySelector(".tabla-requi-page");
  var esTablaServicios =
    container?.getAttribute("data-tipo-tabla") === "servicios";
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
  var urlBuscarCogs = container
    ? container.getAttribute("data-url-buscar-cogs")
    : "";
  var urlEnviarAlmacen = container
    ? container.getAttribute("data-url-enviar-almacen")
    : "";
  var urlRechazar = container
    ? container.getAttribute("data-url-rechazar")
    : "";
  var urlModificar = container
    ? container.getAttribute("data-url-modificar")
    : "";
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
    ? container.getAttribute("data-url-subir-doc-proveedor")
    : "";
  var urlObtenerDocsProveedor = container
    ? container.getAttribute("data-url-obtener-docs-proveedor")
    : "";
  var urlEnviarFinancierosDocs = container
    ? container.getAttribute("data-url-enviar-financieros-docs")
    : "";
  var urlRebotarDocumentos = container
    ? container.getAttribute("data-url-rebotar-documentos")
    : "";
  var urlDescargarCuadroComparativo = container
    ? container.getAttribute("data-url-descargar-cuadro-comparativo")
    : "";

  var urlGuardarCotizaciones = container
    ? container.getAttribute("data-url-guardar-cotizaciones")
    : "";
  var urlObtenerCotizaciones = container
    ? container.getAttribute("data-url-obtener-cotizaciones")
    : "";
  var urlObtenerPartidas = container
    ? container.getAttribute("data-url-obtener-partidas")
    : "";
  var urlObtenerOpcionesGanador = container
    ? container.getAttribute("data-url-obtener-opciones-ganador")
    : "";
  var urlObtenerGanador = container
    ? container.getAttribute("data-url-obtener-ganador")
    : "";
  var urlGuardarGanador = container
    ? container.getAttribute("data-url-guardar-ganador")
    : "";

  var urlFinalizar = container
    ? container.getAttribute("data-url-finalizar")
    : "";

  var urlSubirDocumentoPedido = container
    ? container.getAttribute("data-url-subir-documento-pedido")
    : "";

  var urlObtenerArchivos = container
    ? container.getAttribute("data-url-obtener-archivos")
    : "";
  var urlObtenerTodosArchivos = container
    ? container.getAttribute("data-url-obtener-todos-archivos")
    : "";
  var urlDescargarTodosArchivosZip = container
    ? container.getAttribute("data-url-descargar-todos-archivos-zip")
        : "";
    var urlDescargarReqDirecta = container
        ? container.getAttribute("data-url-descargar-req-directa")
        : "";
    var urlObtenerIdAdquisicion = container
        ? container.getAttribute("data-url-obtener-id-adquisicion")
        : "";
    var urlListarConsolidadas = container
        ? container.getAttribute("data-url-listar-consolidadas")
        : "";
    var urlConsolidadasAutorizadas = container
        ? container.getAttribute("data-url-consolidadas-autorizadas")
        : "";
    var urlConsolidadasDocumentos = container
        ? container.getAttribute("data-url-consolidadas-documentos")
        : "";
    var urlObtenerArchivosConsolidada = container
        ? container.getAttribute("data-url-obtener-archivos-consolidada")
        : "";
    var urlDescargarArchivosConsolidadaZip = container
        ? container.getAttribute("data-url-descargar-archivos-consolidada-zip")
        : "";
    var urlDetalleConsolidada = container
        ? container.getAttribute("data-url-detalle-consolidada")
        : "";

    var urlAtenderConsolidada = container
        ? container.getAttribute("data-url-atender-consolidada") : "";
    var urlSubirArchivosConsolidada = container
        ? container.getAttribute("data-url-subir-archivos-consolidada") : "";
    var urlPartidasConsolidada = container
        ? container.getAttribute("data-url-partidas-consolidada") : "";

    var urlObtenerOpcionesGanadorConsolidada = container
        ? container.getAttribute("data-url-obtener-opciones-ganador-consolidada")
        : "";
    var urlDescargarCuadroConsolidada = container
        ? container.getAttribute("data-url-descargar-cuadro-consolidada")
        : "";
    var urlDescargarReqDirectaConsolidada = container
        ? container.getAttribute("data-url-descargar-req-directa-consolidada")
        : "";

    window._modoConsolidada = false;
    window._idConsolidadaWizard = null;
    window._idConsolidadaAtender = null;

    var urlConsolidadasVerificadas = container
        ? container.getAttribute("data-url-consolidadas-verificadas")
        : "";
    var urlExpedienteConsolidada = container
        ? container.getAttribute("data-url-expediente-consolidada")
        : "";
    var urlSubirDocProveedorConsolidada = container
        ? container.getAttribute("data-url-subir-doc-proveedor-consolidada")
        : "";
    var urlObtenerDocsProveedorConsolidada = container
        ? container.getAttribute("data-url-obtener-docs-proveedor-consolidada")
        : "";
    var urlEditarPedidoConsolidada = container
        ? container.getAttribute("data-url-editar-pedido-consolidada")
        : "";
    var urlGenerarPedidoPdfConsolidada = container
        ? container.getAttribute("data-url-generar-pedido-pdf-consolidada")
        : "";
    var urlEnviarFinancierosDocsConsolidada = container
        ? container.getAttribute("data-url-enviar-financieros-docs-consolidada")
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
    { clave: "MemoPago", label: "Memorandum Instrucción de Pago" },
  ];

  var CACHE_PARTIDAS = [];
  var CACHE_PARTIDAS_REQUI = null;

  function escapeHtml(value) {
    return String(value ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function obtenerBadgeEstatusHtml(idEstatus, nombreEstatus) {
    var nombreSafe = escapeHtml(nombreEstatus || "Desconocido");
    var colorClass = "badge-estado-info";
    var iconClass =
      idEstatus === 1
        ? "fa-solid fa-file-signature"
        : "fa-solid fa-spinner fa-spin-pulse";

    switch (idEstatus) {
      case 7:
      case 12:
      case 17:
        colorClass = "badge-estado-success";
        iconClass = "fa-solid fa-check-double";
        break;
      case 4:
      case 10:
      case 15:
      case 16:
        colorClass = "badge-estado-purple";
        iconClass = "fa-solid fa-user-check";
        break;
      case 5:
      case 6:
        colorClass = "badge-estado-danger";
        iconClass = "fa-solid fa-ban";
        break;
      case 3:
      case 18:
        colorClass = "badge-estado-warning";
        iconClass = "fa-solid fa-triangle-exclamation";
        break;
      case 1:
      case 2:
      case 9:
      case 11:
      case 13:
      case 14:
      default:
        colorClass = "badge-estado-info";
        iconClass =
          idEstatus === 1
            ? "fa-solid fa-file-signature"
            : "fa-solid fa-spinner fa-spin-pulse";
        break;
    }

    return (
      '<div class="badge-estado-premium ' +
      colorClass +
      '" title="' +
      nombreSafe +
      '">' +
      '<i class="' +
      iconClass +
      '"></i><span class="badge-text">' +
      nombreSafe +
      "</span></div>"
    );
  }

  function renderTextoTablaPrincipal(texto, icono, secundaria) {
    var principal = escapeHtml(texto || "—");
    var detalle = secundaria ? escapeHtml(secundaria) : "";

    return (
      '<div class="tabla-meta-stack">' +
      '<span class="tabla-meta-principal"><i class="' +
      icono +
      '"></i>' +
      principal +
      "</span>" +
      (detalle
        ? '<span class="tabla-meta-secundaria">' + detalle + "</span>"
        : "") +
      "</div>"
    );
  }

  function renderFolioTabla(texto, secundaria) {
    var folio = escapeHtml(texto || "—");
    var detalle = secundaria ? escapeHtml(secundaria) : "";

    return (
      '<div class="tabla-meta-stack">' +
      '<span class="folio-badge">' +
      folio +
      "</span>" +
      (detalle
        ? '<span class="tabla-meta-secundaria">' + detalle + "</span>"
        : "") +
      "</div>"
    );
  }

  function actualizarEstadoProveedoresSeleccionados(tieneCotizaciones) {
    var mensaje = document.getElementById("estadoProveedoresSeleccionados");
    var textoBoton = document.getElementById("textoBtnProveedores");
    var btnCuadro = document.getElementById("btnDescargarCuadroComparativo");

    if (mensaje) {
      mensaje.style.display = tieneCotizaciones ? "block" : "none";
    }

    if (textoBoton) {
      textoBoton.textContent = tieneCotizaciones
        ? "Editar proveedores"
        : "Seleccionar Proveedores";
    }

    if (btnCuadro) {
      btnCuadro.disabled = !tieneCotizaciones;
      btnCuadro.title = tieneCotizaciones
        ? "Descargar cuadro comparativo"
        : "Disponible cuando se hayan seleccionado los proveedores";
    }
  }

  function cargarEstadoProveedoresSeleccionados(idRequisicion) {
    if (!idRequisicion || !urlObtenerCotizaciones) {
      actualizarEstadoProveedoresSeleccionados(false);
      return;
    }

    $.get(
      urlObtenerCotizaciones,
      { idRequisicion: idRequisicion },
      function (cotizaciones) {
        actualizarEstadoProveedoresSeleccionados(
          !!(cotizaciones && cotizaciones.length),
        );
      },
    ).fail(function () {
      actualizarEstadoProveedoresSeleccionados(false);
    });
  }

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

  var wizardOpciones = [];
  var wizardIdGanador = 0;
  var wizardGanadorManual = false;

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
  var paginaPorTab = {
    principal: 1,
    autorizadas: 1,
    rechazadas: 1,
      verificadas: 1,
      consolidadas: 1,
      documentos: 1
  };

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
                    : panel.id === "tab-consolidadas"
                        ? "consolidadas"
            : panel.id === "tab-verificadas"
              ? "verificadas"
              : panel.id === "tab-documentos"
                ? "documentos"
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
        document.getElementById("filtroEstado").value) ||
      ""
    )
      .toLowerCase()
      .trim();

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

    var todasLasFilas = [].slice
      .call(ctx.tbody.querySelectorAll("tr"))
      .filter(function (tr) {
        return (
          !tr.classList.contains("fila-vacia") &&
          !tr.classList.contains("fila-detalle")
        );
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
        if (siguiente && siguiente.classList.contains("fila-detalle"))
          siguiente.style.display = "none";
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
      if (siguiente && siguiente.classList.contains("fila-detalle"))
        siguiente.style.display = visible ? "" : "none";
    });

    mostrarMensajeVacio(total, ctx.tbody, todasLasFilas.length);
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
    [
      "pasoOpciones",
      "pasoAsignar",
      "pasoAlmacen",
      "pasoRechazar",
      "pasoModificar",
    ].forEach(function (id) {
      var el = document.getElementById(id);
      if (el) el.style.display = "none";
    });
  }

  function obtenerOpcionesPartidasHtml(callback) {
    var buildHtml = function (data) {
      var html = '<option value="">-- Seleccione partida --</option>';
      data.forEach(function (p) {
        html +=
          '<option value="' +
          p.idRequiDetalle +
          '">' +
          p.nombrePartida +
          "</option>";
      });
      callback(html);
    };

    if (
      CACHE_PARTIDAS_REQUI === _idRequiCotizaciones &&
      CACHE_PARTIDAS.length
    ) {
      buildHtml(CACHE_PARTIDAS);
    } else {
      $.get(
        urlObtenerPartidas,
        { idRequisicion: _idRequiCotizaciones },
        function (data) {
          CACHE_PARTIDAS_REQUI = _idRequiCotizaciones;
          CACHE_PARTIDAS = data || [];
          buildHtml(data);
        },
      );
    }
  }

    function mostrarPasoGanador() {
        $("#wizardProveedorActual").hide();
        $("#modalProveedoresFilas").hide();
        $("#btnModalProveedoresAgregar").hide();
        $("#btnWizardSiguiente").hide();
        $("#btnWizardAnterior").hide();
        document.getElementById("wizardProveedoresSubtitulo").textContent =
            "Selecciona el proveedor ganador";

        asegurarControlesGanador();

        // ── FIX: determinar idRequisicion según modo ──
        var idReqGanador = null;
        if (window._modoConsolidada) {
            // Sacar la primera requisición del mapa de partidas consolidadas
            for (var i = 0; i < CACHE_PARTIDAS.length; i++) {
                var mapa = CACHE_PARTIDAS[i].idRequisicionPorDetalle;
                if (mapa) {
                    var vals = Object.values(mapa);
                    if (vals.length) {
                        idReqGanador = vals[0];
                        break;
                    }
                }
            }
        } else {
            idReqGanador = _idRequiCotizaciones;
        }

        if (!idReqGanador) {
            Swal.fire({ icon: "error", title: "No se pudo determinar la requisición para el ganador." });
            return;
        }

        $.get(urlObtenerOpcionesGanador, { idRequisicion: idReqGanador }, function (opciones) {
            wizardOpciones = opciones || [];

            $.get(urlObtenerGanador, { idRequisicion: idReqGanador }, function (ganador) {
                var sugerido = wizardOpciones.find(function (op) { return op.esSugerido; });

                if (ganador && ganador.idProveedor) {
                    wizardIdGanador = ganador.idProveedor;
                    $("#wizardGanadorJustificacion").val(ganador.justificacion || "");
                } else {
                    wizardIdGanador = sugerido ? sugerido.idProveedor : 0;
                    $("#wizardGanadorJustificacion").val("");
                }

                pintarOpcionesGanador();
                $("#wizardPasoGanador").show();
            }).fail(function () {
                var sugerido = wizardOpciones.find(function (op) { return op.esSugerido; });
                wizardIdGanador = sugerido ? sugerido.idProveedor : 0;
                $("#wizardGanadorJustificacion").val("");
                pintarOpcionesGanador();
                $("#wizardPasoGanador").show();
            });
        }).fail(function () {
            Swal.fire({ icon: "error", title: "Error al cargar opciones de ganador." });
            $("#wizardProveedorActual").show();
            $("#btnModalProveedoresAgregar").show();
            $("#btnWizardSiguiente").show();
            $("#btnWizardAnterior").show();
        });
    }

  function confirmarGanadorFinal() {
    if (!wizardIdGanador) {
      Swal.fire({ icon: "warning", title: "Selecciona el proveedor ganador." });
      return;
    }
    $.ajax({
      url: urlGuardarGanador,
      type: "POST",
      contentType: "application/json",
      data: JSON.stringify({
        idRequisicion: _idRequiCotizaciones,
        idProveedor: wizardIdGanador,
        seleccionManual: wizardGanadorManual,
      }),
      success: function () {
        window.cerrarSoloModalProveedores();
        Swal.fire({
          icon: "success",
          title: "Proveedores y ganador guardados.",
          confirmButtonText: "Aceptar",
        });
      },
      error: function () {
        Swal.fire({ icon: "error", title: "Error al guardar el ganador." });
      },
    });
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

    function renderChecklist(docsSubidos, idRequi, idEstatus, notaObservacion, esConsolidada) {
        esConsolidada = esConsolidada || false;
        var idReal = esConsolidada ? idRequi.replace("cons_", "") : idRequi;
        var idHtml = esConsolidada ? "'" + idRequi + "'" : idRequi;
        var notaEl = document.getElementById("expNotaObservacion");
        if (notaObservacion && idEstatus === 18) {
            notaEl.textContent = "Financieros observaron: " + notaObservacion;
            notaEl.style.display = "block";
        } else {
            notaEl.style.display = "none";
        }

        var clavesSubidas = docsSubidos.map(function (d) { return d.nombreArchivo; });

        // Extraer qué docs están observados de la nota de financieros
        var clavesObservadas = [];
        if (idEstatus === 18 && notaObservacion) {
            DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
                if (notaObservacion.indexOf(doc.label) !== -1) {
                    clavesObservadas.push(doc.clave);
                }
            });
        }

        // Inicializar estado "No aplica" si no existe para esta requisición
        if (!window._noAplica) window._noAplica = {};
        if (!window._noAplica[idRequi]) window._noAplica[idRequi] = {};

        DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
            var subido = clavesSubidas.indexOf(doc.clave) !== -1;
            var observado = clavesObservadas.indexOf(doc.clave) !== -1;

            // Solo precargar si el usuario no ha interactuado aún (undefined = intacto)
            if (window._noAplica[idRequi][doc.clave] === undefined) {
                // No está subido y no está observado → asumir "No aplica"
                window._noAplica[idRequi][doc.clave] = !subido && !observado;
            }

            // Si financieros lo observó pero estaba marcado como "No aplica" → forzar a false
            if (observado && window._noAplica[idRequi][doc.clave]) {
                window._noAplica[idRequi][doc.clave] = false;
            }
        });

        var checklist = document.getElementById("expChecklistDocs");
        checklist.innerHTML = "";

        var todosResueltos = true;

        DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
            var subido = clavesSubidas.indexOf(doc.clave) !== -1;
            var noAplica = window._noAplica[idRequi][doc.clave] === true;
            var observado = clavesObservadas.indexOf(doc.clave) !== -1;

            if (!subido && !noAplica) todosResueltos = false;

            var archivo = docsSubidos.find(function (d) { return d.nombreArchivo === doc.clave; });
            var esObservado = observado && !subido;

            var borderColor = esObservado ? "#fecaca"
                : noAplica ? "#e2e8f0"
                    : subido ? "#bbf7d0"
                        : "var(--color-border-tertiary)";
            var bgColor = esObservado ? "#fef2f2"
                : noAplica ? "#f8fafc"
                    : subido ? "#f0fdf4"
                        : "var(--color-background-secondary)";

            var fila = document.createElement("div");
            fila.style.cssText =
                "display:flex; align-items:center; gap:10px; padding:8px 12px;" +
                "border-radius:8px; border:1px solid " + borderColor + ";" +
                "background:" + bgColor + ";";

            var icono;
            if (esObservado) {
                icono = '<i class="fa-solid fa-circle-exclamation" style="color:#dc2626;font-size:16px;flex-shrink:0;"></i>';
            } else if (noAplica) {
                icono = '<i class="fa-solid fa-minus-circle" style="color:#94a3b8;font-size:16px;flex-shrink:0;"></i>';
            } else if (subido) {
                icono = '<i class="fa-solid fa-circle-check" style="color:#16a34a;font-size:16px;flex-shrink:0;"></i>';
            } else {
                icono = '<i class="fa-regular fa-circle" style="color:#9ca3af;font-size:16px;flex-shrink:0;"></i>';
            }

            var linkArchivo = subido && archivo
                ? '<a href="' + archivo.ruta + '" target="_blank" ' +
                'style="font-size:11px;color:var(--color-text-secondary);text-decoration:none;">' +
                '<i class="fa-solid fa-eye"></i> Ver</a>'
                : "";

            var btnSubir = "";
            if ((idEstatus === 15 || idEstatus === 18) && !noAplica) {
                btnSubir =
                    '<label style="cursor:pointer;flex-shrink:0;">' +
                    '<input type="file" style="display:none;" ' +
                    'onchange="subirDocProveedor(' + idHtml + ", '" + doc.clave + "', this, " + (esConsolidada ? "true" : "false") + ')\">"' +
                    '<span style="font-size:11px;color:var(--color-text-secondary);' +
                    "padding:3px 8px;border:1px solid var(--color-border-secondary);" +
                    'border-radius:6px;white-space:nowrap;">' +
                    (subido
                        ? '<i class="fa-solid fa-arrow-rotate-right"></i> Reemplazar'
                        : '<i class="fa-solid fa-upload"></i> Subir') +
                    "</span></label>";
            }

            var chkNoAplica = "";
            if ((idEstatus === 15 || idEstatus === 18) && !subido) {
                chkNoAplica =
                    '<label style="display:flex;align-items:center;gap:4px;cursor:pointer;' +
                    'font-size:11px;color:var(--color-text-secondary);flex-shrink:0;white-space:nowrap;">' +
                    '<input type="checkbox" ' +
                    (noAplica ? "checked " : "") +
                    'style="width:14px;height:14px;cursor:pointer;accent-color:#94a3b8;" ' +
                    'onchange="toggleNoAplica(' + idHtml + ", '" + doc.clave + "', this, " + idEstatus + ", '" +
                    (notaObservacion || "").replace(/'/g, "\\'") + '\')"> No aplica</label>';
            }

            fila.innerHTML =
                icono +
                '<span style="font-size:13px;flex:1;">' + doc.label + "</span>" +
                linkArchivo +
                chkNoAplica +
                btnSubir;

            checklist.appendChild(fila);
        });

        var acciones = document.getElementById("expAccionesProveedor");
        if (idEstatus === 15 || idEstatus === 18) {
            acciones.style.display = todosResueltos ? "block" : "none";
        } else {
            acciones.style.display = "none";
        }
    }

    window.toggleNoAplica = function (idRequi, clave, checkbox, idEstatus, notaObservacion) {
        if (!window._noAplica) window._noAplica = {};
        if (!window._noAplica[idRequi]) window._noAplica[idRequi] = {};

        window._noAplica[idRequi][clave] = checkbox.checked;

        var esCons = typeof idRequi === "string" && idRequi.startsWith("cons_");
        var idReal = esCons ? idRequi.replace("cons_", "") : idRequi;

        if (esCons) {
            $.get(urlObtenerDocsProveedorConsolidada, { idConsolidada: idReal }, function (docs) {
                renderChecklist(docs, idRequi, idEstatus, notaObservacion, true);
            });
        } else {
            $.get(urlObtenerDocsProveedor, { idRequisicion: idReal }, function (docs) {
                renderChecklist(docs, idRequi, idEstatus, notaObservacion);
            });
        }
    };

  window.subirDocProveedor = function (idParam, clave, inputEl, esConsolidada) {
    var archivo = inputEl.files[0];
    if (!archivo) return;
    esConsolidada = esConsolidada || false;

    var formData = new FormData();
    var idReal = esConsolidada ? idParam.replace("cons_", "") : idParam;

    if (esConsolidada) {
      formData.append("idConsolidada", idReal);
      formData.append("tipoDocumento", clave);
      formData.append("archivo", archivo);

      $.ajax({
        url: urlSubirDocProveedorConsolidada,
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function () {
          $.get(urlObtenerDocsProveedorConsolidada, { idConsolidada: idReal }, function (docs) {
            renderChecklist(docs, idParam, expedienteEstatusActual, expedienteNotaActual, true);
          });
        },
        error: function () {
          Swal.fire({ icon: "error", title: "Error al subir el archivo" });
        },
      });
    } else {
      formData.append("idRequisicion", idReal);
      formData.append("tipoDocumento", clave);
      formData.append("archivo", archivo);

      $.ajax({
        url: urlSubirDocProveedor,
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function () {
          $.get(urlObtenerDocsProveedor, { idRequisicion: idReal }, function (docs) {
            renderChecklist(docs, idParam, expedienteEstatusActual, expedienteNotaActual);
          });
        },
        error: function () {
          Swal.fire({ icon: "error", title: "Error al subir el archivo" });
        },
      });
    }
  };

  window.enviarDocumentosAFinancieros = function () {
    var esConsolidada = window._modoConsolidada && window._idConsolidadaExpediente;
    var id = esConsolidada ? window._idConsolidadaExpediente : window._expedienteActualGlobal;

    if (!id) {
      Swal.fire({ icon: "error", title: "No se pudo identificar la requisición." });
      return;
    }

    Swal.fire({
      title: "\u00bfEnviar a financieros?",
      text: esConsolidada
        ? "Se enviar\u00e1n los documentos de la consolidada para revisi\u00f3n."
        : "Se enviar\u00e1n todos los documentos para revisi\u00f3n.",
      icon: "question",
      showCancelButton: true,
      confirmButtonColor: "#fe6291",
      confirmButtonText: "S\u00ed, enviar",
      cancelButtonText: "Cancelar",
    }).then(function (result) {
      if (!result.isConfirmed) return;

      var ajaxOpts;

      if (esConsolidada) {
        var formData = new FormData();
        formData.append("idConsolidada", id);
        var archivoInput = document.getElementById("inputSubirPedido");
        var archivo = archivoInput ? archivoInput.files[0] : null;
        if (archivo) {
          formData.append("archivo", archivo);
        }
        ajaxOpts = {
          url: urlEnviarFinancierosDocsConsolidada,
          type: "POST",
          data: formData,
          processData: false,
          contentType: false
        };
      } else {
        var formData = new FormData();
        formData.append("idRequisicion", id);
        var archivoInput = document.getElementById("inputSubirPedido");
        var archivo = archivoInput ? archivoInput.files[0] : null;
        if (archivo) {
          formData.append("archivo", archivo);
        }
        ajaxOpts = {
          url: urlEnviarFinancierosDocs,
          type: "POST",
          data: formData,
          processData: false,
          contentType: false
        };
      }

      Swal.fire({
        title: "Enviando...",
        allowOutsideClick: false,
        didOpen: function () {
          Swal.showLoading();
        },
      });

      $.ajax(ajaxOpts)
        .done(function (res) {
          if (res && typeof res === "object" && res.success) {
            bootstrap.Modal.getInstance(
              document.getElementById("modalExpediente"),
            ).hide();
            Swal.fire({
              icon: "success",
              title: "Enviado a financieros",
              timer: 2000,
              showConfirmButton: false,
            }).then(function () {
              location.reload();
            });
          } else {
            var msg =
              (res && res.message) ||
              "No se pudo completar el env\u00edo. Verifica los documentos.";
            Swal.fire({ icon: "error", title: msg });
          }
        })
        .fail(function (jqXHR) {
          var mensaje = "Error al enviar a financieros.";
          if (jqXHR && jqXHR.responseText) {
            try {
              var parsed = JSON.parse(jqXHR.responseText);
              if (parsed && parsed.message) mensaje = parsed.message;
            } catch (e) {}
          }
          Swal.fire({ icon: "error", title: mensaje });
        });
    });
  };

  var _idRequiFinalizar = null;

  window.abrirModalFinalizar = function (idRequi) {
    _idRequiFinalizar = idRequi;
    document.getElementById("txtObservacionesFinalizar").value = "";

    var el = document.getElementById("modalFinalizar");
    var instancia = bootstrap.Modal.getInstance(el);
    if (instancia) {
      instancia.show();
    } else {
      new bootstrap.Modal(el, {
        backdrop: false,
        keyboard: true,
      }).show();
    }
    };

    window.verDetalleConsolidada = function (idConsolidada) {
        document.getElementById("consolidadaTitulo").textContent = "Requisición Consolidada";
        document.getElementById("consolidadaSubtitulo").textContent = "Cargando...";
        document.getElementById("consolidadaHijas").innerHTML = "";
        document.getElementById("consolidadaArticulosBody").innerHTML = "";

        var modal = new bootstrap.Modal(
            document.getElementById("modalDetalleConsolidada")
        );
        modal.show();

        $.get(urlDetalleConsolidada, { idConsolidada: idConsolidada }, function (data) {

            document.getElementById("consolidadaTitulo").textContent =
                data.folioConsolidada;
            document.getElementById("consolidadaSubtitulo").textContent =
                data.estatus + " · Creada el " + data.fechaCreacion + " por " + data.creadoPor;

            // ── Cards hijas ──────────────────────────────────────
            var hijasHtml = "";
            (data.requisiciones || []).forEach(function (r) {
                hijasHtml +=
                    '<div style="display:flex;flex-direction:column;gap:4px;padding:10px 14px;' +
                    'border-radius:10px;border:1px solid var(--color-border-tertiary);' +
                    'background:var(--color-background-secondary);min-width:200px;flex:1;">' +
                    '<span style="font-weight:700;font-size:13px;">' + r.numRequi + '</span>' +
                    '<span style="font-size:12px;color:var(--color-text-secondary);">' +
                    '<i class="fa-solid fa-building" style="margin-right:4px;"></i>' +
                    r.departamento + '</span>' +
                    '<span style="font-size:12px;color:var(--color-text-secondary);">' +
                    '<i class="fa-solid fa-user" style="margin-right:4px;"></i>' +
                    r.responsable + '</span>' +
                    '<span style="font-size:11px;padding:2px 8px;border-radius:10px;' +
                    'background:#ede9fe;color:#6d28d9;font-weight:600;align-self:flex-start;">' +
                    r.cantidadPartidas + ' partida(s)</span>' +
                    '</div>';
            });
            document.getElementById("consolidadaHijas").innerHTML = hijasHtml;

            // ── Tabla de artículos ───────────────────────────────
            var articulos = data.articulos || [];
            if (!articulos.length) {
                document.getElementById("consolidadaArticulosBody").innerHTML =
                    '<tr><td colspan="6" class="text-center">Sin artículos</td></tr>';
                return;
            }

            var rows = "";
            articulos.forEach(function (a) {
                var textoCompleto = a.descripcionDetallada || "";
                var textoCorto = textoCompleto.length > 28
                    ? textoCompleto.substring(0, 28) + "…"
                    : textoCompleto || "Sin descripción...";
                var fullEscapado = textoCompleto.replace(/"/g, "&quot;");
                var tieneTexto = textoCompleto ? "tiene-texto" : "";

                rows +=
                    "<tr>" +
                    '<td><span style="font-size:11px;padding:2px 8px;border-radius:10px;' +
                    'background:#fef9c3;color:#854d0e;font-weight:600;">' +
                    a.numRequi + "</span></td>" +
                    '<td style="text-align:center">' + (a.numPartida || "") + "</td>" +
                    '<td style="text-align:center">' + (a.cantidad || "") + "</td>" +
                    "<td>" + (a.unidadMedida || "") + "</td>" +
                    "<td>" + (a.descripcion || "") + "</td>" +
                    '<td><div class="desc-preview-modal" data-full="' + fullEscapado + '" ' +
                    'onclick="verDescDetalleModal(this)">' +
                    '<span class="desc-texto-preview ' + tieneTexto + '">' +
                    textoCorto + "</span>" +
                    '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                    "</tr>";
            });
            document.getElementById("consolidadaArticulosBody").innerHTML = rows;

            // ── Documentos adjuntos ────────────────────────────
            var docsHtml = "";
            var tieneCuadro = data.cuadroComparativo && data.cuadroComparativo.length;
            var tieneAnexos = data.anexos && data.anexos.length;

            if (tieneCuadro || tieneAnexos) {
                if (tieneCuadro) {
                    docsHtml += ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                }
                if (tieneAnexos) {
                    docsHtml += ModalAdjuntos.renderGrupoHtml("Anexos", data.anexos);
                }
            } else {
                docsHtml = '<p style="color:#888;font-size:12px;font-style:italic;">Sin documentos</p>';
            }
            var docsContainer = document.getElementById("consolidadaDocumentos");
            if (docsContainer) {
                docsContainer.innerHTML = docsHtml;
                if (tieneCuadro || tieneAnexos) {
                    ModalAdjuntos.enlazarEventosContenedor(docsContainer);
                }
            }

        }).fail(function () {
            Swal.fire({ icon: "error", title: "Error al cargar el detalle." });
            bootstrap.Modal.getInstance(
                document.getElementById("modalDetalleConsolidada")
            ).hide();
        });
    };

  window.confirmarFinalizar = function () {
    var obs = document.getElementById("txtObservacionesFinalizar").value.trim();
    if (!obs) {
      Swal.fire({
        icon: "warning",
        title: "Escribe una observación de cierre.",
        confirmButtonColor: "#fe6291",
      });
      return;
    }

    $.ajax({
      url: urlFinalizar,
      type: "POST",
      contentType: "application/json",
      data: JSON.stringify({
        idRequisicion: _idRequiFinalizar,
        observaciones: obs,
      }),
      success: function (res) {
        if (res.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalFinalizar"),
          ).hide();
          Swal.fire({
            icon: "success",
            title: "Requisición finalizada",
            timer: 2000,
            showConfirmButton: false,
          }).then(function () {
            location.reload();
          });
        } else {
          Swal.fire({
            icon: "error",
            title: "No se pudo finalizar la requisición.",
          });
        }
      },
      error: function () {
        Swal.fire({ icon: "error", title: "Error al procesar la solicitud." });
      },
    });
  };

  window.volverOpciones = function () {
    ocultarTodosPasos();
    document.getElementById("pasoOpciones").style.display = "block";
  };

  window.mostrarPasoAsignar = function () {
    ocultarTodosPasos();
    document.getElementById("pasoAsignar").style.display = "block";
  };

  window.mostrarPasoAlmacen = function () {
    ocultarTodosPasos();
    document.getElementById("pasoAlmacen").style.display = "block";
  };

  window.mostrarPasoRechazar = function () {
    ocultarTodosPasos();
    document.getElementById("pasoRechazar").style.display = "block";
  };

  window.mostrarPasoModificar = function () {
    ocultarTodosPasos();
    document.getElementById("pasoModificar").style.display = "block";
  };

  // Abrir modal — siempre arranca en paso 1
  window.abrirModalAsignar = function (idRequi, idEstatus) {
    _idRequiAsignar = idRequi;
    var esDeCompra = idEstatus === 11;

    if (esTablaServicios) {
      // Servicios: Asignar + Rechazar (sin Enviar a almacén)
      volverOpciones();
      document.getElementById("txtMotivoRechazo").value = "";

      //Ocultar el botón de enviar a almacén
      var btnAlmacen = document.getElementById("btnOpcionAlmacen");
      if (btnAlmacen) btnAlmacen.style.display = "none";

      var $select = $("#selectUsuarioAsignar");
      if ($select.data("select2")) $select.select2("destroy");
      $select.html('<option value="">-- Seleccionar responsable --</option>');

      $.get(urlUsuariosServicios, function (data) {
        data.forEach(function (u) {
          $select.append(
            '<option value="' + u.id + '">' + u.nombre + "</option>",
          );
        });
        $select.select2({
          language: "es",
          placeholder: "-- Seleccionar responsable --",
          allowClear: false,
          minimumResultsForSearch: Infinity,
          width: "100%",
        });
      });
    } else {
      // Requisiciones: los 3 botones
      volverOpciones();
      document.getElementById("txtMotivoRechazo").value = "";

      //Asegurar que el botón de almacén esté visible
      var btnAlmacen = document.getElementById("btnOpcionAlmacen");
      if (btnAlmacen) btnAlmacen.style.display = esDeCompra ? "none" : "";

      var btnModificar = document.querySelector(
        '#pasoOpciones button[onclick="mostrarPasoModificar()"]',
      );
      if (btnModificar) btnModificar.style.display = esDeCompra ? "none" : "";

      var $select = $("#selectUsuarioAsignar");
      if ($select.data("select2")) $select.select2("destroy");
      $select.html('<option value="">-- Seleccionar responsable --</option>');

      $.get(urlUsuariosMateriales, function (data) {
        data.forEach(function (u) {
          $select.append(
            '<option value="' + u.id + '">' + u.nombre + "</option>",
          );
        });
        $select.select2({
          language: "es",
          placeholder: "-- Seleccionar responsable --",
          allowClear: false,
          minimumResultsForSearch: Infinity,
          width: "100%",
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
        bootstrap.Modal.getInstance(
          document.getElementById("modalAsignar"),
        ).hide();
        Swal.fire({
          icon: "success",
          title: "Enviada a almac\u00e9n",
          confirmButtonText: "Aceptar",
          confirmButtonColor: "#fe6291",
        }).then(function () {
          location.reload();
        });
      } else {
        Swal.fire({
          icon: "error",
          title: "No se pudo enviar a almac\u00e9n",
          text: res.mensaje || "Error desconocido",
          footer: res.detalle || "",
          confirmButtonText: "Aceptar",
          confirmButtonColor: "#fe6291",
        });
      }
    });
  };

  // Confirmar rechazo
  window.confirmarRechazo = function () {
    var motivo = document.getElementById("txtMotivoRechazo").value.trim();
    if (!motivo) {
      Swal.fire({
        icon: "warning",
        title: "Escribe el motivo del rechazo",
        confirmButtonText: "Ok",
      });
      return;
    }

    $.post(
      urlRechazar,
      { idRequi: _idRequiAsignar, motivo: motivo },
      function (res) {
        if (res.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalAsignar"),
          ).hide();
          Swal.fire({
            icon: "success",
            title: "Requisici\u00f3n rechazada",
            confirmButtonText: "Aceptar",
            confirmButtonColor: "#fe6291",
          }).then(function () {
            location.reload();
          });
        } else {
          Swal.fire({
            icon: "error",
            title: "No se pudo rechazar la requisici\u00f3n",
          });
        }
      },
    );
  };

  window.confirmarModificacion = function () {
    var observacion = document
      .getElementById("txtObservacionModificacion")
      .value.trim();
    if (!observacion) {
      Swal.fire({
        icon: "warning",
        title: "Escribe las observaciones de modificaci\u00f3n",
        confirmButtonText: "Ok",
      });
      return;
    }

    $.post(
      urlModificar,
      { idRequi: _idRequiAsignar, observaciones: observacion },
      function (res) {
        if (res.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalAsignar"),
          ).hide();
          Swal.fire({
            icon: "success",
            title: "Enviada a modificaci\u00f3n",
          }).then(function () {
            location.reload();
          });
        } else {
          Swal.fire({
            icon: "error",
            title: "No se pudo enviar a modificaci\u00f3n",
            text: res.mensaje || "",
          });
        }
      },
    ).fail(function () {
      Swal.fire({ icon: "error", title: "Error al enviar la solicitud" });
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
    var observaciones = document
      .getElementById("txtObservaciones")
      .value.trim();
    var idpp = parseInt($("#actividadSeleccionada").val()) || 0;
      var ff = $("#ffSelect").val() || "";
    var tipoPrograma = $("#tipoProgramaSelect")
      .find("option:selected")
      .text()
      .trim();

    if (!observaciones) {
      Swal.fire({
        icon: "warning",
        title: "Debe escribir una observaci\u00f3n.",
      });
      return;
    }
    if (!idpp) {
      Swal.fire({ icon: "warning", title: "Debe seleccionar una actividad." });
      return;
    }
    if (!$("#ffSelect").val()) {
      Swal.fire({
        icon: "warning",
        title: "Debe seleccionar una fuente de financiamiento.",
      });
      return;
    }
    if (!$("#tipoProgramaSelect").val()) {
      Swal.fire({
        icon: "warning",
        title: "Debe seleccionar un tipo de programa.",
      });
      return;
    }

    var cogsEditados = [];
    document.querySelectorAll("#tablaDetalle tr").forEach(function (tr) {
      var inputCog = tr.querySelector(".select-cog-editable");
      var idArticuloTd = tr.querySelectorAll("td")[1];
      if (inputCog && idArticuloTd) {
        cogsEditados.push({
          idArticulo: parseInt(idArticuloTd.textContent.trim()) || 0,
          cog: parseInt($(inputCog).val()) || 0,
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
          bootstrap.Modal.getInstance(
            document.getElementById("modalDetalle"),
          ).hide();
          document.getElementById("txtObservaciones").value = "";
          location.reload();
          return;
        }

        var formData = new FormData();
        formData.append("IdRequisicion", requisicionActual);
        if (tieneCot)
          Array.from(inputCot.files).forEach((f) =>
            formData.append("Cotizaciones", f),
          );
        if (tieneCuadro)
          Array.from(inputCuadro.files).forEach((f) =>
            formData.append("CuadroComparativo", f),
          );
        if (tieneAnexos)
          Array.from(inputAnexos.files).forEach((f) =>
            formData.append("Anexos", f),
          );

        $.ajax({
          url: urlSubirArchivosAtencion,
          type: "POST",
          data: formData,
          processData: false,
          contentType: false,
          success: function () {
            bootstrap.Modal.getInstance(
              document.getElementById("modalDetalle"),
            ).hide();
            document.getElementById("txtObservaciones").value = "";
            location.reload();
          },
          error: function () {
            Swal.fire({
              icon: "warning",
              title:
                "La atenci\u00f3n se guard\u00f3, pero hubo un error al subir los archivos.",
            });
            location.reload();
          },
        });
      },
      error: function () {
        Swal.fire({
          icon: "error",
          title: "Error al atender la requisici\u00f3n.",
        });
      },
    });
    };

    window.atenderConsolidada = function (idConsolidada) {
        window._idConsolidadaAtender = idConsolidada;

        // Limpiar modal
        document.getElementById("atenderConsArticulosBody").innerHTML = "";
        document.getElementById("txtObsConsolidada").value = "";
        document.getElementById("estadoProveedoresConsolidada").style.display = "none";
        document.getElementById("textoBtnProveedoresCons").textContent = "Seleccionar Proveedores";
        document.getElementById("btnCuadroConsolidada").disabled = true;

        // Destruir select2 previos
        ["consActividadSelect", "consFfSelect", "consTipoProgramaSelect"].forEach(function (id) {
            var $el = $("#" + id);
            if ($el.data("select2")) $el.select2("destroy");
            $el.val("").trigger("change");
        });
        ["consActividadSelect", "consFfSelect", "consTipoProgramaSelect"].forEach(function (id) {
            $("#" + id).select2({
                dropdownParent: $("#modalAtenderConsolidada"),
                width: "100%",
                language: "es"
            });
        });

        // Cargar detalle para mostrar artículos y obtener primera hija
        $.get(urlDetalleConsolidada, { idConsolidada: idConsolidada }, function (data) {
            document.getElementById("atenderConsolidadaSubtitulo").textContent =
                data.folioConsolidada;

            // Tabla artículos
            var rows = "";
            (data.articulos || []).forEach(function (a) {
                rows += "<tr>" +
                    '<td><span style="font-size:11px;padding:2px 8px;border-radius:10px;' +
                    'background:#fef9c3;color:#854d0e;font-weight:600;">' +
                    a.numRequi + "</span></td>" +
                    '<td style="text-align:center">' + (a.numPartida || "") + "</td>" +
                    '<td style="text-align:center">' + (a.cantidad || "") + "</td>" +
                    "<td>" + (a.unidadMedida || "") + "</td>" +
                    "<td>" + (a.descripcion || "") + "</td>" +
                    "</tr>";
            });
            document.getElementById("atenderConsArticulosBody").innerHTML =
                rows || '<tr><td colspan="5" class="text-center">Sin artículos</td></tr>';

            // Verificar si ya existen cotizaciones guardadas
            var primeraRequi = data.requisiciones && data.requisiciones.length
                ? data.requisiciones[0].idRequi : null;
            if (primeraRequi) {
                $.get(urlObtenerCotizaciones, { idRequisicion: primeraRequi }, function (cotizaciones) {
                    if (cotizaciones && cotizaciones.length) {
                        document.getElementById("estadoProveedoresConsolidada").style.display = "block";
                        document.getElementById("textoBtnProveedoresCons").textContent = "Editar proveedores";
                        document.getElementById("btnCuadroConsolidada").disabled = false;
                    }
                });
            }

            // Mostrar documentos existentes si los hay
            var docsHtml = "";
            var tieneCuadro = data.cuadroComparativo && data.cuadroComparativo.length;
            var tieneAnexos = data.anexos && data.anexos.length;
            if (tieneCuadro || tieneAnexos) {
                if (tieneCuadro) {
                    docsHtml += ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                }
                if (tieneAnexos) {
                    docsHtml += ModalAdjuntos.renderGrupoHtml("Anexos", data.anexos);
                }
                var docsBody = document.getElementById("consolidadaDocsExistentesBody");
                if (docsBody) docsBody.innerHTML = docsHtml;
                var docsWrap = document.getElementById("consolidadaDocsExistentes");
                if (docsWrap) docsWrap.style.display = "block";
            }
        });

        new bootstrap.Modal(
            document.getElementById("modalAtenderConsolidada")
        ).show();
    };

    window.abrirWizardCotizacionesConsolidada = function () {
        if (!_idConsolidadaAtender) {
            Swal.fire({ icon: "warning", title: "No hay consolidada activa." });
            return;
        }

        window._modoConsolidada = true;
        window._idConsolidadaWizard = window._idConsolidadaAtender;
        window._idRequiCotizaciones = null;

        var el = document.getElementById("modalProveedoresRequisicion");

        // ── FIX: elevar z-index para que quede sobre el modal de atender ──
        el.style.zIndex = "1060";  // Bootstrap usa 1055 para modals apilados

        var instancia = bootstrap.Modal.getInstance(el);
        if (instancia) {
            instancia.show();
        } else {
            new bootstrap.Modal(el, { backdrop: false, keyboard: false }).show();
        }
    };

    window.descargarCuadroConsolidada = function () {
        if (!window._idConsolidadaAtender) {
            Swal.fire({ icon: "warning", title: "No hay consolidada activa." });
            return;
        }

        $.get(
            urlObtenerIdAdquisicion,
            { idConsolidada: window._idConsolidadaAtender },
            function (res) {
                var idAdquisicion = res && res.idAdquisicion ? res.idAdquisicion : null;
                var url;

                if (idAdquisicion === 1) {
                    url = (urlDescargarReqDirectaConsolidada || "").replace(/\/$/, "")
                        + "?idConsolidada=" + window._idConsolidadaAtender;
                } else {
                    url = (urlDescargarCuadroConsolidada || "").replace(/\/$/, "")
                        + "?idConsolidada=" + window._idConsolidadaAtender;
                }
                window.open(url, "_blank");
            }
        ).fail(function () {
            var url = (urlDescargarCuadroConsolidada || "").replace(/\/$/, "")
                + "?idConsolidada=" + window._idConsolidadaAtender;
            window.open(url, "_blank");
        });
    };

    window.enviarAtencionConsolidada = function () {
        var idPP = parseInt($("#consActividadSelect").val()) || 0;
        var ff = $("#consFfSelect").val() || "";
        var tipoPrograma = $("#consTipoProgramaSelect").find("option:selected").text().trim();
        var observaciones = document.getElementById("txtObsConsolidada").value.trim();

        if (!idPP) {
            Swal.fire({ icon: "warning", title: "Selecciona una actividad." });
            return;
        }
        if (!ff) {
            Swal.fire({ icon: "warning", title: "Selecciona una fuente de financiamiento." });
            return;
        }
        if (!$("#consTipoProgramaSelect").val()) {
            Swal.fire({ icon: "warning", title: "Selecciona el tipo de programa." });
            return;
        }
        if (!observaciones) {
            Swal.fire({ icon: "warning", title: "Escribe una observación." });
            return;
        }

        $.ajax({
            url: urlAtenderConsolidada,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                idConsolidada: _idConsolidadaAtender,
                idPP: idPP,
                ff: ff,
                tipoPrograma: tipoPrograma,
                observaciones: observaciones
            }),
            success: function () {
                var inputCuadro = document.getElementById("inputCuadroConsolidada");
                var inputAnexos = document.getElementById("inputAnexosConsolidada");
                var tieneCuadro = inputCuadro && inputCuadro.files.length > 0;
                var tieneAnexos = inputAnexos && inputAnexos.files.length > 0;

                if (!tieneCuadro && !tieneAnexos) {
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalAtenderConsolidada")
                    ).hide();
                    Swal.fire({
                        icon: "success", title: "Consolidada atendida",
                        timer: 2000, showConfirmButton: false
                    }).then(function () { location.reload(); });
                    return;
                }

                var formData = new FormData();
                formData.append("IdConsolidada", _idConsolidadaAtender);
                if (tieneCuadro)
                    Array.from(inputCuadro.files).forEach(function (f) {
                        formData.append("CuadroComparativo", f);
                    });
                if (tieneAnexos)
                    Array.from(inputAnexos.files).forEach(function (f) {
                        formData.append("Anexos", f);
                    });

                $.ajax({
                    url: urlSubirArchivosConsolidada,
                    type: "POST",
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function () {
                        bootstrap.Modal.getInstance(
                            document.getElementById("modalAtenderConsolidada")
                        ).hide();
                        Swal.fire({
                            icon: "success", title: "Consolidada atendida",
                            timer: 2000, showConfirmButton: false
                        }).then(function () { location.reload(); });
                    },
                    error: function () {
                        Swal.fire({
                            icon: "warning",
                            title: "Atención guardada, error al subir archivos."
                        });
                        location.reload();
                    }
                });
            },
            error: function () {
                Swal.fire({ icon: "error", title: "Error al atender la consolidada." });
            }
        });
    };

  function renderizarArchivosReadonly(cotizaciones, cuadro) {
    if (
      window.ModalAdjuntos &&
      typeof window.ModalAdjuntos.renderizarArchivosReadonly === "function"
    ) {
      window.ModalAdjuntos.renderizarArchivosReadonly(cotizaciones, cuadro);
    }
  }

  function mostrarMensajeVacio(totalVisibles, tbodyOptional, totalOriginal) {
    var tbody =
      tbodyOptional ||
      document.querySelector(
        ".tabla-requisiciones:not(#tablaModalDetalle) tbody",
      );
    if (!tbody) return;

    // Para tabs con segundo tbody (consolidadas), sumar ambos
    var totalReal = totalVisibles;
    var totalOriginalReal = totalOriginal || totalVisibles;
    var segundoTbodyId = null;
    if (tbody.id === "tbodyVerificadasIndividuales") {
      segundoTbodyId = "tbodyVerificadasConsolidadas";
    } else if (tbody.id === "tbodyAutorizadasIndividuales") {
      segundoTbodyId = "tbodyAutorizadasConsolidadas";
    } else if (tbody.id === "tbodyDocumentos") {
      segundoTbodyId = "tbodyDocumentosConsolidadas";
    }
    if (segundoTbodyId) {
      var tbodyCons = document.getElementById(segundoTbodyId);
      if (tbodyCons) {
        var filasCons = tbodyCons.querySelectorAll("tr.fila-requi, tr.fila-consolidada");
        totalReal += filasCons.length;
        totalOriginalReal += filasCons.length;
      }
    }

    var hayFiltrosActivos =
      (document.getElementById("filtroNumReq") && document.getElementById("filtroNumReq").value) ||
      (document.getElementById("filtroDepartamento") && document.getElementById("filtroDepartamento").value) ||
      (document.getElementById("filtroEstado") && document.getElementById("filtroEstado").value) ||
      fechaSeleccionada;

    var filaVacia = tbody.querySelector(".fila-vacia");
    if (totalReal === 0) {
      if (!filaVacia) {
        filaVacia = document.createElement("tr");
        filaVacia.className = "fila-vacia";
        var mensajeBase = "No hay requisiciones disponibles";
        if (tbody.id === "tbodyAutorizadasIndividuales") {
            mensajeBase = "No hay requisiciones autorizadas";
        }
        var mensaje = hayFiltrosActivos
          ? "Sin resultados para los filtros aplicados"
          : mensajeBase;
        filaVacia.innerHTML =
          '<td colspan="8" class="text-center">' + mensaje + '</td>';
        tbody.appendChild(filaVacia);
      } else if (hayFiltrosActivos) {
        // Si ya existe fila-vacia pero ahora hay filtros activos, actualizar el mensaje
        filaVacia.innerHTML =
          '<td colspan="8" class="text-center">Sin resultados para los filtros aplicados</td>';
      } else if (!hayFiltrosActivos) {
        // Si no hay filtros, restaurar mensaje original
        filaVacia.innerHTML =
          '<td colspan="8" class="text-center">No hay requisiciones disponibles</td>';
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

  function cargarRequisicionesConDocumentos() {
    const tbodyDocumentos = document.getElementById("tbodyDocumentos");
      if (!tbodyDocumentos) return;

    fetch(urlObtenerArchivos, {
      method: "GET",
      headers: { Accept: "application/json" },
      credentials: "same-origin",
    })
      .then((r) => r.json())
      .then((data) => {
        if (!data || !data.requisiciones || data.requisiciones.length === 0) {
          tbodyDocumentos.innerHTML =
            '<tr id="filaVaciaDocumentos" class="fila-vacia"><td colspan="8" class="text-center">No hay requisiciones con documentos</td></tr>';
          return;
        }

        tbodyDocumentos.innerHTML = "";
        data.requisiciones.forEach((item, idx) => {
          const tr = document.createElement("tr");
          tr.className = "fila-requi";
          tr.setAttribute("data-requi-id", item.idRequi);
          tr.innerHTML = `
            <td style="text-align:center">${idx + 1}</td>
            <td>${renderFolioTabla(item.numRequi)}</td>
            <td>${escapeHtml(item.fechaEmision || "—")}</td>
            <td>${renderTextoTablaPrincipal(item.departamento || "—", "fa-solid fa-building")}</td>
            <td>${renderTextoTablaPrincipal(item.responsable || "—", "fa-solid fa-user")}</td>
            <td style="text-align:center">${item.cantidadPartidas}</td>
            <td>${obtenerBadgeEstatusHtml(item.idEstatus || 0, item.estatus || "—")}</td>
            <td style="text-align:center">
              <button class="btn-accion btn-ver" title="Ver archivos" onclick="verArchivosRequisicion(${item.idRequi})">
                <i class="fa-solid fa-file"></i>
              </button>
            </td>
          `;
          tbodyDocumentos.appendChild(tr);
        });
        mostrarMensajeVacio(data.requisiciones.length, tbodyDocumentos, data.requisiciones.length);
        aplicarPaginacionRequisiciones();
      })
      .catch((err) => {
        console.error("Error cargando requisiciones con documentos:", err);
        tbodyDocumentos.innerHTML =
          '<tr id="filaVaciaDocumentos" class="fila-vacia"><td colspan="8" class="text-center">Error al cargar los datos</td></tr>';
      });
  }

  window.cargarConsolidadasDocumentos = function () {
    var tbody = document.getElementById("tbodyDocumentosConsolidadas");
    if (!tbody) return;
    if (tbody.getAttribute("data-cargado") === "1") return;
    if (!urlConsolidadasDocumentos) return;

    fetch(urlConsolidadasDocumentos + "?servicio=" + esTablaServicios, { credentials: "same-origin" })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        tbody.setAttribute("data-cargado", "1");

        if (!data || !data.consolidadas || data.consolidadas.length === 0) return;

        // Eliminar mensaje vacío si existe
        var filaVacia = document.getElementById("filaVaciaDocumentos");
        if (filaVacia) filaVacia.remove();
        var tbodyIndDoc = document.getElementById("tbodyDocumentos");
        if (tbodyIndDoc) {
            var fvDoc = tbodyIndDoc.querySelector(".fila-vacia");
            if (fvDoc) fvDoc.remove();
        }

        tbody.innerHTML = "";
        data.consolidadas.forEach(function (c, idx) {
          var tr = document.createElement("tr");
          tr.className = "fila-requi fila-consolidada";
          tr.setAttribute("data-consolidada-id", c.consolidadaId);
          tr.innerHTML =
            '<td style="text-align:center">' + (idx + 1) + "</td>" +
            "<td><strong>" + (c.folioConsolidada || "—") + " <span style='font-size:10px;color:var(--color-text-secondary);'>(<i class='fa-solid fa-layer-group'></i> Consolidada)</span></strong></td>" +
            "<td>" + (c.fechaCreacion || "—") + "</td>" +
            '<td style="max-width:200px;white-space:normal;font-size:12px;">' +
            (c.departamentos || "—") + "</td>" +
            '<td style="text-align:center">' + (c.cantidadRequis || 0) + "</td>" +
            '<td style="text-align:center">' + (c.totalPartidas || 0) + "</td>" +
            "<td>" + (c.estatus || "—") + "</td>" +
            '<td style="text-align:center">' +
            '<div class="acciones-grupo" style="justify-content:center">' +
            '<button class="btn-accion btn-ver" title="Ver archivos" ' +
            'onclick="verArchivosConsolidada(' + c.consolidadaId + ')">' +
            '<i class="fa-solid fa-file"></i>' +
            "</button>" +
            "</div>" +
            "</td>";
          tbody.appendChild(tr);
        });

        aplicarPaginacionRequisiciones();
      })
      .catch(function () {
        if (tbody) tbody.innerHTML =
          '<tr class="fila-vacia"><td colspan="8" class="text-center">' +
          "Error al cargar las consolidadas con documentos</td></tr>";
      });
  };

  window.verArchivosConsolidada = function (idConsolidada) {
    if (window.DocumentosRequisicionModal) {
      window.DocumentosRequisicionModal.open({
        idConsolidada: idConsolidada,
        fetchUrl: urlObtenerArchivosConsolidada,
        downloadZipUrl: urlDescargarArchivosConsolidadaZip,
      });
      return;
    }

    fetch(urlObtenerArchivosConsolidada + "?idConsolidada=" + idConsolidada, {
      method: "GET",
      headers: { Accept: "application/json" },
      credentials: "same-origin",
    })
      .then((r) => r.json())
      .then((archivos) => {
        if (!archivos || archivos.length === 0) {
          alert("Esta consolidada no tiene archivos vinculados");
          return;
        }

        var archivoActual = archivos[0];
        var esImagen = /\.(jpg|jpeg|png|gif|webp)$/i.test(archivoActual.nombreArchivo);
        var esPdf = /\.pdf$/i.test(archivoActual.nombreArchivo);

        var listaHtml = '<ul style="list-style: none; padding: 0; margin: 0;">';
        archivos.forEach((arch, idx) => {
          var nombre = arch.nombreArchivo || arch.ruta.split("/").pop();
          var activo = idx === 0 ? "activo" : "";
          listaHtml +=
            '<li class="archivo-item ' + activo + '" data-archivo="' + arch.ruta + '" data-nombre="' + nombre + '" data-tipo="' + arch.tipo + '" data-fecha="' + arch.fechaSubida + '" style="padding: 12px; border-bottom: 1px solid #f0f0f0; cursor: pointer; transition: background-color 0.2s;">' +
              '<div style="display: flex; justify-content: space-between; align-items: flex-start;">' +
                '<div style="flex: 1;">' +
                  '<div style="font-weight: 500; color: #333;">' +
                    '<i class="fa-solid fa-file"></i> ' + nombre +
                  '</div>' +
                  '<div style="font-size: 0.85rem; color: #888; margin-top: 4px;">' +
                    arch.tipo + ' &bull; ' + arch.fechaSubida +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</li>';
        });
        listaHtml += '</ul>';

        var previewHtml = esImagen
          ? '<img src="' + archivoActual.ruta + '" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />'
          : esPdf
            ? '<iframe src="' + archivoActual.ruta + '" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>'
            : '<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;"><div style="text-align: center; color: #999;"><i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i><p>No hay vista previa disponible</p><p style="font-size: 0.9rem;">Descarga el archivo para verlo</p></div></div>';

        var modalHtml = '<div class="modal fade" id="modalArchivosConsolidada" tabindex="-1" aria-hidden="true">' +
          '<div class="modal-dialog modal-lg modal-dialog-centered">' +
            '<div class="modal-content modal-premium">' +
              '<div class="modal-header">' +
                '<h5 class="modal-title modal-titulo-premium"><i class="fa-solid fa-folder-open" style="margin-right:8px;"></i>Archivos de la consolidada</h5>' +
                '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cerrar"></button>' +
              '</div>' +
              '<div class="modal-body" style="display:flex;gap:16px;padding:16px;">' +
                '<div style="flex:1;max-height:400px;overflow-y:auto;">' + listaHtml + '</div>' +
                '<div style="flex:1.5;min-height:300px;background:#fafafa;border-radius:8px;padding:12px;display:flex;align-items:center;justify-content:center;" id="previewContainerConsolidada">' + previewHtml + '</div>' +
              '</div>' +
              '<div class="modal-footer" style="justify-content: flex-end;">' +
                '<a href="' + urlDescargarArchivosConsolidadaZip + "?idConsolidada=" + idConsolidada + '" class="btn boton-rosa"><i class="fa-solid fa-file-zipper"></i> Descarga masiva</a>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</div>';

        var existingModal = document.getElementById("modalArchivosConsolidada");
        if (existingModal) existingModal.remove();

        var div = document.createElement("div");
        div.innerHTML = modalHtml;
        document.body.appendChild(div.firstElementChild);

        var modalEl = document.getElementById("modalArchivosConsolidada");
        var modalInstance = new bootstrap.Modal(modalEl);
        modalInstance.show();

        var archivoItems = modalEl.querySelectorAll(".archivo-item");
        var previewContainer = modalEl.querySelector("#previewContainerConsolidada");
        archivoItems.forEach((item) => {
          item.addEventListener("click", function () {
            archivoItems.forEach((it) => { it.classList.remove("activo"); });
            this.classList.add("activo");
            var ruta = this.getAttribute("data-archivo") || "";
            var nombre = this.getAttribute("data-nombre") || "";
            var esImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(nombre);
            var esPDF = /\.pdf$/i.test(nombre);
            previewContainer.innerHTML = esImg
              ? '<img src="' + ruta + '" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />'
              : esPDF
                ? '<iframe src="' + ruta + '" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>'
                : '<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;"><div style="text-align: center; color: #999;"><i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i><p>No hay vista previa disponible</p><p style="font-size: 0.9rem;">Descarga el archivo para verlo</p></div></div>';
          });
        });
      })
      .catch((err) => {
        console.error("Error cargando archivos de consolidada:", err);
        alert("Error al cargar los archivos");
      });
  };

  window.verArchivosRequisicion = function (idRequisicion) {
    if (window.DocumentosRequisicionModal) {
      window.DocumentosRequisicionModal.open({
        idRequisicion: idRequisicion,
        fetchUrl: urlObtenerTodosArchivos,
        downloadZipUrl: urlDescargarTodosArchivosZip,
      });
      return;
    }

    fetch(urlObtenerTodosArchivos + "?idRequisicion=" + idRequisicion, {
      method: "GET",
      headers: { Accept: "application/json" },
      credentials: "same-origin",
    })
      .then((r) => r.json())
      .then((archivos) => {
        if (!archivos || archivos.length === 0) {
          alert("Esta requisición no tiene archivos vinculados");
          return;
        }

        const archivoActual = archivos[0];
        const esImagen = /\.(jpg|jpeg|png|gif|webp)$/i.test(
          archivoActual.nombreArchivo,
        );
        const esPdf = /\.pdf$/i.test(archivoActual.nombreArchivo);

        let listaHtml = '<ul style="list-style: none; padding: 0; margin: 0;">';
        archivos.forEach((arch, idx) => {
          const nombre = arch.nombreArchivo || arch.ruta.split("/").pop();
          const activo = idx === 0 ? "activo" : "";
          listaHtml += `
            <li class="archivo-item ${activo}" data-archivo="${arch.ruta}" data-nombre="${nombre}" data-tipo="${arch.tipo}" data-fecha="${arch.fechaSubida}" style="padding: 12px; border-bottom: 1px solid #f0f0f0; cursor: pointer; transition: background-color 0.2s;">
              <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                <div style="flex: 1;">
                  <div style="font-weight: 500; color: #333;"><i class="fa-solid fa-file"></i> ${nombre}</div>
                  <div style="font-size: 0.85rem; color: #888; margin-top: 4px;">${arch.tipo} • ${arch.fechaSubida}</div>
                </div>
                <a href="${arch.ruta}" download style="margin-left: 10px; white-space: nowrap; padding: 4px 8px; font-size: 11px; color: #666; border: 1px solid #ddd; border-radius: 4px; text-decoration: none; display: inline-block; transition: all 0.2s; background: #f8f8f8;" onmouseover="this.style.background='#efefef'; this.style.color='#333';" onmouseout="this.style.background='#f8f8f8'; this.style.color='#666';">
                  <i class="fa-solid fa-download" style="font-size: 9px; margin-right: 4px;"></i>Descargar
                </a>
              </div>
            </li>
          `;
        });
        listaHtml += "</ul>";

        let previewHtml = "";
        if (esPdf) {
          previewHtml = `<iframe src="${archivoActual.ruta}" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>`;
        } else if (esImagen) {
          previewHtml = `<img src="${archivoActual.ruta}" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />`;
        } else {
          previewHtml = `<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;">
            <div style="text-align: center; color: #999;">
              <i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i>
              <p>No hay vista previa disponible</p>
              <p style="font-size: 0.9rem;">Descarga el archivo para verlo</p>
            </div>
          </div>`;
        }

        const modal = document.createElement("div");
        modal.className = "modal fade";
        modal.setAttribute("tabindex", "-1");
        modal.setAttribute("aria-hidden", "true");
        modal.innerHTML = `
          <div class="modal-dialog modal-xl modal-dialog-centered">
            <div class="modal-content modal-premium">
              <div class="modal-header modal-header-premium">
                <div>
                  <h5 class="modal-title modal-titulo-premium">Archivos de la Requisición</h5>
                  <p class="modal-subtitulo-premium">Visualiza y descarga los documentos adjuntos</p>
                </div>
                <button type="button" class="modal-btn-cerrar" data-bs-dismiss="modal">
                  <i class="fa-solid fa-xmark"></i>
                </button>
              </div>
              <div class="modal-body modal-body-premium" style="padding: 0;">
                <div style="display: flex; height: 600px;">
                  <div style="flex: 1; overflow-y: auto; border-right: 1px solid #e0e0e0; background: #fafafa;">
                    <div style="padding: 0;">${listaHtml}</div>
                  </div>
                  <div style="flex: 2; padding: 20px; display: flex; align-items: center; justify-content: center; background: white;" id="previewContainer">
                    ${previewHtml}
                  </div>
                </div>
              </div>
              <div class="modal-footer" style="justify-content: flex-end;">
                <a href="${urlDescargarTodosArchivosZip}?idRequisicion=${encodeURIComponent(idRequisicion)}"
                   class="btn boton-rosa">
                  <i class="fa-solid fa-file-zipper"></i> Descarga masiva
                </a>
              </div>
            </div>
          </div>
        `;
        document.body.appendChild(modal);

        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        const archivoItems = modal.querySelectorAll(".archivo-item");
        archivoItems.forEach((item) => {
          item.addEventListener("click", function () {
            archivoItems.forEach((it) => it.classList.remove("activo"));
            this.classList.add("activo");

            const ruta = this.getAttribute("data-archivo");
            const nombre = this.getAttribute("data-nombre");
            const esImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(nombre);
            const isPdf = /\.pdf$/i.test(nombre);

            const container = document.getElementById("previewContainer");
            let nuevoPreview = "";
            if (isPdf) {
              nuevoPreview = `<iframe src="${ruta}" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>`;
            } else if (esImg) {
              nuevoPreview = `<img src="${ruta}" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />`;
            } else {
              nuevoPreview = `<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;">
                <div style="text-align: center; color: #999;">
                  <i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i>
                  <p>No hay vista previa disponible</p>
                  <p style="font-size: 0.9rem;">Descarga el archivo para verlo</p>
                </div>
              </div>`;
            }
            container.innerHTML = nuevoPreview;
          });

          item.addEventListener("mouseover", function () {
            if (!this.classList.contains("activo")) {
              this.style.backgroundColor = "#f0f0f0";
            }
          });

          item.addEventListener("mouseout", function () {
            if (!this.classList.contains("activo")) {
              this.style.backgroundColor = "transparent";
            }
          });
        });

        modal.addEventListener("hidden.bs.modal", () => {
          modal.remove();
        });
      })
      .catch((err) => {
        console.error("Error cargando archivos:", err);
        alert("Error al cargar los archivos");
      });
  };

  if (modoTabs && container) {
    var tabBtns = container.querySelectorAll(".almacen-tabs-btn");
    var tabPanels = container.querySelectorAll(".almacen-tab-panel");
    tabBtns.forEach(function (btn) {
      btn.addEventListener("click", function () {
        var tab = this.getAttribute("data-tab");

        // Cargar datos del tab de documentos si es necesario
        if (tab === "documentos" && urlObtenerArchivos) {
          cargarRequisicionesConDocumentos();
          if (urlConsolidadasDocumentos) {
            cargarConsolidadasDocumentos();
          }
        }

          if (tab === "consolidadas" && urlListarConsolidadas) {
              cargarConsolidadas();
          }

          if (tab === "verificadas" && urlConsolidadasVerificadas) {
              cargarVerificadasConsolidadas();
          }

          if (tab === "autorizadas" && urlConsolidadasAutorizadas) {
              cargarConsolidadasAutorizadas();
          }

        var panelActivoAnterior = container.querySelector(
          ".almacen-tab-panel.activo",
        );
        if (panelActivoAnterior && panelActivoAnterior.id !== "tab-" + tab) {
          var filasVistas =
            panelActivoAnterior.querySelectorAll("tr.fila-nueva");
          filasVistas.forEach(function (f) {
            f.classList.remove("fila-nueva");
          });
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
            window.TabsNotificacionesRequi.iniciarParpadeoFilasNuevasEnPanel(
              panelDestinoClick,
            );
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
      },
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
      if (
        !tr ||
        tr.classList.contains("fila-detalle") ||
        tr.classList.contains("fila-vacia")
      )
        return;
      if (e.target.closest(".acciones-grupo, button, a.btn-accion")) return;
      if (!tr.classList.contains("fila-requi")) return;

      var tabla = getTablaFromRow(tr);
      var filaDetalle = tr.nextElementSibling;
      if (!filaDetalle || !filaDetalle.classList.contains("fila-detalle"))
        return;

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
        tablas.forEach((t) => colapsarTodasDetalle(t));
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
        var btnCuadro = document.getElementById("btnDescargarCuadroComparativo");
        if (btnCuadro && btnCuadro.disabled) {
            Swal.fire({
                icon: "warning",
                title: "Selecciona los proveedores primero",
                text: "Guarda las cotizaciones para habilitar el cuadro comparativo.",
                confirmButtonColor: "#fe6291",
            });
            return;
        }

        if (!requisicionActual) {
            Swal.fire({ icon: "warning", title: "Sin requisición activa." });
            return;
        }

        // Consultar qué tipo de adquisición tiene esta requisición
        $.get(
            urlObtenerIdAdquisicion,
            { idRequisicion: requisicionActual },
            function (res) {
                var idAdquisicion = res && res.idAdquisicion ? res.idAdquisicion : null;

                if (idAdquisicion === 1) {
                    // Adjudicación directa → ReqDirecta
                    var url =
                        (urlDescargarReqDirecta || "").replace(/\/$/, "") +
                        "?idRequisicion=" +
                        requisicionActual;
                    window.open(url, "_blank");
                } else {
                    // Cualquier otro → cuadro comparativo
                    var url =
                        (urlDescargarCuadroComparativo || "").replace(/\/$/, "") +
                        "?idRequisicion=" +
                        requisicionActual;
                    window.open(url, "_blank");
                }
            }
        ).fail(function () {
            // Si falla, caer al cuadro comparativo por defecto
            var url =
                (urlDescargarCuadroComparativo || "").replace(/\/$/, "") +
                "?idRequisicion=" +
                requisicionActual;
            window.open(url, "_blank");
        });
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
      _tplEl.content
        .querySelectorAll(".modal-proveedores-select option")
        .forEach(function (o) {
          if (o.value)
            CATALOGO_PROVEEDORES_MODAL.push({ v: o.value, t: o.text });
        });
    }

    // ── Estado del wizard ──
    // wizardDatos[i] = [ { idProveedor, importe }, ... ]  (filas de la partida i)
    var wizardPartidas = []; // array de { idRequiDetalle, nombrePartida }
    var wizardDatos = []; // datos por índice de partida
    var wizardIdx = 0; // índice de la partida activa

    function $modal() {
      return $("#modalProveedoresRequisicion");
    }
    function $contenedor() {
      return $("#modalProveedoresFilas");
    }

    function cargarOpcionesGanador() {
      var $divOpciones = $("#wizardGanadorOpciones");
      $divOpciones.empty();

      var $sel = $("#selectGanadorManual");
      if ($sel.data("select2")) $sel.select2("destroy");
      $sel.html('<option value="">-- Seleccionar --</option>');

      // Calcular totales por proveedor usando wizardDatos (lo que el usuario llenó)
      var totalesPorProveedor = {};

      wizardPartidas.forEach(function (partida, idxPartida) {
        var filas = wizardDatos[idxPartida] || [];
        filas.forEach(function (fila) {
          if (!fila.idProveedor || fila.idProveedor <= 0) return;
          var importe = parseFloat(String(fila.importe).replace(/,/g, "")) || 0;
          if (importe <= 0) return;

          var idProv = fila.idProveedor;
          if (!totalesPorProveedor[idProv]) {
            // Buscar nombre del proveedor en el catálogo
            var nombreProv = "";
            CATALOGO_PROVEEDORES_MODAL.forEach(function (c) {
              if (String(c.v) === String(idProv)) nombreProv = c.t;
            });
            totalesPorProveedor[idProv] = {
              idProveedor: idProv,
              nombreProveedor: nombreProv,
              subtotal: 0,
              iva: 0,
            };
          }

          totalesPorProveedor[idProv].subtotal += importe;
          if (fila.iva) {
            totalesPorProveedor[idProv].iva += importe * 0.16;
          }
        });
      });

      var opciones = Object.values(totalesPorProveedor)
        .map(function (o) {
          return {
            idProveedor: o.idProveedor,
            nombreProveedor: o.nombreProveedor,
            subtotal: o.subtotal,
            iva: o.iva,
            total: o.subtotal + o.iva,
            esSugerido: false,
          };
        })
        .filter(function (o) {
          return o.total > 0;
        });

      // Ordenar por total y marcar el más barato como sugerido
      opciones.sort(function (a, b) {
        return a.total - b.total;
      });
      if (opciones.length > 0) opciones[0].esSugerido = true;

      if (!opciones.length) {
        $divOpciones.html(
          '<p style="color:#888;font-size:13px;font-style:italic;">Agrega precios para ver opciones.</p>',
        );
        return;
      }

      // Si el ganador actual ya no está en las opciones, resetear
      var idsValidos = opciones.map(function (o) {
        return o.idProveedor;
      });
      if (wizardIdGanador && idsValidos.indexOf(wizardIdGanador) === -1) {
        wizardIdGanador = 0;
        wizardGanadorManual = false;
      }

      // Si no hay ganador elegido, auto-seleccionar el sugerido
      if (!wizardIdGanador) {
        wizardIdGanador = opciones[0].idProveedor;
        wizardGanadorManual = false;
      }

      var fmt = function (v) {
        return v.toLocaleString("es-MX", {
          style: "currency",
          currency: "MXN",
        });
      };

      opciones.forEach(function (op) {
        var esSeleccionado = op.idProveedor === wizardIdGanador;

        var badge = op.esSugerido
          ? '<span style="font-size:10px;background:#fef9c3;color:#854d0e;padding:2px 7px;border-radius:10px;margin-left:6px;">Menor costo</span>'
          : "";

        var $fila = $("<div>")
          .css({
            display: "flex",
            alignItems: "center",
            gap: "10px",
            padding: "10px 14px",
            borderRadius: "8px",
            cursor: "pointer",
            border: esSeleccionado
              ? "2px solid #fe6291"
              : "1px solid var(--color-border-tertiary)",
            background: esSeleccionado
              ? "#fff0f5"
              : "var(--color-background-secondary)",
            marginBottom: "4px",
            transition: "all .15s",
          })
          .html(
            '<div style="display:flex;align-items:center;gap:6px;flex:1;">' +
              '<i class="fa-solid fa-' +
              (esSeleccionado ? "circle-dot" : "circle") +
              '" style="color:' +
              (esSeleccionado ? "#fe6291" : "#9ca3af") +
              ';font-size:15px;"></i>' +
              '<span style="font-size:13px;font-weight:500;">' +
              op.nombreProveedor +
              badge +
              "</span>" +
              "</div>" +
              '<div style="text-align:right;font-size:12px;color:var(--color-text-secondary);">' +
              "<div>Subtotal: " +
              fmt(op.subtotal) +
              "</div>" +
              "<div>IVA: " +
              fmt(op.iva) +
              "</div>" +
              '<div style="font-weight:600;color:var(--color-text-primary);">Total: ' +
              fmt(op.total) +
              "</div>" +
              "</div>",
          );

        $fila.on("click", function () {
          wizardIdGanador = op.idProveedor;
          wizardGanadorManual = !op.esSugerido;
          cargarOpcionesGanador(); // re-renderizar con nueva selección
        });

        $divOpciones.append($fila);
        $sel.append(
          '<option value="' +
            op.idProveedor +
            '">' +
            op.nombreProveedor +
            "</option>",
        );
      });

      $sel.select2({ dropdownParent: $modal(), width: "100%", language: "es" });
    }

    // ── Helpers Select2 ──
    function buildOpcionesHtml() {
      var html = '<option value="">-- Seleccione proveedor --</option>';
      CATALOGO_PROVEEDORES_MODAL.forEach(function (p) {
        html += '<option value="' + p.v + '">' + p.t + "</option>";
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
      $contenedor()
        .find(".modal-proveedores-select")
        .each(function () {
          var val = $(this).val();
          if (val) seleccionados.push(val);
        });

      $contenedor()
        .find(".modal-proveedores-select")
        .each(function () {
          var $sel = $(this);
          var currentVal = $sel.val();

          // Reconstruir opciones ocultando los seleccionados en otras filas
          var newHtml = '<option value="">-- Seleccione proveedor --</option>';
          if (CATALOGO_PROVEEDORES_MODAL) {
            CATALOGO_PROVEEDORES_MODAL.forEach(function (c) {
              var strId = String(c.v);
              var isSelectedInOther =
                strId !== String(currentVal) &&
                seleccionados.indexOf(strId) !== -1;
              if (!isSelectedInOther) {
                newHtml +=
                  '<option value="' +
                  c.v +
                  '"' +
                  (strId === String(currentVal) ? " selected" : "") +
                  ">" +
                  c.t +
                  "</option>";
              }
            });
          }

          // Comprobar si el innerHTML ha cambiado para evitar reinicializaciones innecesarias
          // Validamos contando cuántos options tiene ahora vs los que tendría el nuevo HTML
          var currentOptionsCount = $sel.find("option").length;
          var newOptionsCount = (newHtml.match(/<option/g) || []).length;

          // También comprobamos si los values exactos cambiaron
          var currentVals = [];
          $sel.find("option").each(function () {
            currentVals.push($(this).val());
          });
          var newVals = [];
          var match;
          var regex = /value="([^"]*)"/g;
          while ((match = regex.exec(newHtml)) !== null) {
            newVals.push(match[1]);
          }

          var changed = currentVals.join(",") !== newVals.join(",");

          if (changed) {
            $sel.html(newHtml);
            if ($sel.data("select2")) {
              $sel.select2("destroy");
              initSelect2($sel);
            }
          }
        });
    }

    $(document).on(
      "change",
      "#modalProveedoresFilas .modal-proveedores-select",
      function () {
        actualizarOpcionesProveedores();
      },
    );

    // ── Guardar filas actuales al estado del wizard ──
    function guardarFilasActuales() {
      var filas = [];
      $contenedor()
        .find(".modal-proveedores-fila")
        .each(function () {
          filas.push({
            idProveedor:
              parseInt($(this).find(".modal-proveedores-select").val()) || 0,
            importe:
              $(this).find(".modal-proveedores-input-precio").val() || "",
            iva: $(this).find(".modal-proveedores-check-iva").prop("checked"),
          });
        });
      wizardDatos[wizardIdx] = filas;
    }


      //Consolidadas
      (function () {
          var urlRequisConsolidables = container
              ? container.getAttribute("data-url-requis-consolidables") : "";
          var urlCrearConsolidada = container
              ? container.getAttribute("data-url-crear-consolidada") : "";

          var _requisDisponibles = [];
          var _seleccionadas = new Set();

          window.abrirModalConsolidada = function () {
              _seleccionadas.clear();
              document.getElementById("filtroConsolidada").value = "";
              document.getElementById("listaRequisConsolidables").innerHTML =
                  '<div style="text-align:center;padding:2rem;color:#9ca3af;">' +
                  '<i class="fa-solid fa-spinner fa-spin"></i> Cargando...</div>';
              document.getElementById("sinRequisConsolidables").style.display = "none";
              document.getElementById("contadorConsolidada").style.display = "none";
              document.getElementById("btnConfirmarConsolidada").disabled = true;

              fetch(urlRequisConsolidables, { credentials: "same-origin" })
                  .then(r => r.json())
                  .then(data => {
                      _requisDisponibles = data || [];
                      renderizarCardsConsolidada(_requisDisponibles);
                  })
                  .catch(() => {
                      document.getElementById("listaRequisConsolidables").innerHTML =
                          '<div style="text-align:center;padding:2rem;color:#b91c1c;">' +
                          '<i class="fa-solid fa-triangle-exclamation"></i> Error al cargar las requisiciones.</div>';
                  });
          };

          function renderizarCardsConsolidada(lista) {
              var contenedor = document.getElementById("listaRequisConsolidables");
              var sinResultados = document.getElementById("sinRequisConsolidables");

              if (!lista.length) {
                  contenedor.innerHTML = "";
                  sinResultados.style.display = "block";
                  return;
              }

              sinResultados.style.display = "none";
              contenedor.innerHTML = "";

              lista.forEach(function (req) {
                  var sel = _seleccionadas.has(req.idRequi);
                  var card = document.createElement("div");
                  card.setAttribute("data-id", req.idRequi);
                  card.setAttribute("data-folio", (req.numRequi || "").toLowerCase());
                  card.setAttribute("data-depto", (req.departamento || "").toLowerCase());
                  card.style.cssText =
                      "display:flex;align-items:center;gap:14px;padding:12px 16px;" +
                      "border-radius:10px;cursor:pointer;transition:all .15s;" +
                      "border:2px solid " + (sel ? "#fe6291" : "var(--color-border-tertiary)") + ";" +
                      "background:" + (sel ? "#fff0f5" : "var(--color-background-secondary)") + ";";

                  card.innerHTML =
                      '<div class="cons-chk" style="width:22px;height:22px;border-radius:50%;flex-shrink:0;' +
                      'display:flex;align-items:center;justify-content:center;' +
                      'border:2px solid ' + (sel ? "#fe6291" : "#d1d5db") + ';' +
                      'background:' + (sel ? "#fe6291" : "transparent") + ';">' +
                      (sel ? '<i class="fa-solid fa-check" style="color:white;font-size:11px;"></i>' : "") +
                      '</div>' +
                      '<div style="flex:1;min-width:0;">' +
                      '<div style="display:flex;align-items:center;gap:8px;margin-bottom:2px;">' +
                      '<span style="font-weight:700;font-size:13px;">' + (req.numRequi || "—") + '</span>' +
                      '<span style="font-size:11px;padding:2px 8px;border-radius:10px;' +
                      'background:#fef9c3;color:#854d0e;font-weight:600;">En proceso</span>' +
                      '</div>' +
                      '<div style="font-size:12px;color:var(--color-text-secondary);">' +
                      '<i class="fa-solid fa-building" style="margin-right:4px;"></i>' + (req.departamento || "—") +
                      ' &nbsp;·&nbsp; ' +
                      '<i class="fa-solid fa-calendar-days" style="margin-right:4px;"></i>' + (req.fechaEmision || "—") +
                      '</div>' +
                      '</div>' +
                      '<div style="text-align:right;flex-shrink:0;">' +
                      '<span style="font-size:11px;color:var(--color-text-secondary);">Partidas</span><br/>' +
                      '<span style="font-weight:700;font-size:15px;">' + (req.cantidadPartidas || 0) + '</span>' +
                      '</div>';

                  card.addEventListener("click", function () {
                      if (_seleccionadas.has(req.idRequi)) {
                          _seleccionadas.delete(req.idRequi);
                          card.style.border = "2px solid var(--color-border-tertiary)";
                          card.style.background = "var(--color-background-secondary)";
                          card.querySelector(".cons-chk").style.cssText +=
                              ";border-color:#d1d5db;background:transparent;";
                          card.querySelector(".cons-chk").innerHTML = "";
                      } else {
                          _seleccionadas.add(req.idRequi);
                          card.style.border = "2px solid #fe6291";
                          card.style.background = "#fff0f5";
                          card.querySelector(".cons-chk").style.borderColor = "#fe6291";
                          card.querySelector(".cons-chk").style.background = "#fe6291";
                          card.querySelector(".cons-chk").innerHTML =
                              '<i class="fa-solid fa-check" style="color:white;font-size:11px;"></i>';
                      }
                      var n = _seleccionadas.size;
                      var ctr = document.getElementById("contadorConsolidada");
                      ctr.style.display = n > 0 ? "block" : "none";
                      document.getElementById("textoContadorConsolidada").textContent =
                          n + (n === 1 ? " requisición seleccionada" : " requisiciones seleccionadas");
                      document.getElementById("btnConfirmarConsolidada").disabled = n < 2;
                  });

                  contenedor.appendChild(card);
              });
          }

          // Filtro buscador
          document.getElementById("filtroConsolidada").addEventListener("input", function () {
              var term = this.value.toLowerCase().trim();
              document.querySelectorAll("#listaRequisConsolidables [data-id]").forEach(function (card) {
                  var coincide = !term ||
                      card.getAttribute("data-folio").includes(term) ||
                      card.getAttribute("data-depto").includes(term);
                  card.style.display = coincide ? "" : "none";
              });
          });

          window.confirmarCrearConsolidada = function () {
              var ids = Array.from(_seleccionadas);
              Swal.fire({
                  title: "¿Crear consolidada?",
                  html: "Se agruparán <strong>" + ids.length + " requisiciones</strong> en una sola.",
                  icon: "question",
                  showCancelButton: true,
                  confirmButtonColor: "#fe6291",
                  confirmButtonText: "Sí, crear",
                  cancelButtonText: "Cancelar",
              }).then(function (result) {
                  if (!result.isConfirmed) return;
                  Swal.fire({ title: "Creando...", allowOutsideClick: false, didOpen: () => Swal.showLoading() });

                  fetch(urlCrearConsolidada + "?servicio=" + esTablaServicios, {
                      method: "POST",
                      credentials: "same-origin",
                      headers: { "Content-Type": "application/json" },
                      body: JSON.stringify({ idsRequisiciones: ids }),
                  })
                      .then(r => r.json())
                      .then(function (data) {
                          bootstrap.Modal.getInstance(document.getElementById("modalCrearConsolidada")).hide();
                          Swal.fire({
                              icon: "success",
                              title: "Consolidada creada",
                              html: "Folio: <strong>" + (data.folioConsolidada || "") + "</strong>",
                              confirmButtonColor: "#fe6291",
                          }).then(() => location.reload());
                      })
                      .catch(function () {
                          Swal.fire({ icon: "error", title: "Error al crear la consolidada", confirmButtonColor: "#fe6291" });
                      });
              });
          };
      })();

    // ── Renderizar la pantalla de la partida activa ──
    function renderizarPartida(idx) {
      var $c = $contenedor();
      var partida = wizardPartidas[idx];
      var total = wizardPartidas.length;

      // Actualizar header
      document.getElementById("wizardProveedoresSubtitulo").textContent =
        "Partida " + (idx + 1) + " de " + total;
      var numLabel = partida.numPartida ? "Partida " + partida.numPartida : "";
      document.getElementById("wizardProveedoresNombrePartida").innerHTML =
        (numLabel
          ? '<span style="font-weight:700;">' + numLabel + "</span> "
          : "") +
        '<span style="font-weight:400; color:var(--slate-500,#64748b);">' +
        (partida.nombrePartida || "") +
        "</span>";

      // Destruir select2 y limpiar filas
      destruirSelect2En($modal());
      $c.empty();

      // Determinar filas a renderizar
      // Si hay datos guardados para esta partida úsalos, si no precarga proveedores de la anterior
      var filasBase;
      if (wizardDatos[idx] && wizardDatos[idx].length) {
        filasBase = wizardDatos[idx];
      } else if (
        idx > 0 &&
        wizardDatos[idx - 1] &&
        wizardDatos[idx - 1].length
      ) {
        // Precargar proveedores de la partida anterior (sin precio)
        filasBase = wizardDatos[idx - 1].map(function (f) {
          return { idProveedor: f.idProveedor, importe: "" };
        });
      } else {
        // Primera partida sin datos previos: 2 filas vacías
        filasBase = [
          { idProveedor: 0, importe: "" },
          { idProveedor: 0, importe: "" },
        ];
      }

      // Crear filas en el DOM
      var tpl = document.getElementById("tplModalProveedorFila");
      filasBase.forEach(function (fila) {
        $c[0].appendChild(tpl.content.cloneNode(true));
        var $nueva = $c.find(".modal-proveedores-fila").last();
        var $sel = $nueva.find(".modal-proveedores-select");
        $sel.html(buildOpcionesHtml());
        if (fila.idProveedor) $sel.val(fila.idProveedor);
        if (fila.importe)
          $nueva.find(".modal-proveedores-input-precio").val(fila.importe);
        if (fila.iva)
          $nueva.find(".modal-proveedores-check-iva").prop("checked", true);
        initSelect2($sel);
      });

      // Botones de navegación
      var $btnAnterior = $("#btnWizardAnterior");
      var $btnSiguiente = $("#btnWizardSiguiente");

      $btnAnterior.toggle(idx > 0);

      if (idx === total - 1) {
        $btnSiguiente.html('Guardar <i class="fa-solid fa-floppy-disk"></i>');
      } else {
        $btnSiguiente.html(
          'Siguiente <i class="fa-solid fa-chevron-right"></i>',
        );
      }

      actualizarOpcionesProveedores();

      $("#wizardPasoGanador").hide();
    }

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
          confirmButtonColor: "#fe6291",
        });
        return;
      }

      if (wizardIdx < wizardPartidas.length - 1) {
        // Avanzar a la siguiente partida
        wizardIdx++;
        renderizarPartida(wizardIdx);
      } else {
        var cotizaciones = [];
        wizardPartidas.forEach(function (partida, i) {
          var filas = wizardDatos[i] || [];
          filas.forEach(function (fila) {
            if (fila.idProveedor > 0) {
              cotizaciones.push({
                idProveedor: fila.idProveedor,
                importe:
                  parseFloat(String(fila.importe).replace(/,/g, "")) || 0,
                idPartida: partida.idRequiDetalle,
                iva: fila.iva === true,
              });
            }
          });
        });

        if (!cotizaciones.length) {
          Swal.fire({
            icon: "warning",
            title: "Agrega al menos un proveedor.",
          });
          return;
        }

        $.ajax({
          url: urlGuardarCotizaciones,
          type: "POST",
          contentType: "application/json",
          data: JSON.stringify({
            idRequisicion: _idRequiCotizaciones,
            cotizaciones: cotizaciones,
          }),
          success: function () {
            actualizarEstadoProveedoresSeleccionados(true);
            // Cotizaciones guardadas → ahora cargar opciones ganador desde BD
            mostrarPasoGanadorDesdeBD();
          },
          error: function (xhr) {
            var mensaje =
              xhr.responseJSON && xhr.responseJSON.message
                ? xhr.responseJSON.message
                : "Error al guardar los proveedores.";
            Swal.fire({ icon: "error", title: mensaje });
          },
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
  })();

  (function modalProveedoresPorProveedor() {
    var proveedoresCatalogo = [];
    var proveedoresWizard = [];
    var proveedorIdx = 0;

    function $modalProveedores() {
      return $("#modalProveedoresRequisicion");
    }

    function $selectProveedor() {
      return $("#wizardProveedorSelect");
    }

    function $bodyPartidas() {
      return $("#wizardProveedorPartidasBody");
    }

    function cargarCatalogoProveedores() {
      proveedoresCatalogo = [];
      $("#wizardProveedorSelect option").each(function () {
        if (this.value) {
          proveedoresCatalogo.push({
            id: parseInt(this.value, 10),
            nombre: $(this).text(),
          });
        }
      });
    }

    function obtenerPartidasCotizacion(done) {
      if (
        CACHE_PARTIDAS_REQUI === _idRequiCotizaciones &&
        CACHE_PARTIDAS.length
      ) {
        done(CACHE_PARTIDAS);
        return;
      }

      $.get(
        urlObtenerPartidas,
        { idRequisicion: _idRequiCotizaciones },
        function (data) {
          CACHE_PARTIDAS_REQUI = _idRequiCotizaciones;
          CACHE_PARTIDAS = data || [];
          done(CACHE_PARTIDAS);
        },
      ).fail(function () {
        Swal.fire({
          icon: "error",
          title: "No se pudieron cargar las partidas.",
        });
      });
    }

    function crearPartidasVacias() {
      return CACHE_PARTIDAS.map(function (partida) {
        return {
          idPartida: partida.idRequiDetalle,
          importe: "",
          iva: false,
        };
      });
    }

    function crearProveedorVacio() {
      return {
        idProveedor: 0,
        partidas: crearPartidasVacias(),
      };
    }

    function completarPartidasProveedor(proveedor) {
      var mapa = {};

      (proveedor.partidas || []).forEach(function (fila) {
        mapa[fila.idPartida] = {
          idPartida: fila.idPartida,
          importe: fila.importe || "",
          iva: fila.iva === true,
        };
      });

      proveedor.partidas = CACHE_PARTIDAS.map(function (partida) {
        return (
          mapa[partida.idRequiDetalle] || {
            idPartida: partida.idRequiDetalle,
            importe: "",
            iva: false,
          }
        );
      });

      return proveedor;
    }

    function limpiarPrecioProveedor(valor) {
      var texto = String(valor || "");
      var textoLimpio = texto.replace(/[^0-9.]/g, "");
      var partes = textoLimpio.split(".");

      if (partes.length > 2) {
        textoLimpio = partes[0] + "." + partes.slice(1).join("");
      }

      return textoLimpio;
    }

    function obtenerProveedoresUsados() {
      return proveedoresWizard
        .map(function (proveedor, idx) {
          return idx === proveedorIdx ? 0 : proveedor.idProveedor || 0;
        })
        .filter(function (id) {
          return id > 0;
        });
    }

    function inicializarSelectProveedor(valorSeleccionado) {
      var usados = obtenerProveedoresUsados();
      var html = '<option value="">-- Seleccione proveedor --</option>';

      proveedoresCatalogo.forEach(function (proveedor) {
        var esActual = proveedor.id === valorSeleccionado;
        if (usados.indexOf(proveedor.id) !== -1 && !esActual) return;

        html +=
          '<option value="' +
          proveedor.id +
          '"' +
          (esActual ? " selected" : "") +
          ">" +
          proveedor.nombre +
          "</option>";
      });

      var $select = $selectProveedor();
      if ($select.data("select2")) {
        $select.select2("destroy");
      }

      $select.html(html);
      $select.select2({
        dropdownParent: $modalProveedores(),
        width: "100%",
        language: "es",
      });
    }

    function renderizarPartidasProveedor(proveedor) {
      var $tbody = $bodyPartidas();
      $tbody.empty();

      CACHE_PARTIDAS.forEach(function (partida, idx) {
        var fila = proveedor.partidas[idx] || {
          idPartida: partida.idRequiDetalle,
          importe: "",
          iva: false,
        };

        var numeroPartida = partida.numPartida || idx + 1;
        var descripcion = partida.descripcion || "";
        var descripcionDetallada =
          partida.descripcionDetallada || partida.nombrePartida || "";
        var descripcionDetalladaCorta =
          descripcionDetallada.length > 28
            ? descripcionDetallada.substring(0, 28) + "…"
            : descripcionDetallada || "Sin descripción...";
        var tieneDescripcionDetallada = descripcionDetallada
          ? "tiene-texto"
          : "";
        var descripcionDetalladaEscapada = descripcionDetallada.replace(
          /"/g,
          "&quot;",
        );
        var html =
          '<tr data-id-partida="' +
          partida.idRequiDetalle +
          '">' +
          '<td class="wizard-proveedor-num-partida">' +
          numeroPartida +
          "</td>" +
          '<td class="wizard-proveedor-desc-partida">' +
          "<strong>" +
          descripcion +
          "</strong>" +
          "</td>" +
          '<td class="wizard-proveedor-desc-detallada-partida">' +
          '<div class="desc-preview-modal" style="width:430px; min-width:430px; max-width:430px;" data-full="' +
          descripcionDetalladaEscapada +
          '" onclick="verDescDetalleModal(this)">' +
          '<span class="desc-texto-preview ' +
          tieneDescripcionDetallada +
          '">' +
          descripcionDetalladaCorta +
          "</span>" +
          '<i class="fa-solid fa-eye desc-icon"></i></div>' +
          "</td>" +
          '<td><input type="text" class="form-control wizard-proveedor-precio" ' +
          'inputmode="decimal" placeholder="0.00" autocomplete="off" value="' +
          (fila.importe || "") +
          '" /></td>' +
          '<td style="text-align:center;">' +
          '<input type="checkbox" class="wizard-proveedor-iva" ' +
          (fila.iva ? "checked " : "") +
          'style="width:18px; height:18px; cursor:pointer; accent-color:#fe6291; margin:0;" />' +
          "</td>" +
          "</tr>";

        $tbody.append(html);
      });
    }

    function actualizarBotonesWizard() {
      $("#btnWizardAnterior").toggle(proveedorIdx > 0);

      if (proveedorIdx === proveedoresWizard.length - 1) {
        $("#btnWizardSiguiente").html(
          'Guardar cotizaciones <i class="fa-solid fa-floppy-disk"></i>',
        );
      } else {
        $("#btnWizardSiguiente").html(
          'Siguiente <i class="fa-solid fa-chevron-right"></i>',
        );
      }
    }

    function renderizarProveedorActual() {
      if (!proveedoresWizard.length) {
        proveedoresWizard = [crearProveedorVacio()];
        proveedorIdx = 0;
      }

      var proveedor = completarPartidasProveedor(
        proveedoresWizard[proveedorIdx],
      );
      proveedoresWizard[proveedorIdx] = proveedor;

      $("#wizardPasoGanador").hide();
      $("#wizardProveedorActual").show();
      $("#modalProveedoresFilas").hide();
      $("#btnModalProveedoresAgregar").show();
      $("#btnWizardSiguiente").show();

      document.getElementById("wizardProveedoresSubtitulo").textContent =
        "Proveedor " + (proveedorIdx + 1) + " de " + proveedoresWizard.length;

      inicializarSelectProveedor(proveedor.idProveedor || 0);
      renderizarPartidasProveedor(proveedor);
      actualizarBotonesWizard();
    }

    function guardarProveedorActual() {
      if (!proveedoresWizard.length) return;

      var proveedor = proveedoresWizard[proveedorIdx] || crearProveedorVacio();
      proveedor.idProveedor = parseInt($selectProveedor().val(), 10) || 0;
      proveedor.partidas = [];

      $bodyPartidas()
        .find("tr")
        .each(function () {
          proveedor.partidas.push({
            idPartida: parseInt($(this).attr("data-id-partida"), 10) || 0,
            importe: (
              $(this).find(".wizard-proveedor-precio").val() || ""
            ).trim(),
            iva: $(this).find(".wizard-proveedor-iva").prop("checked"),
          });
        });

      proveedoresWizard[proveedorIdx] = proveedor;
    }

    $(document).off(
      "input.precioProveedor",
      ".wizard-proveedor-precio, .modal-proveedores-input-precio",
    );
    $(document).on(
      "input.precioProveedor",
      ".wizard-proveedor-precio, .modal-proveedores-input-precio",
      function () {
        this.value = limpiarPrecioProveedor(this.value);
      },
    );

    function validarProveedorActual() {
      guardarProveedorActual();

      var proveedor = proveedoresWizard[proveedorIdx];
      if (!proveedor || !proveedor.idProveedor) {
        Swal.fire({ icon: "warning", title: "Selecciona un proveedor." });
        return false;
      }

      var repetido = proveedoresWizard.some(function (item, idx) {
        return (
          idx !== proveedorIdx && item.idProveedor === proveedor.idProveedor
        );
      });

      if (repetido) {
        Swal.fire({
          icon: "warning",
          title: "Ese proveedor ya fue capturado.",
        });
        return false;
      }

      var faltaPrecio = proveedor.partidas.some(function (fila) {
        var importe = parseFloat(String(fila.importe).replace(/,/g, ""));
        return !fila.importe || isNaN(importe) || importe <= 0;
      });

      if (faltaPrecio) {
        Swal.fire({
          icon: "warning",
          title: "Captura el precio de todas las partidas.",
        });
        return false;
      }

      return true;
    }

    function construirCotizacionesParaGuardar() {
      var cotizaciones = [];

      proveedoresWizard.forEach(function (proveedor) {
        proveedor.partidas.forEach(function (fila) {
          cotizaciones.push({
            idProveedor: proveedor.idProveedor,
            importe: parseFloat(String(fila.importe).replace(/,/g, "")) || 0,
            idPartida: fila.idPartida,
            iva: fila.iva === true,
          });
        });
      });

      return cotizaciones;
    }

    function asegurarControlesGanador() {
      if (!document.getElementById("wizardGanadorJustificacionWrap")) {
        $("#wizardPasoGanador").append(
          '<div id="wizardGanadorJustificacionWrap" style="display:none; margin-top:12px;">' +
            '<div class="formulario-input">' +
            "<label>Justificación de selección manual</label>" +
            '<textarea id="wizardGanadorJustificacion" class="form-control" rows="4" ' +
            'placeholder="Escribe por qué se eligió este proveedor y no el de menor costo..."></textarea>' +
            "</div>" +
            "</div>",
        );
      }

      $("#btnConfirmarGanadorWrap").remove();
      $("#wizardPasoGanador").append(
        '<div id="btnConfirmarGanadorWrap" style="margin-top:16px;text-align:right;">' +
          '<button type="button" class="btn boton-rosa" id="btnConfirmarGanador">' +
          '<i class="fa-solid fa-trophy"></i> Confirmar ganador' +
          "</button>" +
          "</div>",
      );
      }


    function actualizarJustificacionGanador() {
      var sugerido = wizardOpciones.find(function (op) {
        return op.esSugerido;
      });
      var esManual = !!sugerido && wizardIdGanador !== sugerido.idProveedor;
      var $wrap = $("#wizardGanadorJustificacionWrap");
      var $txt = $("#wizardGanadorJustificacion");

      wizardGanadorManual = esManual;

      if (esManual) {
        $wrap.show();
        $txt.prop("disabled", false);
        return;
      }

      $wrap.hide();
      $txt.val("").prop("disabled", true);
    }

    function pintarOpcionesGanador() {
      var $divOpciones = $("#wizardGanadorOpciones");
      $divOpciones.empty();

      if (!wizardOpciones.length) {
        $divOpciones.html(
          '<p style="color:#888;font-size:13px;font-style:italic;">Sin proveedores con precios registrados.</p>',
        );
        return;
      }

      var fmt = function (v) {
        return v.toLocaleString("es-MX", {
          style: "currency",
          currency: "MXN",
        });
      };

      wizardOpciones.forEach(function (op) {
        var esSeleccionado = op.idProveedor === wizardIdGanador;
        var badge = op.esSugerido
          ? '<span style="font-size:10px;background:#fef9c3;color:#854d0e;padding:2px 7px;border-radius:10px;margin-left:6px;">Menor costo</span>'
          : "";

        var $fila = $("<div>")
          .attr("data-id-proveedor", op.idProveedor)
          .css({
            display: "flex",
            alignItems: "center",
            gap: "10px",
            padding: "10px 14px",
            borderRadius: "8px",
            cursor: "pointer",
            border: esSeleccionado
              ? "2px solid #fe6291"
              : "1px solid var(--color-border-tertiary)",
            background: esSeleccionado
              ? "#fff0f5"
              : "var(--color-background-secondary)",
            marginBottom: "4px",
            transition: "all .15s",
          })
          .html(
            '<div style="display:flex;align-items:center;gap:6px;flex:1;">' +
              '<i class="fa-solid fa-' +
              (esSeleccionado ? "circle-dot" : "circle") +
              '" style="color:' +
              (esSeleccionado ? "#fe6291" : "#9ca3af") +
              ';font-size:15px;"></i>' +
              '<span style="font-size:13px;font-weight:500;">' +
              op.nombreProveedor +
              badge +
              "</span>" +
              "</div>" +
              '<div style="text-align:right;font-size:12px;color:var(--color-text-secondary);">' +
              "<div>Subtotal: " +
              fmt(op.subtotal) +
              "</div>" +
              "<div>IVA: " +
              fmt(op.iva) +
              "</div>" +
              '<div style="font-weight:600;color:var(--color-text-primary);">Total: ' +
              fmt(op.total) +
              "</div>" +
              "</div>",
          );

        $fila.on("click", function () {
          wizardIdGanador = op.idProveedor;
          pintarOpcionesGanador();
        });

        $divOpciones.append($fila);
      });

      actualizarJustificacionGanador();
    }

      function mostrarPasoGanador() {
          $("#wizardProveedorActual").hide();
          $("#modalProveedoresFilas").hide();
          $("#btnModalProveedoresAgregar").hide();
          $("#btnWizardSiguiente").hide();
          $("#btnWizardAnterior").hide();
          document.getElementById("wizardProveedoresSubtitulo").textContent =
              "Selecciona el proveedor ganador";

          asegurarControlesGanador();

          if (window._modoConsolidada) {
              // ── Recopilar todos los idRequisicion del grupo ──
              var idSet = {};
              CACHE_PARTIDAS.forEach(function (p) {
                  if (!p.idRequisicionPorDetalle) return;
                  Object.values(p.idRequisicionPorDetalle).forEach(function (idReq) {
                      idSet[idReq] = true;
                  });
              });
              var idsRequisiciones = Object.keys(idSet).map(Number);

              if (!idsRequisiciones.length) {
                  Swal.fire({ icon: "error", title: "No se pudieron determinar las requisiciones." });
                  return;
              }

              // Construir query string con múltiples ids: ?idsRequisiciones=1&idsRequisiciones=2
              var params = idsRequisiciones.map(function (id) {
                  return "idsRequisiciones=" + id;
              }).join("&");

              $.get(urlObtenerOpcionesGanadorConsolidada + "?" + params, function (opciones) {
                  wizardOpciones = opciones || [];

                  // Para obtener ganador guardado usamos la primera requisición como referencia
                  $.get(urlObtenerGanador, { idRequisicion: idsRequisiciones[0] }, function (ganador) {
                      var sugerido = wizardOpciones.find(function (op) { return op.esSugerido; });
                      if (ganador && ganador.idProveedor) {
                          wizardIdGanador = ganador.idProveedor;
                          $("#wizardGanadorJustificacion").val(ganador.justificacion || "");
                      } else {
                          wizardIdGanador = sugerido ? sugerido.idProveedor : 0;
                          $("#wizardGanadorJustificacion").val("");
                      }
                      pintarOpcionesGanador();
                      $("#wizardPasoGanador").show();
                  }).fail(function () {
                      var sugerido = wizardOpciones.find(function (op) { return op.esSugerido; });
                      wizardIdGanador = sugerido ? sugerido.idProveedor : 0;
                      pintarOpcionesGanador();
                      $("#wizardPasoGanador").show();
                  });

              }).fail(function () {
                  Swal.fire({ icon: "error", title: "Error al cargar opciones de ganador." });
                  $("#wizardProveedorActual").show();
                  $("#btnModalProveedoresAgregar").show();
                  $("#btnWizardSiguiente").show();
                  $("#btnWizardAnterior").show();
              });

          } else {
              // ── Modo normal — sin cambios ──
              $.get(urlObtenerOpcionesGanador, { idRequisicion: _idRequiCotizaciones }, function (opciones) {
                  wizardOpciones = opciones || [];
                  $.get(urlObtenerGanador, { idRequisicion: _idRequiCotizaciones }, function (ganador) {
                      var sugerido = wizardOpciones.find(function (op) { return op.esSugerido; });
                      if (ganador && ganador.idProveedor) {
                          wizardIdGanador = ganador.idProveedor;
                          $("#wizardGanadorJustificacion").val(ganador.justificacion || "");
                      } else {
                          wizardIdGanador = sugerido ? sugerido.idProveedor : 0;
                          $("#wizardGanadorJustificacion").val("");
                      }
                      pintarOpcionesGanador();
                      $("#wizardPasoGanador").show();
                  }).fail(function () {
                      var sugerido = wizardOpciones.find(function (op) { return op.esSugerido; });
                      wizardIdGanador = sugerido ? sugerido.idProveedor : 0;
                      pintarOpcionesGanador();
                      $("#wizardPasoGanador").show();
                  });
              }).fail(function () {
                  Swal.fire({ icon: "error", title: "Error al cargar opciones de ganador." });
                  $("#wizardProveedorActual").show();
                  $("#btnModalProveedoresAgregar").show();
                  $("#btnWizardSiguiente").show();
                  $("#btnWizardAnterior").show();
              });
          }
      }

      window.cargarConsolidadas = function () {
          var tbody = document.getElementById("tbodyConsolidadas");
          if (!tbody) return;

          // Si ya tiene datos reales, no recargar
          if (tbody.getAttribute("data-cargado") === "1") return;

          fetch(urlListarConsolidadas + "?servicio=" + esTablaServicios, { credentials: "same-origin" })
              .then(function (r) { return r.json(); })
              .then(function (data) {
                  tbody.setAttribute("data-cargado", "1");

                  if (!data || !data.length) {
                      tbody.innerHTML =
                          '<tr class="fila-vacia"><td colspan="9" class="text-center">' +
                          "No hay requisiciones consolidadas</td></tr>";
                      return;
                  }

                  tbody.innerHTML = "";
                  data.forEach(function (c, idx) {
                      var tr = document.createElement("tr");
                      tr.className = "fila-requi";
                      tr.setAttribute("data-consolidada-id", c.consolidadaID);
                      var folio = escapeHtml(c.folioConsolidada || "—");
                      tr.innerHTML =
                          '<td style="text-align:center">' + (idx + 1) + "</td>" +
                          '<td><span class="folio-badge">' + folio + "</span></td>" +
                          "<td>" + escapeHtml(c.fechaCreacion || "—") + "</td>" +
                          "<td>" + renderTextoTablaPrincipal(c.departamentos || "—", "fa-solid fa-building") + "</td>" +
                          '<td style="text-align:center">' + (c.cantidadRequis || 0) + "</td>" +
                          '<td style="text-align:center">' + (c.totalPartidas || 0) + "</td>" +
                          "<td>" + obtenerBadgeEstatusHtml(c.idEstatus || 0, c.estatus || "—") + "</td>" +
                          "<td>" + renderTextoTablaPrincipal(c.creadoPor || "—", "fa-solid fa-user") + "</td>" +
                          '<td style="text-align:center">' +
                          '<div class="acciones-grupo" style="justify-content:center">' +
                          '<button class="btn-accion btn-ver" title="Ver detalle" ' +
                          'onclick="verDetalleConsolidada(' + c.consolidadaID + ')">' +
                          '<i class="fa-solid fa-eye"></i>' +
                          "</button>" +
                          (c.idEstatus === 2
                          ? '<button class="btn-accion" title="Atender" ' +
                          'onclick="atenderConsolidada(' + c.consolidadaID + ')">' +
                          '<i class="fa-solid fa-hand-holding"></i></button>'
                          : '') +
                          "</div>" +
                          "</td>";
                      tbody.appendChild(tr);
                  });
              })
              .catch(function () {
                  if (tbody) tbody.innerHTML =
                      '<tr class="fila-vacia"><td colspan="9" class="text-center">' +
                      "Error al cargar las consolidadas</td></tr>";
              });
      };

      window.cargarVerificadasConsolidadas = function () {
          var tbody = document.getElementById("tbodyVerificadasConsolidadas");
          if (!tbody) return;

          if (tbody.getAttribute("data-cargado") === "1") return;

          fetch(urlConsolidadasVerificadas + "?servicio=" + esTablaServicios, { credentials: "same-origin" })
              .then(function (r) { return r.json(); })
              .then(function (data) {
                  tbody.setAttribute("data-cargado", "1");

                  if (!data || !data.length) return;

                  // Solo eliminar el mensaje vacío si realmente hay datos que insertar
                  document.getElementById("filaVaciaVerificadas")?.remove();

                  tbody.innerHTML = "";
                  data.forEach(function (c, idx) {
                      var tr = document.createElement("tr");
                      tr.className = "fila-requi fila-consolidada";
                      tr.setAttribute("data-consolidada-id", c.consolidadaId);
                      var folioConsolidado = escapeHtml(c.folioConsolidada || "—");
                      tr.innerHTML =
                          '<td style="text-align:center">' + (idx + 1) + "</td>" +
                          '<td><div class="tabla-meta-stack"><span class="folio-badge">' + folioConsolidado + '</span><span class="tabla-meta-secundaria"><i class="fa-solid fa-layer-group"></i>Consolidada</span></div></td>' +
                          "<td>" + escapeHtml(c.fechaCreacion || "—") + "</td>" +
                          "<td>" + renderTextoTablaPrincipal(c.departamentos || "—", "fa-solid fa-building") + "</td>" +
                          '<td style="text-align:center">' + (c.cantidadRequisiciones || 0) + "</td>" +
                          '<td style="text-align:center">' + (c.totalPartidas || 0) + "</td>" +
                          "<td>" + obtenerBadgeEstatusHtml(c.idEstatus || 0, c.estatus || "—") + "</td>" +
                          '<td style="text-align:center">' +
                          '<div class="acciones-grupo" style="justify-content:center">' +
                          '<button class="btn-accion btn-ver" title="Ver expediente completo" ' +
                          'onclick="verExpedienteConsolidada(' + c.consolidadaId + ')">' +
                          '<i class="fa-solid fa-folder-open"></i>' +
                          "</button>" +
                          "</div>" +
                          "</td>";
                      tbody.appendChild(tr);
                  });

                  // Después de insertar filas, re-evaluar la paginación para remover/ajustar el mensaje vacío
                  aplicarPaginacionRequisiciones();
              })
              .catch(function () {
                  if (tbody) tbody.innerHTML =
                      '<tr class="fila-vacia"><td colspan="8" class="text-center">' +
                      "Error al cargar las consolidadas en verificadas</td></tr>";
              });
      };

      window.cargarConsolidadasAutorizadas = function () {
          var tbody = document.getElementById("tbodyAutorizadasConsolidadas");
          if (!tbody) return;

          if (tbody.getAttribute("data-cargado") === "1") return;

          if (!urlConsolidadasAutorizadas) return;

          fetch(urlConsolidadasAutorizadas + "?servicio=" + esTablaServicios, { credentials: "same-origin" })
              .then(function (r) { return r.json(); })
              .then(function (data) {
                  tbody.setAttribute("data-cargado", "1");

                  if (!data || !data.length) return;

                  document.getElementById("filaVaciaAutorizadas")?.remove();

                  tbody.innerHTML = "";
                  data.forEach(function (c, idx) {
                      var tr = document.createElement("tr");
                      tr.className = "fila-requi fila-consolidada";
                      tr.setAttribute("data-consolidada-id", c.consolidadaID);
                      tr.innerHTML =
                          '<td style="text-align:center">' + (idx + 1) + "</td>" +
                          "<td><strong>" + (c.folioConsolidada || "—") + " <span style='font-size:10px;color:var(--color-text-secondary);'>(<i class='fa-solid fa-layer-group'></i> Consolidada)</span></strong></td>" +
                          "<td>" + (c.fechaCreacion || "—") + "</td>" +
                          '<td style="max-width:200px;white-space:normal;font-size:12px;">' +
                          (c.departamentos || "—") + "</td>" +
                          '<td style="text-align:center">' + (c.cantidadRequis || 0) + "</td>" +
                          '<td style="text-align:center">' + (c.totalPartidas || 0) + "</td>" +
                          "<td>" + (c.estatus || "—") + "</td>" +
                          '<td style="text-align:center">' +
                          '<div class="acciones-grupo" style="justify-content:center">' +
                          '<button class="btn-accion btn-ver" title="Ver detalle" ' +
                          'onclick="verDetalleConsolidada(' + c.consolidadaID + ')">' +
                          '<i class="fa-solid fa-eye"></i>' +
                          "</button>" +
                          "</div>" +
                          "</td>";
                      tbody.appendChild(tr);
                  });

                  aplicarPaginacionRequisiciones();
              })
              .catch(function () {
                  if (tbody) tbody.innerHTML =
                      '<tr class="fila-vacia"><td colspan="8" class="text-center">' +
                      "Error al cargar las consolidadas autorizadas</td></tr>";
              });
      };

      function guardarGanadorSeleccionado() {
          var justificacion = ($("#wizardGanadorJustificacion").val() || "").trim();

          if (!wizardIdGanador) {
              Swal.fire({ icon: "warning", title: "Selecciona el proveedor ganador." });
              return;
          }
          if (wizardGanadorManual && !justificacion) {
              Swal.fire({ icon: "warning", title: "Escribe la justificación." });
              return;
          }

          // Determinar a qué requisiciones guardar el ganador
          var idsRequisiciones = [];
          if (window._modoConsolidada) {
              // Obtener todos los idRequisicion únicos del mapa
              var idSet = {};
              CACHE_PARTIDAS.forEach(function (p) {
                  if (!p.idRequisicionPorDetalle) return;
                  Object.values(p.idRequisicionPorDetalle).forEach(function (idReq) {
                      idSet[idReq] = true;
                  });
              });
              idsRequisiciones = Object.keys(idSet).map(Number);
          } else {
              idsRequisiciones = [_idRequiCotizaciones];
          }

          var promesas = idsRequisiciones.map(function (idReq) {
              return $.ajax({
                  url: urlGuardarGanador,
                  type: "POST",
                  contentType: "application/json",
                  data: JSON.stringify({
                      idRequisicion: idReq,
                      idProveedor: wizardIdGanador,
                      seleccionManual: wizardGanadorManual,
                      justificacion: wizardGanadorManual ? justificacion : ""
                  })
              });
          });

          $.when.apply($, promesas)
              .done(function () {
                  window.cerrarSoloModalProveedores();
                  Swal.fire({
                      icon: "success",
                      title: "Proveedores y ganador guardados.",
                      confirmButtonText: "Aceptar"
                  });
              })
              .fail(function () {
                  Swal.fire({ icon: "error", title: "Error al guardar el ganador." });
              });
      }

    function reconstruirProveedoresDesdeCotizaciones(cotizaciones) {
      var mapa = {};

      (cotizaciones || []).forEach(function (cotizacion) {
        var idProveedor = parseInt(cotizacion.idProveedor, 10) || 0;
        if (!idProveedor) return;

        if (!mapa[idProveedor]) {
          mapa[idProveedor] = {
            idProveedor: idProveedor,
            partidas: [],
          };
        }

        mapa[idProveedor].partidas.push({
          idPartida: cotizacion.idPartida,
          importe: cotizacion.importe != null ? String(cotizacion.importe) : "",
          iva: cotizacion.iva === true,
        });
      });

      proveedoresWizard = Object.keys(mapa).map(function (idProveedor) {
        return completarPartidasProveedor(mapa[idProveedor]);
      });

      if (!proveedoresWizard.length) {
        proveedoresWizard = [crearProveedorVacio()];
      }
    }

    $(document).off("show.bs.modal", "#modalProveedoresRequisicion");
    $(document).off("hidden.bs.modal", "#modalProveedoresRequisicion");
    $(document).off("click", "#btnWizardSiguiente");
    $(document).off("click", "#btnWizardAnterior");
    $(document).off("click", "#btnModalProveedoresAgregar");
    $(document).off("click", ".btn-wizard-eliminar-fila");
    $(document).off(
      "change",
      "#modalProveedoresFilas .modal-proveedores-select",
    );
    $(document).off("click", "#btnConfirmarGanador");

      $(document).on("show.bs.modal", "#modalProveedoresRequisicion", function () {

          cargarCatalogoProveedores();
          proveedorIdx = 0;
          proveedoresWizard = [];
          wizardOpciones = [];
          wizardIdGanador = 0;
          wizardGanadorManual = false;

          $("#modalProveedoresFilas").hide();
          $("#wizardPasoGanador").hide();
          $("#wizardProveedorActual").show();
          $("#btnModalProveedoresAgregar").show();
          $("#btnWizardSiguiente").show();
          $("#btnWizardAnterior").hide();

          if (window._modoConsolidada && window._idConsolidadaWizard) {
              // ── Modo consolidada ──
              $.get(urlPartidasConsolidada,
                  { idConsolidada: window._idConsolidadaWizard },
                  function (partidas) {
                      CACHE_PARTIDAS = partidas.map(function (p) {
                          return {
                              idRequiDetalle: p.idRequiDetalle,
                              idArticulo: p.idArticulo,
                              nombrePartida: p.descripcion,
                              descripcion: p.descripcion,
                              descripcionDetallada: p.descripcionDetallada,
                              cantidad: p.cantidadTotal,
                              unidadMedida: p.unidadMedida,
                              numPartida: p.numPartida,
                              idsRequiDetalle: p.idsRequiDetalle,
                              idRequisicionPorDetalle: p.idRequisicionPorDetalle
                          };
                      });
                      CACHE_PARTIDAS_REQUI = "consolidada_" + _idConsolidadaWizard;

                      var idReqRef = CACHE_PARTIDAS.length && CACHE_PARTIDAS[0].idRequisicionPorDetalle
                          ? Object.values(CACHE_PARTIDAS[0].idRequisicionPorDetalle)[0]
                          : null;

                      if (idReqRef) {
                          $.get(urlObtenerCotizaciones,
                              { idRequisicion: idReqRef },
                              function (cotizaciones) {
                                  reconstruirProveedoresDesdeCotizaciones(cotizaciones);
                                  renderizarProveedorActual();
                              }
                          ).fail(function () {
                              proveedoresWizard = [crearProveedorVacio()];
                              renderizarProveedorActual();
                          });
                      } else {
                          proveedoresWizard = [crearProveedorVacio()];
                          renderizarProveedorActual();
                      }
                  }
              ).fail(function () {
                  Swal.fire({ icon: "error", title: "No se pudieron cargar las partidas." });
              });

          } else {
              // ── Modo normal ──
              if (!_idRequiCotizaciones) return;

              obtenerPartidasCotizacion(function () {
                  $.get(urlObtenerCotizaciones,
                      { idRequisicion: _idRequiCotizaciones },
                      function (cotizaciones) {
                          reconstruirProveedoresDesdeCotizaciones(cotizaciones);
                          renderizarProveedorActual();
                      }
                  ).fail(function () {
                      proveedoresWizard = [crearProveedorVacio()];
                      renderizarProveedorActual();
                  });
              });
          }
      });

    $(document).on("click", "#btnModalProveedoresAgregar", function () {
      if (!validarProveedorActual()) return;

      proveedoresWizard.push(crearProveedorVacio());
      proveedorIdx = proveedoresWizard.length - 1;
      renderizarProveedorActual();
    });

    $(document).on("click", "#btnWizardAnterior", function () {
      guardarProveedorActual();
      if (proveedorIdx <= 0) return;
      proveedorIdx--;
      renderizarProveedorActual();
    });

      $(document).on("click", "#btnWizardSiguiente", function () {
          if (!validarProveedorActual()) return;

          if (proveedorIdx < proveedoresWizard.length - 1) {
              proveedorIdx++;
              renderizarProveedorActual();
              return;
          }

          var cotizaciones = construirCotizacionesParaGuardar();
          if (!cotizaciones.length) {
              Swal.fire({ icon: "warning", title: "Agrega al menos un proveedor." });
              return;
          }

          if (window._modoConsolidada) {
              // expandir cotizaciones a todos los IdRequisicionDetalle del grupo
              var cotizacionesExpandidas = [];
              cotizaciones.forEach(function (cot) {
                  var partida = CACHE_PARTIDAS.find(function (p) {
                      return p.idRequiDetalle === cot.idPartida;
                  });
                  var ids = partida && partida.idsRequiDetalle && partida.idsRequiDetalle.length
                      ? partida.idsRequiDetalle : [cot.idPartida];
                  ids.forEach(function (idDetalle) {
                      cotizacionesExpandidas.push({
                          idProveedor: cot.idProveedor,
                          importe: cot.importe,
                          idPartida: idDetalle,
                          iva: cot.iva
                      });
                  });
              });

              // Agrupar por idRequisicion usando el mapa idRequisicionPorDetalle
              var cotsPorRequi = {};
              cotizacionesExpandidas.forEach(function (cot) {
                  CACHE_PARTIDAS.forEach(function (p) {
                      if (!p.idsRequiDetalle || p.idsRequiDetalle.indexOf(cot.idPartida) === -1) return;
                      var idReq = p.idRequisicionPorDetalle
                          ? p.idRequisicionPorDetalle[cot.idPartida] : null;
                      if (!idReq) return;
                      if (!cotsPorRequi[idReq]) cotsPorRequi[idReq] = [];
                      cotsPorRequi[idReq].push(cot);
                  });
              });

              var requis = Object.keys(cotsPorRequi);
              var promesas = requis.map(function (idReq) {
                  return $.ajax({
                      url: urlGuardarCotizaciones,
                      type: "POST",
                      contentType: "application/json",
                      data: JSON.stringify({
                          idRequisicion: parseInt(idReq),
                          cotizaciones: cotsPorRequi[idReq]
                      })
                  });
              });

              $.when.apply($, promesas)
                  .done(function () {
                      actualizarEstadoProveedoresSeleccionados(true);
                      var elEstado = document.getElementById("estadoProveedoresConsolidada");
                      if (elEstado) elEstado.style.display = "block";
                      var elTexto = document.getElementById("textoBtnProveedoresCons");
                      if (elTexto) elTexto.textContent = "Editar proveedores";
                      var btnCuadro = document.getElementById("btnCuadroConsolidada");
                      if (btnCuadro) btnCuadro.disabled = false;
                      mostrarPasoGanador();
                  })
                  .fail(function (xhr) {
                      var msg = xhr.responseJSON && xhr.responseJSON.message
                          ? xhr.responseJSON.message : "Error al guardar cotizaciones.";
                      Swal.fire({ icon: "error", title: msg });
                  });

              return;
          }

          // ── Modo normal ──
          $.ajax({
              url: urlGuardarCotizaciones,
              type: "POST",
              contentType: "application/json",
              data: JSON.stringify({
                  idRequisicion: _idRequiCotizaciones,
                  cotizaciones: cotizaciones
              }),
              success: function () {
                  actualizarEstadoProveedoresSeleccionados(true);
                  mostrarPasoGanador();
              },
              error: function (xhr) {
                  var mensaje = xhr.responseJSON && xhr.responseJSON.message
                      ? xhr.responseJSON.message : "Error al guardar las cotizaciones.";
                  Swal.fire({ icon: "error", title: mensaje });
              }
          });
      });

    $(document).on("click", "#btnConfirmarGanador", function () {
      guardarGanadorSeleccionado();
    });

    $(document).on(
      "hidden.bs.modal",
      "#modalProveedoresRequisicion",
        function () {
            this.style.zIndex = "";
            window._modoConsolidada = false;
            window._idConsolidadaWizard = null;
        if ($selectProveedor().data("select2")) {
          $selectProveedor().select2("destroy");
        }

        proveedoresWizard = [];
        proveedorIdx = 0;
        wizardOpciones = [];
        wizardIdGanador = 0;
        wizardGanadorManual = false;

        $("#wizardGanadorOpciones").empty();
        $("#wizardGanadorJustificacion").val("");
        $("#wizardGanadorJustificacionWrap").hide();
        $("#btnConfirmarGanadorWrap").remove();
        $("#wizardPasoGanador").hide();
        $("#wizardProveedorActual").show();
        $("#modalProveedoresFilas").hide();
        $("#btnModalProveedoresAgregar").show();
        $("#btnWizardSiguiente").show();
      },
    );
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
    if (_idRequiCotizaciones !== idMaestro) {
      CACHE_PARTIDAS = [];
      CACHE_PARTIDAS_REQUI = null;
    }

    _idRequiCotizaciones = idMaestro;
    // Limpiar contenedores al abrir
    var archivosReadonly = document.getElementById(
      "contenedorArchivosReadonly",
    );
    if (archivosReadonly) archivosReadonly.innerHTML = "";
    var galeriaFotosDetalle = document.getElementById("galeriaFotosDetalle");
    var seccionFotosDetalle = document.getElementById("seccionFotosDetalle");
    if (galeriaFotosDetalle) galeriaFotosDetalle.innerHTML = "";
    if (seccionFotosDetalle) seccionFotosDetalle.style.display = "none";
    actualizarEstadoProveedoresSeleccionados(false);

    // Mostrar/ocultar secciones de atender ANTES del $.get (esto no depende de data)
    const seccionesAtender = document.querySelectorAll(".seccionAtender");
    const isAtender = modo === "atender";
    const isReadonly = modo === "readonly";

    seccionesAtender.forEach(function (sec) {
      sec.style.display = isAtender || isReadonly ? "block" : "none";
    });

    if (!obtenerDetallesUrl) return;

    $.get(obtenerDetallesUrl, { idMaestro: idMaestro }, function (data) {
      cargarEstadoProveedoresSeleccionados(idMaestro);
      var articulos = data.articulos || [];
      var esDonativo = data.donativo === true;

      // Tabla de artículos
      var thCog = document.querySelector(
        "#tablaModalDetalle thead tr th:last-child",
      );
      if (thCog) thCog.style.display = esDonativo ? "" : "none";

      var contenido = "";
      if (articulos.length === 0) {
        contenido =
          '<tr><td colspan="' +
          (esDonativo ? 6 : 5) +
          '" class="text-center">Sin artículos</td></tr>';
      } else {
        articulos.forEach(function (item) {
          var textoCompleto = item.descripcionDetallada || "";
          var textoCorto =
            textoCompleto.length > 28
              ? textoCompleto.substring(0, 28) + "…"
              : textoCompleto || "Sin descripción...";
          var tieneTexto = textoCompleto ? "tiene-texto" : "";
          var fullEscapado = (textoCompleto || "").replace(/"/g, "&quot;");

          // Badge de estatus por partida
          var estatusPartida = item.estatusPartida || "";
          var coloresBadge = {
            "En compra": { bg: "#dbeafe", color: "#1d4ed8" },
            "En entrega": { bg: "#fef9c3", color: "#854d0e" },
            Entregado: { bg: "#dcfce7", color: "#166534" },
          };
          var badge = coloresBadge[estatusPartida]
            ? '<span style="font-size:11px;padding:2px 8px;border-radius:10px;' +
              "background:" +
              coloresBadge[estatusPartida].bg +
              ";" +
              "color:" +
              coloresBadge[estatusPartida].color +
              ';font-weight:600;">' +
              estatusPartida +
              "</span>"
            : "";

          var tdCog = esDonativo
            ? '<td><select class="select-cog-editable" style="width:120px;"></select></td>'
            : "";

          contenido +=
            "<tr>" +
            "<td>" +
            (item.numPartida || "") +
            "</td>" +
            "<td>" +
            (item.cantidad || "") +
            "</td>" +
            "<td>" +
            (item.unidadMedida || "") +
            "</td>" +
            "<td>" +
            (item.descripcion || "") +
            "</td>" +
            '<td><div class="desc-preview-modal" data-full="' +
            fullEscapado +
            '" onclick="verDescDetalleModal(this)">' +
            '<span class="desc-texto-preview ' +
            tieneTexto +
            '">' +
            textoCorto +
            "</span>" +
            '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
            '<td style="text-align:center">' +
            badge +
            "</td>" +
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
              cache: true,
            },
          });
        });
      }

      // Fotos de diseño / Archivos adjuntos
      var seccionFotos = document.getElementById("seccionFotosDetalle");
      var galeriaFotos = document.getElementById("galeriaFotosDetalle");
      if (seccionFotos && galeriaFotos) {
        // Se muestran si hay fotos, sin restringir a Servicio Impresión para que funcione igual al enviar adjuntos comunes
        if (data.fotos && data.fotos.length > 0) {
          var objsFotos = data.fotos.map(function (r) {
            return { ruta: r, nombreArchivo: r.split("/").pop() || "archivo" };
          });

          if (
            window.ModalAdjuntos &&
            typeof window.ModalAdjuntos.renderGrupoHtml === "function"
          ) {
            galeriaFotos.innerHTML = window.ModalAdjuntos.renderGrupoHtml(
              "",
              objsFotos,
            );
            window.ModalAdjuntos.enlazarEventosContenedor(galeriaFotos);
          }
          seccionFotos.style.display = "block";
        } else {
          seccionFotos.style.display = "none";
          galeriaFotos.innerHTML = "";
        }
      }

      // Subtítulo
      var subtitulo = document.querySelector(
        "#modalDetalle .modal-subtitulo-premium",
      );
      if (subtitulo)
        subtitulo.textContent =
          "Detalle de partidas solicitadas · Total: " +
          articulos.length +
          " partidas";

      // ── Sección atender/readonly: inicializar select2 y precargar valores ──
      if (isAtender || isReadonly) {
        // Destruir instancias previas si existen
        $(
          "#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio",
        ).each(function () {
          if ($(this).data("select2")) $(this).select2("destroy");
        });

        // Inicializar select2
        $(
          "#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio",
        ).select2({
          dropdownParent: $("#modalDetalle"),
          width: "100%",
          language: "es",
        });

        // Precargar valores (ahora sí data existe)
        if (data.idPp) {
          $("#actividadSeleccionada").val(data.idPp).trigger("change");
        }
        if (data.ff) {
            $("#ffSelect").val(data.ff).trigger("change");
        }
        if (data.tipoPrograma) {
          $("#tipoProgramaSelect option")
            .filter(function () {
              return $(this).text().trim() === data.tipoPrograma;
            })
            .prop("selected", true);
          $("#tipoProgramaSelect").trigger("change");
        }

        // Si es readonly: deshabilitar todo y mostrar archivos
        if (isReadonly) {
          $(
            "#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio",
          ).prop("disabled", true);
          $("#txtObservaciones").prop("readonly", true);
          if (data.observaciones) {
            $("#txtObservaciones").val(data.observaciones);
          }
          $("#inputCotizaciones, #inputCuadroComparativo").prop(
            "disabled",
            true,
          );

          // Ocultar botón enviar
          var btnEnviar = document.querySelector(
            ".seccionAtender div[style*='text-align:right']",
          );
          if (btnEnviar) btnEnviar.style.display = "none";

          document
            .querySelectorAll(".seccionAtender .row.mb-3")
            .forEach(function (row) {
              row.style.display = "none";
            });

          // Mostrar archivos subidos
          renderizarArchivosReadonly(
            data.cotizaciones || [],
            data.cuadroComparativo || [],
          );

          (function () {
            var c = document.getElementById("contenedorArchivosReadonly");
            if (!c || !window.ModalAdjuntos) return;
            if (!(data.anexos || []).length) return;
            var div = document.createElement("div");
            div.innerHTML = window.ModalAdjuntos.renderGrupoHtml(
              "Documentos Anexos",
              data.anexos,
            );
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
    window._expedienteActualGlobal = idRequi;

    // Limpiar
    document.getElementById("expObservaciones").value = "";
    document.getElementById("expArchivosBase").innerHTML = "";
    document.getElementById("expGrupoSiaf").innerHTML = "";
    document.getElementById("expGrupoTablaApi").innerHTML = "";
    document.getElementById("expGrupoNumeroApi").innerHTML = "";
    document.getElementById("expArchivosFinancieros").style.display = "none";
    document.getElementById("expGrupoPedidoCompra").innerHTML = "";
    document.getElementById("pedidoSinArchivo").style.display = "none";
    document.getElementById("pedidoModoEdicion").style.display = "none";
    document.getElementById("pedidoModoReadonly").style.display = "none";
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
        contenido =
          '<tr><td colspan="5" class="text-center">Sin artículos</td></tr>';
      } else {
        articulos.forEach(function (item) {
          var textoCompleto = item.descripcionDetallada || "";
          var textoCorto =
            textoCompleto.length > 28
              ? textoCompleto.substring(0, 28) + "…"
              : textoCompleto || "Sin descripción...";
          var fullEscapado = (textoCompleto || "").replace(/"/g, "&quot;");
          contenido +=
            "<tr>" +
            "<td>" +
            (item.numPartida || "") +
            "</td>" +
            "<td>" +
            (item.cantidad || "") +
            "</td>" +
            "<td>" +
            (item.unidadMedida || "") +
            "</td>" +
            "<td>" +
            (item.descripcion || "") +
            "</td>" +
            '<td><div class="desc-preview-modal" data-full="' +
            fullEscapado +
            '" onclick="verDescDetalleModal(this)">' +
            '<span class="desc-texto-preview' +
            (textoCompleto ? " tiene-texto" : "") +
            '">' +
            textoCorto +
            "</span>" +
            '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
            "</tr>";
        });
      }
      document.getElementById("tablaExpedienteBody").innerHTML = contenido;

      // Fotos
      if (
        data.tipoServicio === "Servicio Impresion" &&
        data.fotos &&
        data.fotos.length
      ) {
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
      ["expActividad", "expFf", "expTipoPrograma", "expMunicipio"].forEach(
        function (id) {
          var el = $("#" + id);
          if (el.data("select2")) el.select2("destroy");
        },
      );
      $("#expActividad, #expFf, #expTipoPrograma, #expMunicipio")
        .select2({
          dropdownParent: $("#modalExpediente"),
          width: "100%",
          language: "es",
        })
        .prop("disabled", true);

      if (data.idPp) $("#expActividad").val(data.idPp).trigger("change");
      if (data.ff) {
        $("#expFf").val(data.ff).trigger("change");
      }
      if (data.tipoPrograma) {
        $("#expTipoPrograma option")
          .filter(function () {
            return $(this).text().trim() === data.tipoPrograma;
          })
          .prop("selected", true);
        $("#expTipoPrograma").trigger("change");
      }

      // Cotizaciones / cuadro
      (function () {
        var contenedor = document.getElementById("expArchivosBase");
        contenedor.innerHTML = "";

        // Botón cotizaciones
        var btnCot = document.createElement("div");
        btnCot.style.cssText = "margin-bottom:12px;";
        btnCot.innerHTML =
          '<button type="button" class="btn boton-rosa" ' +
          'onclick="verCotizaciones(' +
          idRequi +
          ')">' +
          '<i class="fa-solid fa-file-invoice-dollar"></i> Ver cotizaciones' +
          "</button>";
        contenedor.appendChild(btnCot);

        // Cuadro comparativo (se mantiene)
        if (window.ModalAdjuntos && (data.cuadroComparativo || []).length > 0) {
          var divCuadro = document.createElement("div");
          divCuadro.innerHTML = window.ModalAdjuntos.renderGrupoHtml(
            "Cuadro comparativo",
            data.cuadroComparativo,
          );
          window.ModalAdjuntos.enlazarEventosContenedor(divCuadro);
          contenedor.appendChild(divCuadro);
        }

        // Anexos
        if (window.ModalAdjuntos && (data.anexos || []).length > 0) {
          var divAnexos = document.createElement("div");
          divAnexos.innerHTML = window.ModalAdjuntos.renderGrupoHtml(
            "Documentos Anexos",
            data.anexos,
          );
          window.ModalAdjuntos.enlazarEventosContenedor(divAnexos);
          contenedor.appendChild(divAnexos);
        }
      })();

      // Observaciones financieros
      document.getElementById("expObservaciones").value =
        data.observaciones || "";

      // Documentos financieros
      (function () {
        var siaf = data.archivosSiaf || [];
        var tablaApi = data.archivosTablaApi || [];
        var numApi = data.numeroApi || null;
        var hayContenido = siaf.length || tablaApi.length || numApi;
        if (!hayContenido) return;

        document.getElementById("expArchivosFinancieros").style.display =
          "block";

        function renderGrupoFin(elId, titulo, archivos) {
          var el = document.getElementById(elId);
          if (!archivos.length) {
            el.innerHTML = "";
            return;
          }

          if (window.ModalAdjuntos) {
            el.innerHTML = window.ModalAdjuntos.renderGrupoHtml(
              titulo,
              archivos,
            );
            window.ModalAdjuntos.enlazarEventosContenedor(el);
          } else {
            el.innerHTML = "";
          }
        }

        renderGrupoFin("expGrupoSiaf", "Documento SIAF", siaf);
        renderGrupoFin("expGrupoTablaApi", "Tabla de API", tablaApi);

        if (numApi) {
          document.getElementById("expGrupoNumeroApi").innerHTML =
            '<label style="font-size:13px;font-weight:600;color:#555;margin-bottom:4px;display:block;">Nº API</label>' +
            '<span style="font-size:13px;padding:4px 10px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;color:#166534;">' +
            '<i class="fa-solid fa-hashtag" style="margin-right:4px;"></i>' +
            numApi +
            "</span>";
        }
      })();

      expedienteEstatusActual = data.idEstatus || 0;

      expedienteNotaActual = "";
      if (data.idEstatus === 18 && data.observaciones) {
        expedienteNotaActual = data.observaciones;
      }

      expedienteEstatusActual = data.idEstatus || 0;
      expedienteNotaActual =
        data.idEstatus === 18 && data.observaciones ? data.observaciones : "";

      $.get(
        urlObtenerDocsProveedor,
        { idRequisicion: idRequi },
        function (docs) {
          renderChecklist(
            docs,
            idRequi,
            expedienteEstatusActual,
            expedienteNotaActual,
          );
        },
      );

      // ── Sección Pedido de Compra ──────────────────────────────────────────
      (function () {
        var idEstatus = data.idEstatus || 0;
        var pedidoCompra = data.archivosPedidoCompra || [];

        // Estatus 15 (autorizada) o 18 (rebotada): modo edición
        if (idEstatus === 15) {
          document.getElementById("pedidoModoEdicion").style.display = "flex";
          document.getElementById("pedidoModoReadonly").style.display = "none";
          // Limpiar el input por si quedó algo de una apertura anterior
          var inputPedido = document.getElementById("inputSubirPedido");
          if (inputPedido) inputPedido.value = "";
        } else {
          // Cualquier otro estatus (17, etc.): solo lectura
          document.getElementById("pedidoModoEdicion").style.display = "none";
          document.getElementById("pedidoModoReadonly").style.display = "block";

          var elGrupo = document.getElementById("expGrupoPedidoCompra");
          if (pedidoCompra.length && window.ModalAdjuntos) {
            elGrupo.innerHTML = window.ModalAdjuntos.renderGrupoHtml(
              "Documento de pedido",
              pedidoCompra,
            );
            window.ModalAdjuntos.enlazarEventosContenedor(elGrupo);
            document.getElementById("pedidoSinArchivo").style.display = "none";
          } else {
            elGrupo.innerHTML = "";
            document.getElementById("pedidoSinArchivo").style.display = "block";
          }
        }
      })();

      new bootstrap.Modal(document.getElementById("modalExpediente")).show();
    });
  };

  window.verExpedienteConsolidada = function (idConsolidada) {
    window._modoConsolidada = true;
    window._expedienteActualGlobal = null;
    window._idConsolidadaExpediente = idConsolidada;

    document.getElementById("expObservaciones").value = "";
    document.getElementById("expArchivosBase").innerHTML = "";
    document.getElementById("expGrupoSiaf").innerHTML = "";
    document.getElementById("expGrupoTablaApi").innerHTML = "";
    document.getElementById("expGrupoNumeroApi").innerHTML = "";
    document.getElementById("expArchivosFinancieros").style.display = "none";
    document.getElementById("expGrupoPedidoCompra").innerHTML = "";
    document.getElementById("pedidoSinArchivo").style.display = "none";
    document.getElementById("pedidoModoEdicion").style.display = "none";
    document.getElementById("pedidoModoReadonly").style.display = "none";
    document.getElementById("expGaleriaFotos").innerHTML = "";
    document.getElementById("expSeccionFotos").style.display = "none";
    document.getElementById("tablaExpedienteBody").innerHTML = "";
    document.getElementById("expedienteSubtitulo").textContent = "Cargando...";

    $.get(urlExpedienteConsolidada, { idConsolidada: idConsolidada }, function (data) {
      var articulos = data.articulos || [];
      document.getElementById("expedienteSubtitulo").textContent =
        "Expediente consolidado · " + articulos.length + " partidas | " + data.folioConsolidada;

      // Agregar columna Requi al inicio del thead
      var theadTr = document.querySelector("#tablaExpedienteDetalle thead tr");
      if (theadTr) {
        theadTr.innerHTML = '<th>Requi</th><th>Nº Partida</th><th>Cantidad</th><th>Unidad Medida</th><th>Descripción</th><th>Descripción Detallada</th>';
      }

      // Tabla artículos con columna Requi al inicio
      var contenido = "";
      if (!articulos.length) {
        contenido = '<tr><td colspan="6" class="text-center">Sin artículos</td></tr>';
      } else {
        articulos.forEach(function (item) {
          var textoCompleto = item.descripcionDetallada || "";
          var textoCorto = textoCompleto.length > 28
            ? textoCompleto.substring(0, 28) + "…"
            : textoCompleto || "Sin descripción...";
          var fullEscapado = (textoCompleto || "").replace(/"/g, "&quot;");
          contenido +=
            "<tr>" +
            "<td>" + (item.numRequiOrigen || "") + "</td>" +
            "<td>" + (item.numPartida || "") + "</td>" +
            "<td>" + (item.cantidad || "") + "</td>" +
            "<td>" + (item.unidadMedida || "") + "</td>" +
            "<td>" + (item.descripcion || "") + "</td>" +
            '<td><div class="desc-preview-modal" data-full="' +
            fullEscapado +
            '" onclick="verDescDetalleModal(this)">' +
            '<span class="desc-texto-preview' + (textoCompleto ? " tiene-texto" : "") + '">' +
            textoCorto +
            "</span>" +
            '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
            "</tr>";
        });
      }
      document.getElementById("tablaExpedienteBody").innerHTML = contenido;

      // Selects PP / FF (readonly)
      ["expActividad", "expFf", "expTipoPrograma"].forEach(function (id) {
        var el = $("#" + id);
        if (el.data("select2")) el.select2("destroy");
      });
      $("#expActividad, #expFf, #expTipoPrograma")
        .select2({ dropdownParent: $("#modalExpediente"), width: "100%", language: "es" })
        .prop("disabled", true);

      if (data.idPp) $("#expActividad").val(data.idPp).trigger("change");
      if (data.ff) $("#expFf").val(data.ff).trigger("change");
      if (data.tipoPrograma) {
        $("#expTipoPrograma option")
          .filter(function () { return $(this).text().trim() === data.tipoPrograma; })
          .prop("selected", true);
        $("#expTipoPrograma").trigger("change");
      }

      // Cuadro comparativo / anexos
      (function () {
        var contenedor = document.getElementById("expArchivosBase");
        contenedor.innerHTML = "";

        if (window.ModalAdjuntos && (data.cuadroComparativo || []).length > 0) {
          var divCuadro = document.createElement("div");
          divCuadro.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
          window.ModalAdjuntos.enlazarEventosContenedor(divCuadro);
          contenedor.appendChild(divCuadro);
        }
        if (window.ModalAdjuntos && (data.anexos || []).length > 0) {
          var divAnexos = document.createElement("div");
          divAnexos.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
          window.ModalAdjuntos.enlazarEventosContenedor(divAnexos);
          contenedor.appendChild(divAnexos);
        }
      })();

      // Observaciones
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
          }
        }
        renderGrupoFin("expGrupoSiaf", "Documento SIAF", siaf);
        renderGrupoFin("expGrupoTablaApi", "Tabla de API", tablaApi);
        if (numApi) {
          document.getElementById("expGrupoNumeroApi").innerHTML =
            '<label style="font-size:13px;font-weight:600;color:#555;margin-bottom:4px;display:block;">Nº API</label>' +
            '<span style="font-size:13px;padding:4px 10px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;color:#166534;">' +
            '<i class="fa-solid fa-hashtag" style="margin-right:4px;"></i>' + numApi + "</span>";
        }
      })();

      // Checklist documentos proveedor (consolidada)
      expedienteEstatusActual = data.idEstatus || 0;
      expedienteNotaActual = data.idEstatus === 18 && data.observaciones ? data.observaciones : "";

      // Documentos del proveedor para consolidada
      $.get(urlObtenerDocsProveedorConsolidada, { idConsolidada: idConsolidada }, function (docs) {
        renderChecklist(docs, "cons_" + idConsolidada, expedienteEstatusActual, expedienteNotaActual, true);
      });

      // Pedido de compra consolidado
      document.getElementById("pedidoModoEdicion").style.display = "flex";
      document.getElementById("pedidoModoReadonly").style.display = "none";
      var editarBtn = document.querySelector("#pedidoModoEdicion .btn");
      if (editarBtn && urlEditarPedidoConsolidada) {
        editarBtn.setAttribute("data-consolidada-override", "true");
        editarBtn.onclick = function () {
          window.open(urlEditarPedidoConsolidada + "?idConsolidada=" + idConsolidada, "_blank");
        };
        editarBtn.innerHTML = '<i class="fa-solid fa-pen-to-square"></i> Editar Pedido de Compra';
      }

      new bootstrap.Modal(document.getElementById("modalExpediente")).show();
    });
  };

  window.aceptarExpediente = function () {
    var esConsolidada = window._modoConsolidada && window._idConsolidadaExpediente;
    var titulo = esConsolidada ? "¿Aceptar consolidada?" : "¿Aceptar requisición?";
    Swal.fire({
      title: titulo,
      text: "Se enviará a proceso de pago.",
      icon: "question",
      showCancelButton: true,
      confirmButtonColor: "#fe6291",
      cancelButtonColor: "var(--slate-500)",
      confirmButtonText: "Sí, aceptar",
      cancelButtonText: "Cancelar",
    }).then(function (result) {
      if (!result.isConfirmed) return;

      var ajaxOpts = esConsolidada
        ? {
            url: urlEnviarFinancierosDocsConsolidada,
            type: "POST",
            data: { idConsolidada: window._idConsolidadaExpediente }
          }
        : {
            url: urlAceptarExpediente,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({ IdRequisicion: expedienteActual })
          };

      $.ajax(ajaxOpts).done(function (resp) {
        if (!resp.success) {
          Swal.fire({ icon: "error", title: resp.message || "Error al procesar." });
          return;
        }
        bootstrap.Modal.getInstance(
          document.getElementById("modalExpediente"),
        ).hide();
        Swal.fire({
          icon: "success",
          title: "Enviado a proceso de pago",
          confirmButtonText: "Aceptar",
        }).then(function () {
          location.reload();
        });
      }).fail(function () {
        Swal.fire({
          icon: "error",
          title: "Error al procesar la solicitud.",
        });
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
          "Sin cotizaciones registradas.</p>";
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
            (Object.keys(porPartida).indexOf(key) > 0
              ? "margin-top:12px;"
              : "");
          header.innerHTML =
            '<i class="fa-solid fa-tag" style="margin-right:5px;font-size:10px;"></i>' +
            grupo.label;
          lista.appendChild(header);

          // Filas de proveedores de esa partida
          grupo.items.forEach(function (c, i) {
            var importe = parseFloat(c.importe || 0).toLocaleString("es-MX", {
              style: "currency",
              currency: "MXN",
            });
            var fila = document.createElement("div");
            fila.style.cssText =
              "display:flex;align-items:center;gap:12px;padding:8px 14px;" +
              "border-radius:8px;border:1px solid var(--color-border-tertiary);" +
              "background:var(--color-background-secondary);margin-bottom:4px;";
            fila.innerHTML =
              '<span style="font-size:12px;color:var(--color-text-secondary);' +
              'min-width:20px;text-align:center;">#' +
              (i + 1) +
              "</span>" +
              '<span style="flex:1;font-size:13px;font-weight:500;">' +
              (c.nombreProveedor || "Proveedor #" + c.idProveedor) +
              "</span>" +
              '<span style="font-size:13px;color:var(--color-text-success);font-weight:500;">' +
              importe +
              "</span>";
            lista.appendChild(fila);
          });
        });
      }

      new bootstrap.Modal(
        document.getElementById("modalVerCotizaciones"),
      ).show();
    }).fail(function () {
      Swal.fire({
        icon: "error",
        title: "No se pudieron cargar las cotizaciones.",
      });
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
        backdrop: false, // sin backdrop extra para no apilar dos oscurecimientos
        keyboard: false,
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
  var urlObtenerProgreso = container
    ? container.getAttribute("data-url-obtener-progreso")
    : "";
  var urlHistorialPdf = container
    ? container.getAttribute("data-url-historial-pdf")
    : "";

  function configurarDescargaHistorial(idRequi) {
    var btn = document.getElementById("btnDescargarHistorialPdf");
    if (!btn) return;

    if (!urlHistorialPdf || !idRequi) {
      btn.setAttribute("href", "#");
      btn.classList.add("disabled");
      btn.setAttribute("aria-disabled", "true");
      return;
    }

    var separador = urlHistorialPdf.indexOf("?") >= 0 ? "&" : "?";
    btn.setAttribute(
      "href",
      urlHistorialPdf + separador + "id=" + encodeURIComponent(idRequi)
    );
    btn.classList.remove("disabled");
    btn.removeAttribute("aria-disabled");
  }

  function renderHistorialSteps(steps) {
    var done = 0,
      active = 0,
      cancelled = 0,
      completed = 0;
    steps.forEach(function (s) {
      if (s.state === "done") done++;
      else if (s.state === "active") active++;
      else if (s.state === "cancelled") cancelled++;
      else if (s.state === "completed") completed++;
    });

    var summaryEl = document.getElementById("historialSummary");
    if (cancelled > 0) {
      summaryEl.innerHTML =
        '<div class="summary-item"><div class="summary-num" style="color:#15803d">' +
        done +
        '</div><div class="summary-label">Registrados</div></div>' +
        '<div class="summary-item"><div class="summary-num" style="color:#b91c1c">1</div><div class="summary-label">Cancelada</div></div>' +
        '<div class="summary-item"><div class="summary-num" style="color:#64748b">' +
        (done + cancelled) +
        '</div><div class="summary-label">Total</div></div>';
    } else if (completed > 0) {
      summaryEl.innerHTML =
        '<div class="summary-item"><div class="summary-num" style="color:#15803d">' +
        done +
        '</div><div class="summary-label">Registrados</div></div>' +
        '<div class="summary-item"><div class="summary-num" style="color:#065f46">1</div><div class="summary-label">Finalizada</div></div>' +
        '<div class="summary-item"><div class="summary-num" style="color:#64748b">' +
        (done + completed) +
        '</div><div class="summary-label">Total</div></div>';
    } else {
      summaryEl.innerHTML =
        '<div class="summary-item"><div class="summary-num" style="color:#15803d">' +
        done +
        '</div><div class="summary-label">Completados</div></div>' +
        '<div class="summary-item"><div class="summary-num" style="color:#b45309">' +
        active +
        '</div><div class="summary-label">En proceso</div></div>' +
        '<div class="summary-item"><div class="summary-num" style="color:#64748b">' +
        (done + active) +
        '</div><div class="summary-label">Total</div></div>';
    }

    var mtl = document.getElementById("historialTl");
    mtl.innerHTML = "";

    steps.forEach(function (s) {
      var bCls, bTxt;
      switch (s.state) {
        case "done":
          bCls = "mbadge-done";
          bTxt = "Completado";
          break;
        case "active":
          bCls = "mbadge-active";
          bTxt = "En curso";
          break;
        case "completed":
          bCls = "mbadge-completed";
          bTxt = "Finalizado";
          break;
        case "cancelled":
          bCls = "mbadge-cancelled";
          bTxt = "Cancelada";
          break;
        default:
          bCls = "mbadge-pending";
          bTxt = "Pendiente";
          break;
      }
      var tStr = s.time !== "—" ? " · " + s.time : "";

      var item = document.createElement("div");
      item.className = "mtl-item " + s.state;
      item.innerHTML =
        '<div class="mtl-dot-col"><div class="mtl-dot"></div></div>' +
        '<div class="mtl-content">' +
        '<div class="mtl-dept">' +
        s.dept +
        "</div>" +
        '<div class="mtl-meta">' +
        '<span class="mtl-badge ' +
        bCls +
        '">' +
        bTxt +
        "</span>" +
        '<span class="mtl-time">' +
        s.date +
        tStr +
        "</span>" +
        "</div>" +
        (s.comment
          ? '<div class="mtl-detail">' +
            '<div class="mtl-dr"><span class="dr-lbl">Responsable</span>' +
            s.by +
            "</div>" +
            '<div class="mtl-dr"><span class="dr-lbl">Acción</span>' +
            s.action +
            "</div>" +
            '<div class="mtl-dr"><span class="dr-lbl">Nota</span>' +
            s.comment +
            "</div>" +
            "</div>"
          : "") +
        "</div>";
      mtl.appendChild(item);
    });
  }

  window.verHistorialTimeline = function (idRequi, numRequi) {
    document.getElementById("historialSubtitle").textContent = numRequi;
    configurarDescargaHistorial(idRequi);
    document.getElementById("historialSummary").innerHTML =
      '<div style="text-align:center;color:#888;padding:1rem;"><i class="fa-solid fa-spinner fa-spin"></i> Cargando historial…</div>';
    document.getElementById("historialTl").innerHTML = "";

    var modal = new bootstrap.Modal(document.getElementById("modalHistorial"));
    modal.show();

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
          comment: s.comment || s.Comment || "",
        };
      });
      renderHistorialSteps(steps);
    }).fail(function (xhr) {
      var detalle =
        (xhr.responseJSON && xhr.responseJSON.message) ||
        (xhr.status
          ? "Error " + xhr.status + (xhr.statusText ? ": " + xhr.statusText : "")
          : "Error de conexion");
      document.getElementById("historialSummary").innerHTML =
        '<div style="color:#b91c1c;text-align:center;padding:1rem;">' +
        '<i class="fa-solid fa-triangle-exclamation"></i> ' +
        detalle +
        "</div>";
    });
  };

  // Resetear modo consolidada, restaurar thead y botón pedido al cerrar modal
  var modalExp = document.getElementById("modalExpediente");
  if (modalExp) {
    modalExp.addEventListener("hidden.bs.modal", function () {
      window._modoConsolidada = false;
      window._idConsolidadaExpediente = null;
      var theadTr = document.querySelector("#tablaExpedienteDetalle thead tr");
      if (theadTr) {
        theadTr.innerHTML = '<th>N\u00ba Partida</th><th>Cantidad</th><th>Unidad Medida</th><th>Descripci\u00f3n</th><th>Descripci\u00f3n Detallada</th>';
      }
      var btn = document.querySelector("#pedidoModoEdicion .btn");
      if (btn && btn.getAttribute("data-consolidada-override")) {
        btn.onclick = null;
        btn.removeAttribute("data-consolidada-override");
      }
    });
  }
})();


