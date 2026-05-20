(function () {
    var container = document.querySelector(".tabla-requi-page");
    var obtenerDetallesUrl = container ? container.getAttribute("data-url-obtener-detalles") : "";
    var urlUsuariosFinancieros = container ? container.getAttribute("data-url-usuarios-financieros") : "";
    var urlAsignar = container ? container.getAttribute("data-url-asignar") : "";
    var urlRechazar = container ? container.getAttribute("data-url-rechazar") : "";
    var urlObtenerProgreso = container ? container.getAttribute("data-url-obtener-progreso") : "";
    var urlHistorialApi = container ? container.getAttribute("data-url-historial-api") : "";
    var urlObtenerCotizaciones = container
        ? container.getAttribute("data-url-obtener-cotizaciones") : "";
    var expedienteFinActual = null;
    var _modoConsolidadaFinancieros = false;
    var urlFinalizarExpediente = container
        ? container.getAttribute("data-url-finalizar-expediente")
        : "";
    var urlDescargarTablaApi = container
        ? container.getAttribute("data-url-descargar-tabla-api")
        : "";
    var urlEditarTablaApi = container
        ? container.getAttribute("data-url-editar-tabla-api")
        : "";

    // Variables al inicio del módulo
    var urlObtenerDocsProveedor = container
        ? container.getAttribute("data-url-obtener-docs-proveedor") : "";
    var urlRebotarDocumentos = container
        ? container.getAttribute("data-url-rebotar-documentos") : "";
    var urlObtenerArchivos = container
        ? container.getAttribute("data-url-obtener-archivos") : "";
    var urlObtenerTodosArchivos = container
        ? container.getAttribute("data-url-obtener-todos-archivos") : "";
    var urlDescargarTodosArchivosZip = container
        ? container.getAttribute("data-url-descargar-todos-archivos-zip") : "";
    var urlConsolidadas = container
        ? container.getAttribute("data-url-consolidadas") : "";
    var urlAsignarConsolidada = container
        ? container.getAttribute("data-url-asignar-consolidada") : "";
    var urlDetalleConsolidada = container
        ? container.getAttribute("data-url-detalle-consolidada") : "";
    var urlEditarTablaApiConsolidada = container
        ? container.getAttribute("data-url-editar-tabla-api-consolidada") : "";
    var urlDescargarTablaApiConsolidada = container
        ? container.getAttribute("data-url-descargar-tabla-api-consolidada") : "";
    var urlAtenderConsolidada = container
        ? container.getAttribute("data-url-atender-consolidada") : "";
    var esRol8 = container ? container.getAttribute("data-es-rol8") === "true" : false;
    var esRol9 = container ? container.getAttribute("data-es-rol9") === "true" : false;
    var urlProcesoPago = container ? container.getAttribute("data-url-proceso-pago") : "";
    var urlExpedienteConsolidada = container ? container.getAttribute("data-url-expediente-consolidada") : "";
    var urlConsolidadasAutorizadas = container
        ? container.getAttribute("data-url-consolidadas-autorizadas") : "";
    var urlConsolidadasDocumentos = container
        ? container.getAttribute("data-url-consolidadas-documentos") : "";
    var urlObtenerArchivosConsolidada = container
        ? container.getAttribute("data-url-obtener-archivos-consolidada") : "";
    var urlDescargarArchivosConsolidadaZip = container
        ? container.getAttribute("data-url-descargar-archivos-consolidada-zip") : "";

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
    function obtenerBadgeEstatusHtml(idEstatus, nombreEstatus) {
        var nombreSafe = (nombreEstatus || "Desconocido").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
        var colorClass = "badge-estado-info";
        var iconClass = "fa-solid fa-spinner fa-spin-pulse";
        switch (idEstatus) {
            case 7: case 12: case 17:
                colorClass = "badge-estado-success";
                iconClass = "fa-solid fa-check-double";
                break;
            case 4: case 10: case 15: case 16:
                colorClass = "badge-estado-purple";
                iconClass = "fa-solid fa-user-check";
                break;
            case 5: case 6:
                colorClass = "badge-estado-danger";
                iconClass = "fa-solid fa-ban";
                break;
            case 3: case 18:
                colorClass = "badge-estado-warning";
                iconClass = "fa-solid fa-triangle-exclamation";
                break;
            case 1: case 2: case 9: case 11: case 13: case 14:
            default:
                colorClass = "badge-estado-info";
                iconClass = idEstatus === 1 ? "fa-solid fa-file-signature" : "fa-solid fa-spinner fa-spin-pulse";
                break;
        }
        return '<div class="badge-estado-premium ' + colorClass + '" title="' + nombreSafe + '">' +
               '<i class="' + iconClass + '"></i><span class="badge-text">' + nombreSafe + '</span></div>';
    }

    const contenedor = document.querySelector(".tabla-requi-page");
    const atenderUrl = contenedor ? contenedor.dataset.urlAtender : "";

    var _idRequiAsignar = null;
    var _idConsolidadaAsignar = null;
    var fechaSeleccionada = "";
    var requisicionActual = null;
    var consolidadaActual = null;

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
            aplicarFiltrosConsolidadas();
        },
    });

    window.limpiarFecha = function () {
        fpInstance.clear();
        fechaSeleccionada = "";
        var btnLimpiar = document.getElementById("btnLimpiarFecha");
        if (btnLimpiar) btnLimpiar.style.display = "none";
        filtrarTabla();
        aplicarFiltrosConsolidadas();
    };

    const TAMANO_PAGINA_REQUISICIONES = 7;
    let paginaRequisicionActual = 1;
    let paginacionRequisicionesContainer = null;

    var modoTabs =
        container &&
        container.querySelectorAll(".almacen-tabs-btn").length > 0 &&
        container.querySelectorAll(".almacen-tab-panel").length > 0;
    var paginaPorTab = { principal: 1, autorizadas: 1, rechazadas: 1, procesopago: 1 };

    function getActiveTableContext() {
        if (!modoTabs || !container) {
            var tabla = document.querySelector(".tabla-requisiciones:not(#tablaModalDetalle)");
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
                        panel.id === "tab-procesopago" ? "procesopago" : "principal";
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

        // Skip for dynamic procesopago tab (handled by cargarProcesoPago)
        if (ctx.tabKey === "procesopago") return;

        var textoEstado = ((document.getElementById("filtroEstado") && document.getElementById("filtroEstado").value) || "").toLowerCase().trim();
        var textoNumReq = ((document.getElementById("filtroNumReq") && document.getElementById("filtroNumReq").value) || "").toLowerCase().trim();
        var textoDepto = ((document.getElementById("filtroDepartamento") && document.getElementById("filtroDepartamento").value) || "").toLowerCase().trim();

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
            var estado = (celdas[6] && celdas[6].textContent.toLowerCase()) || "";

            var pasaNumReq = !textoNumReq || folio.indexOf(textoNumReq) !== -1;
            var pasaFecha = !fechaSeleccionada || fecha === fechaSeleccionada;
            var pasaDepto = !textoDepto || depto.indexOf(textoDepto) !== -1;
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
        const totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_REQUISICIONES));
        if (paginaRequisicionActual > totalPaginas) paginaRequisicionActual = totalPaginas;
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
        var info = "Mostrando " + (inicio + 1) + "-" + resFinal + " de " + total + " requisiciones";
        var html = '<div class="almacen-paginacion-info">' + info + "</div>";
        html += '<div class="almacen-paginacion-btns">';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="prev" ' + (paginaRequisicionActual <= 1 ? "disabled" : "") + ">Anterior</button>";
        html += '<span class="almacen-paginacion-nums">';

        var PRIMEROS = 3, ULTIMOS = 3;
        var actual = paginaRequisicionActual;
        var set = {};
        for (var i = 1; i <= Math.min(PRIMEROS, totalPaginas); i++) set[i] = true;
        if (actual > 0 && actual <= totalPaginas) {
            set[actual] = true;
            if (actual - 1 >= 1) set[actual - 1] = true;
            if (actual + 1 <= totalPaginas) set[actual + 1] = true;
        }
        for (var j = Math.max(1, totalPaginas - ULTIMOS + 1); j <= totalPaginas; j++) set[j] = true;

        var nums = Object.keys(set).map(Number).sort(function (a, b) { return a - b; });
        var prev = 0;
        for (var n = 0; n < nums.length; n++) {
            var p = nums[n];
            if (prev !== 0 && p > prev + 1) html += '<span class="almacen-paginacion-ellipsis">…</span>';
            html += '<button type="button" class="almacen-paginacion-btn almacen-paginacion-num ' + (p === paginaRequisicionActual ? "activo" : "") + '" data-pagina="' + p + '">' + p + "</button>";
            prev = p;
        }
        html += "</span>";
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="next" ' + (paginaRequisicionActual >= totalPaginas ? "disabled" : "") + ">Siguiente</button>";
        html += "</div>";

        paginacionRequisicionesContainer.innerHTML = html;

        var botones = paginacionRequisicionesContainer.querySelectorAll(".almacen-paginacion-btn");
        var ctxPag = getActiveTableContext();
        for (var k = 0; k < botones.length; k++) {
            botones[k].addEventListener("click", function () {
                if (this.disabled) return;
                var pg = this.getAttribute("data-pagina");
                if (pg === "prev") {
                    paginaRequisicionActual = Math.max(1, paginaRequisicionActual - 1);
                } else if (pg === "next") {
                    paginaRequisicionActual = Math.min(totalPaginas, paginaRequisicionActual + 1);
                } else {
                    paginaRequisicionActual = parseInt(pg, 10);
                }
                if (ctxPag) paginaPorTab[ctxPag.tabKey] = paginaRequisicionActual;
                aplicarPaginacionRequisiciones();
            });
        }
    }

    // ── Modal asignar — solo el select ──────────────────────────────────────
    window.abrirModalAsignar = function (id, tipo) {
        tipo = tipo || "requi";
        _idRequiAsignar = tipo === "requi" ? id : null;
        _idConsolidadaAsignar = tipo === "consolidada" ? id : null;

        var modalTitle = document.querySelector("#modalAsignar .modal-titulo-premium");
        var modalSubtitle = document.querySelector("#modalAsignar .modal-subtitulo-premium");
        var dialog = document.querySelector("#modalAsignar .modal-dialog");
        if (tipo === "consolidada") {
            if (modalTitle) modalTitle.textContent = "Asignar consolidada";
            if (modalSubtitle) modalSubtitle.textContent = "Selecciona el analista financiero para esta consolidada";
            if (dialog) dialog.style.setProperty("max-width", "700px", "important");
        } else {
            if (modalTitle) modalTitle.textContent = "Asignar requisición";
            if (modalSubtitle) modalSubtitle.textContent = "Selecciona el analista";
            if (dialog) dialog.style.setProperty("max-width", "", "important");
        }

        var $select = $("#selectUsuarioAsignar");
        if ($select.data("select2")) $select.select2("destroy");
        $select.html('<option value="">-- Seleccionar responsable --</option>');

        $.get(urlUsuariosFinancieros, function (data) {
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

        new bootstrap.Modal(document.getElementById("modalAsignar")).show();
    };

    window.confirmarAsignacion = function () {
        var idUsuario = $("#selectUsuarioAsignar").val();
        if (!idUsuario) {
            Swal.fire({ icon: "warning", title: "Selecciona un responsable", confirmButtonText: "Ok" });
            return;
        }

        var postUrl, postData;
        if (_idConsolidadaAsignar) {
            postUrl = urlAsignarConsolidada;
            postData = { idConsolidada: _idConsolidadaAsignar, idUsuario: idUsuario };
        } else {
            postUrl = urlAsignar;
            postData = { idRequi: _idRequiAsignar, idUsuario: idUsuario };
        }

        $.post(postUrl, postData, function (res) {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById("modalAsignar")).hide();
                Swal.fire({
                    icon: "success",
                    title: "Asignado correctamente",
                    confirmButtonColor: "#fe6291",
                    confirmButtonText: "Aceptar",
                    timer: 3500,
                    timerProgressBar: true,
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({ icon: "error", title: "No se pudo asignar", text: res.mensaje || "" });
            }
        });
    };

    window.descargarTablaApi = function () {
        if (!requisicionActual) {
            Swal.fire({
                icon: "warning",
                title: "Sin requisición",
                text: "No se pudo identificar la requisición actual.",
                confirmButtonText: "Ok",
                confirmButtonColor: "#fe6291"
            });
            return;
        }

        var id = requisicionActual;
        $.get("/Financieros/ObtenerNumeroApi", { idRequisicion: id }, function (res) {
            if (res && res.numApi) {
                Swal.fire({
                    icon: "success",
                    title: "Tabla API generada",
                    html: "N\u00famero de API asignado:<br><strong style=\"font-size:1.4rem;color:#166534;letter-spacing:.05em;\">"
                        + res.numApi + "</strong>",
                    confirmButtonText: "Aceptar",
                    confirmButtonColor: "#fe6291"
                });
            }
        });

        var url = (urlDescargarTablaApi || "").replace(/\/$/, "")
            + "?idRequisicion=" + id;
        window.open(url, "_blank");
    };

    window.editarTablaApi = function () {
        if (!requisicionActual) {
            Swal.fire({
                icon: "warning",
                title: "Sin requisición",
                text: "No se pudo identificar la requisición actual.",
                confirmButtonText: "Ok",
                confirmButtonColor: "#fe6291"
            });
            return;
        }

        var url = (urlEditarTablaApi || "").replace(/\/$/, "")
            + "?idRequisicion=" + requisicionActual;
        window.open(url, "_blank");
    };

    window.descargarTablaApiConsolidada = function () {
        if (!consolidadaActual) {
            Swal.fire({
                icon: "warning",
                title: "Sin consolidada",
                text: "No se pudo identificar la consolidada actual.",
                confirmButtonText: "Ok",
                confirmButtonColor: "#fe6291"
            });
            return;
        }

        var id = consolidadaActual;
        $.get("/Financieros/ObtenerNumeroApiConsolidada", { idConsolidada: id }, function (res) {
            if (res && res.numApi) {
                Swal.fire({
                    icon: "success",
                    title: "Tabla API generada",
                    html: "N\u00famero de API asignado:<br><strong style=\"font-size:1.4rem;color:#166534;letter-spacing:.05em;\">"
                        + res.numApi + "</strong>",
                    confirmButtonText: "Aceptar",
                    confirmButtonColor: "#fe6291"
                });
            }
        });

        var url = (urlDescargarTablaApiConsolidada || "").replace(/\/$/, "")
            + "?idConsolidada=" + id;
        window.open(url, "_blank");
    };

    window.editarTablaApiConsolidada = function () {
        if (!consolidadaActual) {
            Swal.fire({
                icon: "warning",
                title: "Sin consolidada",
                text: "No se pudo identificar la consolidada actual.",
                confirmButtonText: "Ok",
                confirmButtonColor: "#fe6291"
            });
            return;
        }

        var url = (urlEditarTablaApiConsolidada || "").replace(/\/$/, "")
            + "?idConsolidada=" + consolidadaActual;
        window.open(url, "_blank");
    };

    // ── Ver detalle / atender ───────────────────────────────────────────────
    window.atenderRequisicion = function (idMaestro) {
        requisicionActual = idMaestro;
        consolidadaActual = null;
        verDetalle(idMaestro, "atender");
    };

    window.verDetalle = function (idMaestro, modo) {
        modo = modo || "ver";

        // Hide "Req." column (only visible in consolidada atender mode)
        document.querySelectorAll(".col-requi").forEach(function (th) { th.style.display = "none"; });

        // Restore articles table visibility (may have been hidden by consolidada detail)
        var detalleTableContainer = document.querySelector("#tablaDetalle")?.closest(".table-responsive-container");
        if (detalleTableContainer) detalleTableContainer.style.display = "";

        // Limpiar al abrir
        var archivosReadonly = document.getElementById("contenedorArchivosReadonly");
        if (archivosReadonly) archivosReadonly.innerHTML = "";
        var galeriaFotosDetalle = document.getElementById('galeriaFotosDetalle');
        var seccionFotosDetalle = document.getElementById('seccionFotosDetalle');
        if (galeriaFotosDetalle) galeriaFotosDetalle.innerHTML = '';
        if (seccionFotosDetalle) seccionFotosDetalle.style.display = 'none';
        // Donde limpias los inputs al abrir el modal
        var inputSiaf = document.getElementById("inputSiaf");
        var inputTablaApi = document.getElementById("inputTablaApi");
        if (inputSiaf) inputSiaf.value = "";
        if (inputTablaApi) inputTablaApi.value = "";

        // Restore modal title (in case it was changed by consolidada detail)
        var modalTitle = document.querySelector("#modalDetalle .modal-titulo-premium");
        if (modalTitle) modalTitle.textContent = "Artículos de la Requisición";
        var modalSubtitle = document.querySelector("#modalDetalle .modal-subtitulo-premium");
        if (modalSubtitle) modalSubtitle.textContent = "Detalle de partidas solicitadas";

        // Restore "Editar Tabla API" button visibility (hidden in consolidada mode)
        var btnEditarTablaApi = document.querySelector(".seccionAtender .boton-gris[onclick*='editarTablaApi']");
        if (btnEditarTablaApi) btnEditarTablaApi.style.display = "";

        var isAtender = modo === "atender";
        var isReadonly = modo === "readonly";

        // Mostrar sección selects en ambos casos
        document.querySelectorAll(".seccionAtender").forEach(function (sec) {
            sec.style.display = (isAtender || isReadonly) ? "block" : "none";
        });

        // Botones solo visibles en modo atender
        var botonesAtender = document.getElementById("botonesAtender");
        if (botonesAtender) botonesAtender.style.display = isAtender ? "flex" : "none";

        if (!obtenerDetallesUrl) return;

        $.get(obtenerDetallesUrl, { idMaestro: idMaestro, soloCompra: true }, function (data) {
            var articulos = data.articulos || [];

            // Tabla artículos
            var contenido = "";
            if (articulos.length === 0) {
                contenido = '<tr><td colspan="5" class="text-center">Sin artículos</td></tr>';
            } else {
                articulos.forEach(function (item) {
                    var textoCompleto = item.descripcionDetallada || "";
                    var textoCorto = textoCompleto.length > 28
                        ? textoCompleto.substring(0, 28) + "…"
                        : textoCompleto || "Sin descripción...";
                    var tieneTexto = textoCompleto ? "tiene-texto" : "";
                    var fullEscapado = (textoCompleto || "").replace(/"/g, "&quot;");
                    contenido +=
                        "<tr>" +
                        "<td>" + (item.numPartida || "") + "</td>" +
                        "<td>" + (item.cantidad || "") + "</td>" +
                        "<td>" + (item.unidadMedida || "") + "</td>" +
                        "<td>" + (item.descripcion || "") + "</td>" +
                        '<td><div class="desc-preview-modal" data-full="' + fullEscapado + '" onclick="verDescDetalleModal(this)">' +
                        '<span class="desc-texto-preview ' + tieneTexto + '">' + textoCorto + "</span>" +
                        '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                        "</tr>";
                });
            }
            $("#tablaDetalle").html(contenido);

            // Fotos diseño
            var seccionFotos = document.getElementById('seccionFotosDetalle');
            var galeriaFotos = document.getElementById('galeriaFotosDetalle');
            if (seccionFotos && galeriaFotos) {
                if (data.tipoServicio === 'Servicio Impresion' && data.fotos && data.fotos.length > 0) {
                    galeriaFotos.innerHTML = '';
                    data.fotos.forEach(function (ruta) {
                        var img = document.createElement('img');
                        img.src = ruta;
                        img.classList.add('modal-galeria-foto-thumb');
                        img.alt = '';
                        img.addEventListener('click', function () { window.abrirVisorImagenTabla(ruta); });
                        galeriaFotos.appendChild(img);
                    });
                    seccionFotos.style.display = 'block';
                } else {
                    seccionFotos.style.display = 'none';
                }
            }

            // Subtítulo
            var subtitulo = document.querySelector("#modalDetalle .modal-subtitulo-premium");
            if (subtitulo)
                subtitulo.textContent = "Detalle de partidas · Total: " + articulos.length + " partidas";

            // Inicializar SelectRosaBuscable (PP, FF, TipoPrograma) + Select2 (municipio)
            if (isAtender || isReadonly) {
                ["actividadSeleccionada", "ffSelect", "tipoProgramaSelect"].forEach(function (id) {
                    var el = document.getElementById(id);
                    if (el && window.SelectRosaBuscable) {
                        window.SelectRosaBuscable.destruir(el);
                    }
                });
                var $mun = $("#municipio");
                if ($mun.data("select2")) $mun.select2("destroy");

                ["actividadSeleccionada", "ffSelect", "tipoProgramaSelect"].forEach(function (id) {
                    var el = document.getElementById(id);
                    if (el && window.SelectRosaBuscable) {
                        window.SelectRosaBuscable.inicializar(el, {
                            placeholder: id === "actividadSeleccionada" ? "Buscar actividad..." :
                                         id === "ffSelect" ? "Buscar fuente..." :
                                         "Buscar tipo de programa...",
                            defaultText: false
                        });
                    }
                });
                $mun.select2({
                    dropdownParent: $("#modalDetalle"),
                    width: "100%",
                    language: "es"
                });

                // Precargar valores desde la data (PP, FF, Programa, Municipio)
                if (data.idPp) {
                    $("#actividadSeleccionada").val(data.idPp).trigger("change");
                }
                if (data.ff) {
                    $("#ffSelect").val(data.ff).trigger("change");
                }
                if (data.tipoPrograma) {
                    $("#tipoProgramaSelect option").filter(function () { return $(this).text().trim() === data.tipoPrograma; }).prop("selected", true);
                    $("#tipoProgramaSelect").trigger("change");
                }

                // --- CAMBIO AQUÍ: Deshabilitar campos y cargar archivos para AMBOS modos ---
                $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio")
                    .prop("disabled", true);
                ["actividadSeleccionada", "ffSelect", "tipoProgramaSelect"].forEach(function (id) {
                    var el = document.getElementById(id);
                    if (el && window.SelectRosaBuscable) {
                        window.SelectRosaBuscable.actualizar(el);
                    }
                });

                (function () {
                    var c = document.getElementById("contenedorArchivosReadonly");
                    if (!c) return;
                    c.innerHTML = "";

                    // Botón cotizaciones
                    var btnCot = document.createElement("div");
                    btnCot.style.cssText = "margin-bottom:12px;";
                    btnCot.innerHTML =
                        '<button type="button" class="btn boton-rosa" ' +
                        'onclick="verCotizaciones(' + idMaestro + ')">' +
                        '<i class="fa-solid fa-file-invoice-dollar"></i> Ver cotizaciones' +
                        '</button>';
                    c.appendChild(btnCot);

                    // Cuadro comparativo (se mantiene)
                    if (window.ModalAdjuntos && (data.cuadroComparativo || []).length > 0) {
                        var divCuadro = document.createElement("div");
                        divCuadro.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                        window.ModalAdjuntos.enlazarEventosContenedor(divCuadro);
                        c.appendChild(divCuadro);
                    }

                    // Anexos
                    if (window.ModalAdjuntos && (data.anexos || []).length > 0) {
                        var divAnexos = document.createElement("div");
                        divAnexos.id = "seccionAnexosDetalle";
                        divAnexos.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
                        window.ModalAdjuntos.enlazarEventosContenedor(divAnexos);
                        c.appendChild(divAnexos);
                    }
                })();

                // Diferenciar solo la parte de observaciones y botones
                if (isReadonly) {
                    $("#txtObservaciones")
                        .prop("readonly", true)
                        .val(data.observaciones || "");
                } else {
                    // En modo "Atender" las observaciones quedan habilitadas para escribir la respuesta
                    $("#txtObservaciones")
                        .prop("readonly", false)
                        .val("");
                }
            }

            new bootstrap.Modal(document.getElementById("modalDetalle")).show();
        });
    };

    window.verCotizaciones = function (idRequi) {
        $.get(urlObtenerCotizaciones, { idRequisicion: idRequi }, function (data) {
            var lista = document.getElementById("listaCotizacionesModal");
            lista.innerHTML = "";

            function esc(valor) {
                return String(valor || "")
                    .replace(/&/g, "&amp;")
                    .replace(/</g, "&lt;")
                    .replace(/>/g, "&gt;")
                    .replace(/"/g, "&quot;")
                    .replace(/'/g, "&#39;");
            }

            if (!data || !data.length) {
                lista.innerHTML =
                    '<p style="color:var(--color-text-secondary);font-style:italic;font-size:13px;">' +
                    'Sin cotizaciones registradas.</p>';
            } else {
                // Agrupar por proveedor
                var porProveedor = {};
                data.forEach(function (c) {
                    var key = c.idProveedor || 0;
                    if (!porProveedor[key]) {
                        porProveedor[key] = {
                            nombreProveedor: c.nombreProveedor || ("Proveedor #" + key),
                            items: []
                        };
                    }
                    porProveedor[key].items.push(c);
                });

                Object.keys(porProveedor).forEach(function (key) {
                    var grupo = porProveedor[key];
                    var bloque = document.createElement("div");
                    bloque.style.cssText =
                        "padding:18px 20px;background:white;border:1px solid var(--slate-200);" +
                        "border-radius:var(--radius-md);box-shadow:var(--shadow-sm);";

                    var titulo = document.createElement("div");
                    titulo.style.cssText =
                        "display:flex;align-items:center;gap:8px;margin-bottom:14px;" +
                        "padding-bottom:10px;border-bottom:1px solid var(--slate-200);";
                    titulo.innerHTML =
                        '<i class="fa-solid fa-building-user" style="color:var(--rosa-400);font-size:14px;"></i>' +
                        '<span style="font-family:var(--font-display);font-size:12px;font-weight:700;' +
                        'letter-spacing:.08em;text-transform:uppercase;color:var(--slate-500);">Proveedor</span>' +
                        '<span style="font-family:var(--font-body);font-size:15px;font-weight:600;color:var(--slate-800);">' +
                        esc(grupo.nombreProveedor) +
                        '</span>';
                    bloque.appendChild(titulo);

                    var tablaWrap = document.createElement("div");
                    tablaWrap.className = "table-responsive-container";

                    var filas = "";
                    grupo.items.forEach(function (c, i) {
                        var importe = parseFloat(c.importe || 0).toLocaleString("es-MX", {
                            style: "currency", currency: "MXN"
                        });
                        var descripcionDetallada = c.descripcionDetallada || c.nombrePartida || "";
                        var descripcionDetalladaCorta = descripcionDetallada.length > 28
                            ? descripcionDetallada.substring(0, 28) + "…"
                            : (descripcionDetallada || "Sin descripción...");
                        var tieneTexto = descripcionDetallada ? " tiene-texto" : "";

                        filas +=
                            "<tr>" +
                            "<td>" + esc(c.numPartida || (i + 1)) + "</td>" +
                            "<td><strong>" + esc(c.descripcion || "") + "</strong></td>" +
                            '<td><div class="desc-preview-modal" style="width:430px; min-width:430px; max-width:430px;" data-full="' +
                            esc(descripcionDetallada) +
                            '" onclick="verDescDetalleModal(this)">' +
                            '<span class="desc-texto-preview' + tieneTexto + '">' +
                            esc(descripcionDetalladaCorta) +
                            '</span><i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                            '<td style="text-align:right; color:var(--color-text-success); font-weight:600;">' + importe + "</td>" +
                            '<td style="text-align:center;">' + (c.iva ? "Sí" : "No") + "</td>" +
                            "</tr>";
                    });

                    tablaWrap.innerHTML =
                        '<table class="tabla-requisiciones">' +
                        "<thead>" +
                        "<tr>" +
                        '<th style="width:90px;">Partida</th>' +
                        "<th>Descripción</th>" +
                        "<th>Descripción detallada</th>" +
                        '<th style="width:160px; text-align:right;">Precio</th>' +
                        '<th style="width:100px; text-align:center;">IVA</th>' +
                        "</tr>" +
                        "</thead>" +
                        "<tbody>" + filas + "</tbody>" +
                        "</table>";

                    bloque.appendChild(tablaWrap);
                    lista.appendChild(bloque);
                });
            }

            new bootstrap.Modal(document.getElementById("modalVerCotizaciones")).show();
        }).fail(function () {
            Swal.fire({ icon: "error", title: "No se pudieron cargar las cotizaciones." });
        });
    };

    window.verExpedienteFinancieros = function (id, esConsolidada) {
        esConsolidada = esConsolidada || false;
        expedienteFinActual = id;
        _modoConsolidadaFinancieros = esConsolidada;

        // Show/hide "Requi" column in thead
        document.querySelectorAll(".col-requi").forEach(function (th) { th.style.display = esConsolidada ? "" : "none"; });

        // Limpiar
        document.getElementById("expFinObservaciones").value = "";
        document.getElementById("expFinArchivosBase").innerHTML = "";
        document.getElementById("expFinGrupoSiaf").innerHTML = "";
        document.getElementById("expFinGrupoTablaApi").innerHTML = "";
        document.getElementById("expFinGrupoNumeroApi").innerHTML = "";
        document.getElementById("expFinGrupoPedidoCompra").innerHTML = "";
        document.getElementById("expFinArchivosFinancieros").style.display = "none";
        document.getElementById("expFinGaleriaFotos").innerHTML = "";
        document.getElementById("expFinSeccionFotos").style.display = "none";
        document.getElementById("expFinTablaBody").innerHTML = "";
        document.getElementById("expFinSubtitulo").textContent = "Cargando...";

        if (esConsolidada) {
            consolidadaActual = id;
            requisicionActual = null;
            var URL = (urlExpedienteConsolidada || "").replace(/\/?$/, "") + "?idConsolidada=" + id;
            $.get(URL, function (data) {
                var articulos = data.articulos || [];
                document.getElementById("expFinSubtitulo").textContent = "Expediente consolidado · " + articulos.length + " partidas";

                var contenido = !articulos.length
                    ? '<tr><td colspan="6" class="text-center">Sin artículos</td></tr>'
                    : "";
                articulos.forEach(function (item) {
                    var txtCompleto = item.descripcionDetallada || "";
                    var txtCorto = txtCompleto.length > 28 ? txtCompleto.substring(0, 28) + "…" : txtCompleto || "Sin descripción...";
                    var fullEsc = (txtCompleto || "").replace(/"/g, "&quot;");
                    contenido +=
                        "<tr>" +
                        "<td>" + (item.numRequiOrigen || "") + "</td>" +
                        "<td>" + (item.numPartida || "") + "</td>" +
                        "<td>" + (item.cantidad || "") + "</td>" +
                        "<td>" + (item.unidadMedida || "") + "</td>" +
                        "<td>" + (item.descripcion || "") + "</td>" +
                        '<td><div class="desc-preview-modal" data-full="' + fullEsc + '" onclick="verDescDetalleModal(this)">' +
                        '<span class="desc-texto-preview' + (txtCompleto ? " tiene-texto" : "") + '">' + txtCorto + "</span>" +
                        '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                        "</tr>";
                });
                document.getElementById("expFinTablaBody").innerHTML = contenido;

                // Selects
                ["expFinActividad", "expFinFf", "expFinTipoPrograma", "expFinMunicipio"].forEach(function (selId) {
                    var el = $("#" + selId);
                    if (el.data("select2")) el.select2("destroy");
                });
                $("#expFinActividad, #expFinFf, #expFinTipoPrograma, #expFinMunicipio").select2({
                    dropdownParent: $("#modalExpedienteFinancieros"),
                    width: "100%",
                    language: "es"
                }).prop("disabled", true);
                if (data.idPp) $("#expFinActividad").val(data.idPp).trigger("change");
                if (data.ff) $("#expFinFf").val(data.ff).trigger("change");
                if (data.tipoPrograma) {
                    $("#expFinTipoPrograma option").filter(function () { return $(this).text().trim() === data.tipoPrograma; }).prop("selected", true);
                    $("#expFinTipoPrograma").trigger("change");
                }

                // Observaciones
                document.getElementById("expFinObservaciones").value = data.observaciones || "";

                // Documentos financieros
                (function () {
                    var siaf = data.archivosSiaf || [];
                    var tablaApi = data.archivosTablaApi || [];
                    var pedidoCompra = data.archivosPedidoCompra || [];
                    var numApi = data.numeroApi || null;
                    if (!siaf.length && !tablaApi.length && !numApi && !pedidoCompra.length) return;

                    document.getElementById("expFinArchivosFinancieros").style.display = "block";

                    function rgf(elId, titulo, archivos) {
                        var el = document.getElementById(elId);
                        if (!archivos.length) { el.innerHTML = ""; return; }
                        if (window.ModalAdjuntos) {
                            el.innerHTML = window.ModalAdjuntos.renderGrupoHtml(titulo, archivos);
                            window.ModalAdjuntos.enlazarEventosContenedor(el);
                        } else { el.innerHTML = ""; }
                    }
                    rgf("expFinGrupoSiaf", "Documento SIAF", siaf);
                    rgf("expFinGrupoTablaApi", "Tabla de API", tablaApi);
                    if (numApi) {
                        document.getElementById("expFinGrupoNumeroApi").innerHTML =
                            '<label style="font-size:13px;font-weight:600;color:#555;margin-bottom:4px;display:block;">Nº API</label>'
                            + '<span style="font-size:13px;padding:4px 10px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;color:#166534;">'
                            + '<i class="fa-solid fa-hashtag" style="margin-right:4px;"></i>' + numApi + '</span>';
                    }
                    rgf("expFinGrupoPedidoCompra", "Pedido de Compra", pedidoCompra);
                })();

                // Documentos del proveedor
                (function () {
                    var docs = data.documentosProveedor || [];
                    var seccion = document.getElementById("expFinDocumentosProveedor");
                    var checklist = document.getElementById("expFinChecklistDocs");
                    checklist.innerHTML = "";
                    if (!docs.length) { seccion.style.display = "none"; return; }
                    seccion.style.display = "block";
                    var clavesSubidas = docs.map(function (d) { return d.nombreArchivo; });
                    DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
                        var subido = clavesSubidas.indexOf(doc.clave) !== -1;
                        var archivo = docs.find(function (d) { return d.nombreArchivo === doc.clave; });
                        var noAplica = !subido;
                        var fila = document.createElement("div");
                        var borderColor = noAplica ? "var(--color-border-tertiary)" : "#bbf7d0";
                        var bgColor = noAplica ? "var(--color-background-secondary)" : "#f0fdf4";
                        fila.style.cssText = "display:flex; align-items:center; gap:10px; padding:8px 12px;" +
                            "border-radius:8px; border:1px solid " + borderColor + ";" + "background:" + bgColor + ";";
                        var icono = subido
                            ? '<i class="fa-solid fa-circle-check" style="color:#16a34a;font-size:16px;flex-shrink:0;"></i>'
                            : '<i class="fa-solid fa-minus-circle" style="color:#94a3b8;font-size:16px;flex-shrink:0;"></i>';
                        var linkVer = subido && archivo
                            ? '<a href="' + archivo.ruta + '" target="_blank" style="font-size:11px;color:var(--color-text-secondary);margin-left:auto;text-decoration:none;padding:3px 8px;border:1px solid var(--color-border-secondary);border-radius:6px;white-space:nowrap;"><i class="fa-solid fa-eye"></i> Ver</a>'
                            : '<span style="font-size:11px;color:#94a3b8;margin-left:auto;padding:3px 8px;border:1px solid var(--color-border-tertiary);border-radius:6px;white-space:nowrap;font-style:italic;">No aplica</span>';
                        fila.innerHTML = icono + '<span style="font-size:13px;flex:1;">' + doc.label + '</span>' + linkVer;
                        checklist.appendChild(fila);
                    });
                })();

                new bootstrap.Modal(document.getElementById("modalExpedienteFinancieros")).show();
            });
            return;
        }

        $.get(obtenerDetallesUrl, { idMaestro: id, soloCompra: true }, function (data) {
            var articulos = data.articulos || [];
            document.getElementById("expFinSubtitulo").textContent =
                "Expediente completo · " + articulos.length + " partidas";

            // Tabla artículos
            var contenido = !articulos.length
                ? '<tr><td colspan="5" class="text-center">Sin artículos</td></tr>'
                : "";
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
            document.getElementById("expFinTablaBody").innerHTML = contenido;

            // Fotos
            if (data.tipoServicio === "Servicio Impresion" && data.fotos && data.fotos.length) {
                var galeria = document.getElementById("expFinGaleriaFotos");
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
                document.getElementById("expFinSeccionFotos").style.display = "block";
            }

            // Selects
            ["expFinActividad", "expFinFf", "expFinTipoPrograma", "expFinMunicipio"].forEach(function (id) {
                var el = $("#" + id);
                if (el.data("select2")) el.select2("destroy");
            });
            $("#expFinActividad, #expFinFf, #expFinTipoPrograma, #expFinMunicipio").select2({
                dropdownParent: $("#modalExpedienteFinancieros"),
                width: "100%",
                language: "es"
            }).prop("disabled", true);

            if (data.idPp) $("#expFinActividad").val(data.idPp).trigger("change");
            if (data.ff) {
                $("#expFinFf").val(data.ff).trigger("change");
            }
            if (data.tipoPrograma) {
                $("#expFinTipoPrograma option").filter(function () {
                    return $(this).text().trim() === data.tipoPrograma;
                }).prop("selected", true);
                $("#expFinTipoPrograma").trigger("change");
            }

            // Cotizaciones / cuadro
            (function () {
                var c = document.getElementById("expFinArchivosBase");
                c.innerHTML = "";

                // Botón cotizaciones
                var btnCot = document.createElement("div");
                btnCot.style.cssText = "margin-bottom:12px;";
                btnCot.innerHTML =
                    '<button type="button" class="btn boton-rosa" ' +
                    'onclick="verCotizaciones(' + id + ')">' +
                    '<i class="fa-solid fa-file-invoice-dollar"></i> Ver cotizaciones' +
                    '</button>';
                c.appendChild(btnCot);

                // Cuadro comparativo (se mantiene)
                if (window.ModalAdjuntos && (data.cuadroComparativo || []).length > 0) {
                    var divCuadro = document.createElement("div");
                    divCuadro.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                    window.ModalAdjuntos.enlazarEventosContenedor(divCuadro);
                    c.appendChild(divCuadro);
                }

                // Anexos
                if (window.ModalAdjuntos && (data.anexos || []).length > 0) {
                    var divAnexos = document.createElement("div");
                    divAnexos.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
                    window.ModalAdjuntos.enlazarEventosContenedor(divAnexos);
                    c.appendChild(divAnexos);
                }
            })();

            // Observaciones
            document.getElementById("expFinObservaciones").value = data.observaciones || "";

            // Documentos financieros
            (function () {
                var siaf = data.archivosSiaf || [];
                var tablaApi = data.archivosTablaApi || [];
                var pedidoCompra = data.archivosPedidoCompra || [];
                var numApi = data.numeroApi || null;
                if (!siaf.length && !tablaApi.length && !numApi && !pedidoCompra.length) return;

                document.getElementById("expFinArchivosFinancieros").style.display = "block";

                function rgf(elId, titulo, archivos) {
                    var el = document.getElementById(elId);
                    if (!archivos.length) { el.innerHTML = ""; return; }

                    if (window.ModalAdjuntos) {
                        el.innerHTML = window.ModalAdjuntos.renderGrupoHtml(titulo, archivos);
                        window.ModalAdjuntos.enlazarEventosContenedor(el);
                    } else {
                        el.innerHTML = "";
                    }
                }
                rgf("expFinGrupoSiaf", "Documento SIAF", siaf);
                rgf("expFinGrupoTablaApi", "Tabla de API", tablaApi);

                if (numApi) {
                    document.getElementById("expFinGrupoNumeroApi").innerHTML =
                        '<label style="font-size:13px;font-weight:600;color:#555;margin-bottom:4px;display:block;">Nº API</label>'
                        + '<span style="font-size:13px;padding:4px 10px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;color:#166534;">'
                        + '<i class="fa-solid fa-hashtag" style="margin-right:4px;"></i>' + numApi + '</span>';
                }

                // NUEVO: pedido de compra
                rgf("expFinGrupoPedidoCompra", "Pedido de Compra", pedidoCompra);
            })();

            $.get(urlObtenerDocsProveedor, { idRequisicion: id }, function (docs) {
                var seccion = document.getElementById("expFinDocumentosProveedor");
                var checklist = document.getElementById("expFinChecklistDocs");
                checklist.innerHTML = "";

                if (!docs.length) {
                    seccion.style.display = "none";
                    return;
                }

                seccion.style.display = "block";
                var clavesSubidas = docs.map(function (d) { return d.nombreArchivo; });

                DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
                    var subido = clavesSubidas.indexOf(doc.clave) !== -1;
                    var archivo = docs.find(function (d) { return d.nombreArchivo === doc.clave; });

                    // Si está en BD → subido. Si no → el analista lo marcó "No aplica"
                    var noAplica = !subido;

                    var fila = document.createElement("div");

                    var borderColor = noAplica
                        ? "var(--color-border-tertiary)"
                        : "#bbf7d0";
                    var bgColor = noAplica
                        ? "var(--color-background-secondary)"
                        : "#f0fdf4";

                    fila.style.cssText =
                        "display:flex; align-items:center; gap:10px; padding:8px 12px;" +
                        "border-radius:8px; border:1px solid " + borderColor + ";" +
                        "background:" + bgColor + ";";

                    var icono = subido
                        ? '<i class="fa-solid fa-circle-check" style="color:#16a34a;font-size:16px;flex-shrink:0;"></i>'
                        : '<i class="fa-solid fa-minus-circle" style="color:#94a3b8;font-size:16px;flex-shrink:0;"></i>';

                    var linkVer = subido && archivo
                        ? '<a href="' + archivo.ruta + '" target="_blank" ' +
                        'style="font-size:11px;color:var(--color-text-secondary);margin-left:auto;' +
                        'text-decoration:none;padding:3px 8px;border:1px solid var(--color-border-secondary);' +
                        'border-radius:6px;white-space:nowrap;">' +
                        '<i class="fa-solid fa-eye"></i> Ver</a>'
                        : '<span style="font-size:11px;color:#94a3b8;margin-left:auto;' +
                        'padding:3px 8px;border:1px solid var(--color-border-tertiary);' +
                        'border-radius:6px;white-space:nowrap;font-style:italic;">No aplica</span>';

                    fila.innerHTML = icono +
                        '<span style="font-size:13px;flex:1;">' + doc.label + '</span>' +
                        linkVer;

                    checklist.appendChild(fila);
                });
            });

            new bootstrap.Modal(document.getElementById("modalExpedienteFinancieros")).show();
        });
    };

    window.finalizarExpediente = function () {
        var titulo = _modoConsolidadaFinancieros ? "\u00bfFinalizar consolidada?" : "\u00bfFinalizar requisici\u00f3n?";
        Swal.fire({
            title: titulo,
            text: "Se marcar\u00e1 como finalizada y enviada a proceso de pago.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            cancelButtonColor: "var(--slate-500)",
            confirmButtonText: "S\u00ed, finalizar",
            cancelButtonText: "Cancelar"
        }).then(function (result) {
            if (!result.isConfirmed) return;

            var formData = new FormData();
            formData.append(_modoConsolidadaFinancieros ? "IdConsolidada" : "IdRequisicion", expedienteFinActual);

            var inputTransferencia = document.getElementById("inputTransferencia");
            if (inputTransferencia && inputTransferencia.files.length) {
                for (var i = 0; i < inputTransferencia.files.length; i++)
                    formData.append("Transferencia", inputTransferencia.files[i]);
            }

            $.ajax({
                url: urlFinalizarExpediente,
                type: "POST",
                data: formData,
                processData: false,
                contentType: false,
                success: function () {
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalExpedienteFinancieros")
                    ).hide();
                    if (inputTransferencia) inputTransferencia.value = "";
                    Swal.fire({
                        icon: "success",
                        title: "Requisición finalizada",
                        confirmButtonText: "Aceptar"
                    }).then(function () { location.reload(); });
                },
                error: function () {
                    Swal.fire({ icon: "error", title: "Error al finalizar la requisici\u00f3n." });
                }
            });
        });
    };

    // Función para abrir el modal de rebotar
    window.rebotarDocumentos = function () {
        var checklist = document.getElementById("checklistRebotar");
        checklist.innerHTML = "";
        document.getElementById("txtNotaRebotar").value = "";

        DOCUMENTOS_PROVEEDOR.forEach(function (doc) {
            var fila = document.createElement("label");
            fila.style.cssText = "display:flex; align-items:center; gap:10px; padding:8px 12px;" +
                "border-radius:8px; border:1px solid var(--color-border-tertiary);" +
                "background:var(--color-background-secondary); cursor:pointer;";
            fila.innerHTML =
                window.crearCheckAnimado({ value: doc.label }) +
                '<span style="font-size:13px;">' + doc.label + '</span>';
            checklist.appendChild(fila);
        });

        new bootstrap.Modal(document.getElementById("modalRebotar")).show();
    };

    window.confirmarRebotar = function () {
        var checkboxes = document.querySelectorAll("#checklistRebotar input[type=checkbox]:checked");
        var docsObservados = Array.from(checkboxes).map(function (cb) { return cb.value; });
        var nota = document.getElementById("txtNotaRebotar").value.trim();

        if (!docsObservados.length) {
            Swal.fire({
                icon: "warning",
                title: "Selecciona al menos un documento con anomalía",
                confirmButtonText: "Ok"
            });
            return;
        }
        if (!nota) {
            Swal.fire({
                icon: "warning",
                title: "Escribe una nota para el analista",
                confirmButtonText: "Ok"
            });
            return;
        }

        var payload = _modoConsolidadaFinancieros
            ? { IdConsolidada: expedienteFinActual, Observaciones: nota, DocumentosObservados: docsObservados }
            : { IdRequisicion: expedienteFinActual, Observaciones: nota, DocumentosObservados: docsObservados };

        $.ajax({
            url: urlRebotarDocumentos,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(payload),
            success: function (res) {
                if (res.success) {
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalRebotar")).hide();
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalExpedienteFinancieros")).hide();
                    Swal.fire({
                        icon: "success",
                        title: "Documentos regresados al analista",
                        confirmButtonText: "Aceptar"
                    }).then(function () { location.reload(); });
                }
            },
            error: function () {
                Swal.fire({ icon: "error", title: "Error al rebotar los documentos." });
            }
        });
    };

    // ── Enviar atención ─────────────────────────────────────────────────────
    window.enviarAtencion = function () {
        var esConsolidada = consolidadaActual != null;
        var observaciones = document.getElementById("txtObservaciones").value.trim();
        if (!observaciones) {
            Swal.fire({ icon: "warning", title: "Debe escribir una observaci\u00f3n.", confirmButtonText: "Ok" });
            return;
        }

        var formData = new FormData();
        if (esConsolidada) {
            formData.append("IdConsolidada", consolidadaActual);
        } else {
            formData.append("IdRequisicion", requisicionActual);
        }
        formData.append("Observaciones", observaciones);

        var inputSiaf = document.getElementById("inputSiaf");
        var inputTablaApi = document.getElementById("inputTablaApi");

        if (inputSiaf) {
            for (var i = 0; i < inputSiaf.files.length; i++)
                formData.append("DocSiaf", inputSiaf.files[i]);
        }

        if (inputTablaApi) {
            for (var i = 0; i < inputTablaApi.files.length; i++)
                formData.append("TablaApi", inputTablaApi.files[i]);
        }

        var url = esConsolidada ? urlAtenderConsolidada : atenderUrl;

        $.ajax({
            url: url,
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                bootstrap.Modal.getInstance(document.getElementById("modalDetalle")).hide();
                if (inputSiaf) inputSiaf.value = "";
                if (inputTablaApi) inputTablaApi.value = "";
                document.getElementById("txtObservaciones").value = "";
                var titulo = esConsolidada ? "Consolidada autorizada" : "Requisici\u00f3n autorizada";
                Swal.fire({
                    icon: "success",
                    title: titulo,
                    text: "Se autoriz\u00f3 correctamente.",
                    confirmButtonText: "Aceptar",
                    confirmButtonColor: "#fe6291"
                }).then(function () { location.reload(); });
            },
            error: function () {
                Swal.fire({ icon: "error", title: "Error al atender la " + (esConsolidada ? "consolidada" : "requisici\u00f3n") + "." });
            }
        });
    };

    // ── Rechazar desde modal atender ────────────────────────────────────────
    window.rechazarDesdeAtencion = function () {
        var observaciones = document.getElementById("txtObservaciones").value.trim();
        if (!observaciones) {
            Swal.fire({ icon: 'warning', title: 'Escribe el motivo del rechazo', confirmButtonText: 'Ok' });
            return;
        }

        Swal.fire({
            title: '¿Rechazar esta requisición?',
            text: 'Se enviará con las observaciones escritas.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#e53e3e',
            cancelButtonColor: 'var(--slate-500)',
            confirmButtonText: 'Sí, rechazar',
            cancelButtonText: 'Cancelar'
        }).then(function (result) {
            if (result.isConfirmed) {
                $.post(urlRechazar, { idRequi: requisicionActual, motivo: observaciones }, function (res) {
                    if (res.success) {
                        bootstrap.Modal.getInstance(document.getElementById("modalDetalle")).hide();
                        Swal.fire({
                            icon: 'success', title: 'Requisición rechazada'
                        }).then(function () { location.reload(); });
                    } else {
                        Swal.fire({ icon: 'error', title: 'No se pudo rechazar' });
                    }
                });
            }
        });
    };

    // ── Helpers UI ──────────────────────────────────────────────────────────
    function mostrarMensajeVacio(totalVisibles, tbodyOptional) {
        var tbody = tbodyOptional || document.querySelector(".tabla-requisiciones:not(#tablaModalDetalle) tbody");
        if (!tbody) return;
        var filaVacia = tbody.querySelector(".fila-vacia");

        var table = tbody.closest("table");
        var hayConsolidadas = false;
        if (table) {
            var tbodys = table.querySelectorAll("tbody");
            for (var i = 0; i < tbodys.length; i++) {
                if (tbodys[i] !== tbody) {
                    var rows = tbodys[i].querySelectorAll("tr:not(.fila-vacia):not(.fila-detalle)");
                    if (rows.length > 0) { hayConsolidadas = true; break; }
                }
            }
        }

        if (totalVisibles === 0 && !hayConsolidadas) {
            if (!filaVacia) {
                var colCount = table ? table.querySelectorAll("thead tr th").length : 8;
                filaVacia = document.createElement("tr");
                filaVacia.className = "fila-vacia";
                filaVacia.innerHTML = '<td colspan="' + colCount + '" class="text-center">Sin resultados para los filtros aplicados</td>';
                tbody.appendChild(filaVacia);
            }
        } else {
            if (filaVacia) filaVacia.remove();
        }
    }

    // ── Tabs ────────────────────────────────────────────────────────────────
    function cargarRequisicionesConDocumentos() {
        var tbodyDocumentos = document.getElementById("tbodyDocumentos");
        if (!tbodyDocumentos) return;

        fetch(urlObtenerArchivos, {
            method: "GET",
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data || !data.requisiciones || data.requisiciones.length === 0) {
                    tbodyDocumentos.innerHTML =
                        '<tr id="filaVaciaDocumentos" class="fila-vacia"><td colspan="8" class="text-center">No hay requisiciones con documentos</td></tr>';
                    return;
                }
                tbodyDocumentos.innerHTML = "";
                data.requisiciones.forEach(function (item, idx) {
                    var tr = document.createElement("tr");
                    tr.className = "fila-requi";
                    tr.setAttribute("data-requi-id", item.idRequi);
                    tr.innerHTML =
                        '<td style="text-align:center">' + (idx + 1) + '</td>' +
                        '<td><span class="folio-badge">' + (item.numRequi || "—") + '</span></td>' +
                        '<td>' + item.fechaEmision + '</td>' +
                        '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-building"></i>' + (item.departamento || "—") + '</span></div></td>' +
                        '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-user"></i>' + (item.responsable || "—") + '</span></div></td>' +
                        '<td style="text-align:center">' + item.cantidadPartidas + '</td>' +
                        '<td>' + obtenerBadgeEstatusHtml(item.idEstatus || 0, item.estatus) + '</td>' +
                        '<td style="text-align:center">' +
                            '<button class="btn-accion btn-ver" title="Ver archivos" onclick="verArchivosRequisicionFin(' + item.idRequi + ')">' +
                                '<i class="fa-solid fa-file"></i>' +
                            '</button>' +
                        '</td>';
                    tbodyDocumentos.appendChild(tr);
                });
            })
            .catch(function (err) {
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

        fetch(urlConsolidadasDocumentos, { credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                tbody.setAttribute("data-cargado", "1");

                if (!data || !data.consolidadas || data.consolidadas.length === 0) return;

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
                        "<td><span class=\"folio-badge\">" + (c.folioConsolidada || "—") + "</span> <span style='font-size:10px;color:var(--color-text-secondary);'>(<i class='fa-solid fa-layer-group'></i> Consolidada)</span></td>" +
                        "<td>" + (c.fechaCreacion || "—") + "</td>" +
                        '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-building"></i>' + (c.departamentos || "—") + '</span></div></td>' +
                        '<td style="text-align:center">' + (c.cantidadRequis || 0) + "</td>" +
                        '<td style="text-align:center">' + (c.totalPartidas || 0) + "</td>" +
                        "<td>" + obtenerBadgeEstatusHtml(c.idEstatus || 0, c.estatus) + "</td>" +
                        '<td style="text-align:center">' +
                        '<div class="acciones-grupo" style="justify-content:center">' +
                        '<button class="btn-accion btn-ver" title="Ver archivos" ' +
                        'onclick="verArchivosConsolidadaFin(' + c.consolidadaId + ')">' +
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

    window.verArchivosConsolidadaFin = function (idConsolidada) {
        if (window.DocumentosRequisicionModal) {
            window.DocumentosRequisicionModal.open({
                idConsolidada: idConsolidada,
                fetchUrl: urlObtenerArchivosConsolidada,
                downloadZipUrl: urlDescargarArchivosConsolidadaZip
            });
            return;
        }

        fetch(urlObtenerArchivosConsolidada + "?idConsolidada=" + idConsolidada, {
            method: "GET",
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (archivos) {
                if (!archivos || archivos.length === 0) {
                    alert("Esta consolidada no tiene archivos vinculados");
                    return;
                }

                var archivoActual = archivos[0];
                var esImagen = /\.(jpg|jpeg|png|gif|webp)$/i.test(archivoActual.nombreArchivo);
                var esPdf = /\.pdf$/i.test(archivoActual.nombreArchivo);

                var listaHtml = '<ul style="list-style: none; padding: 0; margin: 0;">';
                archivos.forEach(function (arch, idx) {
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
                archivoItems.forEach(function (item) {
                    item.addEventListener("click", function () {
                        archivoItems.forEach(function (it) { it.classList.remove("activo"); });
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
            .catch(function (err) {
                console.error("Error cargando archivos de consolidada:", err);
                alert("Error al cargar los archivos");
            });
    };

    window.verArchivosRequisicionFin = function (idRequisicion) {
        if (window.DocumentosRequisicionModal) {
            window.DocumentosRequisicionModal.open({
                idRequisicion: idRequisicion,
                fetchUrl: urlObtenerTodosArchivos,
                downloadZipUrl: urlDescargarTodosArchivosZip
            });
            return;
        }

        fetch(urlObtenerTodosArchivos + "?idRequisicion=" + idRequisicion, {
            method: "GET",
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (archivos) {
                if (!archivos || archivos.length === 0) {
                    alert("Esta requisición no tiene archivos vinculados");
                    return;
                }

                var archivoActual = archivos[0];
                var esImagen = /\.(jpg|jpeg|png|gif|webp)$/i.test(archivoActual.nombreArchivo);
                var esPdf = /\.pdf$/i.test(archivoActual.nombreArchivo);

                var listaHtml = '<ul style="list-style: none; padding: 0; margin: 0;">';
                archivos.forEach(function (arch, idx) {
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
                                        arch.tipo + ' • ' + arch.fechaSubida +
                                    '</div>' +
                                '</div>' +
                                '<a href="' + arch.ruta + '" download style="margin-left: 10px; white-space: nowrap; padding: 4px 8px; font-size: 11px; color: #666; border: 1px solid #ddd; border-radius: 4px; text-decoration: none; display: inline-block; transition: all 0.2s; background: #f8f8f8;" onmouseover="this.style.background=\'#efefef\'; this.style.color=\'#333\';" onmouseout="this.style.background=\'#f8f8f8\'; this.style.color=\'#666\'">' +
                                    '<i class="fa-solid fa-download" style="font-size: 9px; margin-right: 4px;"></i>Descargar' +
                                '</a>' +
                            '</div>' +
                        '</li>';
                });
                listaHtml += '</ul>';

                var previewHtml = '';
                if (esPdf) {
                    previewHtml = '<iframe src="' + archivoActual.ruta + '" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>';
                } else if (esImagen) {
                    previewHtml = '<img src="' + archivoActual.ruta + '" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />';
                } else {
                    previewHtml = '<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;">' +
                        '<div style="text-align: center; color: #999;">' +
                            '<i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i>' +
                            '<p>No hay vista previa disponible</p>' +
                            '<p style="font-size: 0.9rem;">Descarga el archivo para verlo</p>' +
                        '</div>' +
                    '</div>';
                }

                var modal = document.createElement("div");
                modal.className = "modal fade";
                modal.setAttribute("tabindex", "-1");
                modal.setAttribute("aria-hidden", "true");
                modal.innerHTML =
                    '<div class="modal-dialog modal-xl modal-dialog-centered">' +
                        '<div class="modal-content modal-premium">' +
                            '<div class="modal-header modal-header-premium">' +
                                '<div>' +
                                    '<h5 class="modal-title modal-titulo-premium">Archivos de la Requisición</h5>' +
                                    '<p class="modal-subtitulo-premium">Visualiza y descarga los documentos adjuntos</p>' +
                                '</div>' +
                                '<button type="button" class="modal-btn-cerrar" data-bs-dismiss="modal">' +
                                    '<i class="fa-solid fa-xmark"></i>' +
                                '</button>' +
                            '</div>' +
                            '<div class="modal-body modal-body-premium" style="padding: 0;">' +
                                '<div style="display: flex; height: 600px;">' +
                                    '<div style="flex: 1; overflow-y: auto; border-right: 1px solid #e0e0e0; background: #fafafa;">' +
                                        '<div style="padding: 0;">' + listaHtml + '</div>' +
                                    '</div>' +
                                    '<div style="flex: 2; padding: 20px; display: flex; align-items: center; justify-content: center; background: white;" id="previewContainer">' +
                                        previewHtml +
                                    '</div>' +
                                '</div>' +
                            '</div>' +
                        '</div>' +
                    '</div>';
                document.body.appendChild(modal);

                var bsModal = new bootstrap.Modal(modal);
                bsModal.show();

                var archivoItems = modal.querySelectorAll(".archivo-item");
                archivoItems.forEach(function (item) {
                    item.addEventListener("click", function () {
                        archivoItems.forEach(function (it) { it.classList.remove("activo"); });
                        this.classList.add("activo");

                        var ruta = this.getAttribute("data-archivo");
                        var nombre = this.getAttribute("data-nombre");
                        var esImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(nombre);
                        var isPdf = /\.pdf$/i.test(nombre);

                        var container = document.getElementById("previewContainer");
                        var nuevoPreview = '';
                        if (isPdf) {
                            nuevoPreview = '<iframe src="' + ruta + '" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>';
                        } else if (esImg) {
                            nuevoPreview = '<img src="' + ruta + '" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />';
                        } else {
                            nuevoPreview = '<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;">' +
                                '<div style="text-align: center; color: #999;">' +
                                    '<i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i>' +
                                    '<p>No hay vista previa disponible</p>' +
                                    '<p style="font-size: 0.9rem;">Descarga el archivo para verlo</p>' +
                                '</div>' +
                            '</div>';
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

                modal.addEventListener("hidden.bs.modal", function () { modal.remove(); });
            })
            .catch(function (err) {
                console.error("Error cargando archivos:", err);
                alert("Error al cargar los archivos");
            });
    };

    // ── Consolidada: cargar, filtrar, detalle ───────────────────────────────
    var CONSOL_COLSPAN = 9; // # + Folio + Deptos + #Req + Fecha + Estado + Asignado + Estatus + Acciones

    window.cargarConsolidadas = function () {
        var tbody = document.getElementById("tbodyConsolidadas");
        if (!tbody) return;

        var URL = urlConsolidadas;
        if (!URL) return;

        fetch(URL, {
            method: "GET",
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data || !data.length) {
                    tbody.innerHTML = '<tr class="fila-vacia"><td colspan="' + CONSOL_COLSPAN + '" class="text-center">No hay consolidadas en seguimiento</td></tr>';
                    return;
                }

                tbody.innerHTML = "";
                data.forEach(function (item, idx) {
                    var tr = document.createElement("tr");
                    tr.className = "fila-requi";
                    tr.setAttribute("data-consolidada-id", item.consolidadaId);
                    tr.setAttribute("data-estatus", item.idEstatus);
                    tr.setAttribute("data-folio", (item.folioConsolidada || "").toLowerCase());
                    tr.setAttribute("data-deptos", (item.departamentos || "").toLowerCase());

                    var badgeHtml = obtenerBadgeEstatusHtml(item.idEstatus, item.estatus);

                    var diasHtml = "";
                    if (item.diasAsignado > 0 || item.nombreAsignado) {
                        var claseColor = item.diasAsignado <= 3 ? "badge-semaforo-verde" : (item.diasAsignado <= 5 ? "badge-semaforo-amarillo" : "badge-semaforo-rojo");
                        diasHtml = '<span class="badge-semaforo-premium ' + claseColor + '">' +
                            '<i class="fa-solid fa-circle"></i>' +
                            '<span>' + (item.diasAsignado === 0 ? "Hoy" : item.diasAsignado + (item.diasAsignado === 1 ? " día" : " días")) + '</span>' +
                            '</span>';
                    } else {
                        diasHtml = '<span class="text-muted" style="font-size: 11px; font-style: italic;">Sin asignación</span>';
                    }

                    var accionesHtml = '<div class="acciones-grupo" style="justify-content:center">' +
                        '<button class="btn-accion btn-ver" title="Ver detalle" onclick="verDetalleConsolidadaFinancieros(' + item.consolidadaId + ')">' +
                        '<i class="fa-solid fa-eye"></i></button>' +
                        '<button class="btn-accion" title="Ver historial" onclick="event.stopPropagation(); window.location.href=\'' + urlHistorialApi + '?idConsolidada=' + item.consolidadaId + '\'">' +
                        '<i class="fa-solid fa-clock-rotate-left"></i></button>';
                    if (esRol8 && item.idEstatus === 13) {
                        accionesHtml += '<button class="btn-accion" title="Asignar Consolidada" onclick="abrirModalAsignar(' + item.consolidadaId + ', \'consolidada\')">' +
                            '<i class="fa-solid fa-clipboard-user"></i></button>';
                    }
                    if (esRol9 && item.idEstatus === 14) {
                        accionesHtml += '<button class="btn-accion" title="Atender" onclick="atenderConsolidada(' + item.consolidadaId + ')">' +
                            '<i class="fa-solid fa-hand-holding"></i></button>';
                    }
                    accionesHtml += '</div>';

                    var celdas = '';
                    celdas += '<td style="text-align:center">' + (idx + 1) + '</td>';
                    celdas += '<td><span class="folio-badge">' + (item.folioConsolidada || "—") + '</span></td>';
                    celdas += '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-building"></i>' + (item.departamentos || "—") + '</span></div></td>';
                    celdas += '<td style="text-align:center">' + item.cantidadRequis + '</td>';
                    celdas += '<td>' + item.fechaCreacion + '</td>';
                    celdas += '<td>' + badgeHtml + '</td>';
                    if (esRol8) {
                        celdas += '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-clipboard-user"></i>' + (item.nombreAsignado || "Sin asignar") + '</span></div></td>';
                    }
                    if (esRol8 || esRol9) {
                        celdas += '<td>' + diasHtml + '</td>';
                    }
                    celdas += '<td>' + accionesHtml + '</td>';

                    tr.innerHTML = celdas;
                    tbody.appendChild(tr);
                });

                aplicarFiltrosConsolidadas();
            })
            .catch(function (err) {
                console.error("Error cargando consolidadas:", err);
                tbody.innerHTML = '<tr class="fila-vacia"><td colspan="' + CONSOL_COLSPAN + '" class="text-center">Error al cargar los datos</td></tr>';
            });
    };

    window.cargarConsolidadasAutorizadasFinancieros = function () {
        var tbody = document.getElementById("tbodyAutorizadasConsolidadas");
        if (!tbody) return;
        if (tbody.getAttribute("data-cargado") === "1") return;
        if (!urlConsolidadasAutorizadas) return;

        fetch(urlConsolidadasAutorizadas, {
            method: "GET",
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                tbody.setAttribute("data-cargado", "1");

                if (!data || !data.length) return;

                var filaVacia = document.getElementById("filaVaciaAutorizadas");
                if (filaVacia) filaVacia.remove();
                var tbodyIndAut = document.getElementById("tbodyAutorizadasIndividuales");
                if (tbodyIndAut) {
                    var fvAut = tbodyIndAut.querySelector(".fila-vacia");
                    if (fvAut) fvAut.remove();
                }

                tbody.innerHTML = "";
                data.forEach(function (item, idx) {
                    var tr = document.createElement("tr");
                    tr.className = "fila-requi fila-consolidada";
                    tr.setAttribute("data-consolidada-id", item.consolidadaId);
                    tr.setAttribute("data-estatus", item.idEstatus);
                    tr.setAttribute("data-folio", (item.folioConsolidada || "").toLowerCase());
                    tr.setAttribute("data-deptos", (item.departamentos || "").toLowerCase());

                    var badgeHtml = obtenerBadgeEstatusHtml(item.idEstatus, item.estatus);

                    var diasHtml = "";
                    if (item.diasAsignado > 0 || item.nombreAsignado) {
                        var claseColor = item.diasAsignado <= 3 ? "badge-semaforo-verde" : (item.diasAsignado <= 5 ? "badge-semaforo-amarillo" : "badge-semaforo-rojo");
                        diasHtml = '<span class="badge-semaforo-premium ' + claseColor + '">' +
                            '<i class="fa-solid fa-circle"></i>' +
                            '<span>' + (item.diasAsignado === 0 ? "Hoy" : item.diasAsignado + (item.diasAsignado === 1 ? " día" : " días")) + '</span>' +
                            '</span>';
                    } else {
                        diasHtml = '<span class="text-muted" style="font-size: 11px; font-style: italic;">Sin asignación</span>';
                    }

                    var accionesHtml = '<div class="acciones-grupo" style="justify-content:center">' +
                        '<button class="btn-accion btn-ver" title="Ver detalle" onclick="verDetalleConsolidadaFinancieros(' + item.consolidadaId + ')">' +
                        '<i class="fa-solid fa-eye"></i></button>' +
                        '<button class="btn-accion" title="Ver historial" onclick="event.stopPropagation(); window.location.href=\'' + urlHistorialApi + '?idConsolidada=' + item.consolidadaId + '\'">' +
                        '<i class="fa-solid fa-clock-rotate-left"></i></button>' +
                        '</div>';

                    var celdas = '';
                    celdas += '<td style="text-align:center">' + (idx + 1) + '</td>';
                    celdas += '<td><span class="folio-badge">' + (item.folioConsolidada || "—") + '</span> <span style="font-size:10px;color:var(--color-text-secondary);">(<i class="fa-solid fa-layer-group"></i> Consolidada)</span></td>';
                    celdas += '<td>' + (item.fechaCreacion || "—") + '</td>';
                    celdas += '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-building"></i>' + (item.departamentos || "—") + '</span></div></td>';
                    celdas += '<td>' + (item.nombreAsignado || "Sin asignar") + '</td>';
                    celdas += '<td style="text-align:center">' + (item.cantidadRequis || 0) + '</td>';
                    celdas += '<td>' + badgeHtml + '</td>';
                    if (esRol8) {
                        celdas += '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-clipboard-user"></i>' + (item.nombreAsignado || "Sin asignar") + '</span></div></td>';
                    }
                    if (esRol8 || esRol9) {
                        celdas += '<td>' + diasHtml + '</td>';
                    }
                    celdas += '<td>' + accionesHtml + '</td>';

                    tr.innerHTML = celdas;
                    tbody.appendChild(tr);
                });

                aplicarPaginacionRequisiciones();
            })
            .catch(function (err) {
                console.error("Error cargando consolidadas autorizadas:", err);
            });
    };

    var _datosProcesoPago = null;

    function renderProcesoPago() {
        var tbody = document.getElementById("tbodyProcesoPago");
        if (!tbody) return;
        tbody.innerHTML = "";
        var data = _datosProcesoPago;
        if (!data || !data.length) {
            tbody.innerHTML = '<tr class="fila-vacia"><td colspan="9" class="text-center">No hay requisiciones en proceso de pago</td></tr>';
            return;
        }

        var textoFolio = ((document.getElementById("filtroNumReq") && document.getElementById("filtroNumReq").value) || "").toLowerCase().trim();
        var textoDepto = ((document.getElementById("filtroDepartamento") && document.getElementById("filtroDepartamento").value) || "").toLowerCase().trim();
        var textoFecha = fechaSeleccionada || "";

        var filtrados = data.filter(function (item) {
            var pasaFolio = !textoFolio || (item.folio || "").toLowerCase().indexOf(textoFolio) !== -1;
            var pasaDepto = !textoDepto || (item.departamento || "").toLowerCase().indexOf(textoDepto) !== -1;
            var pasaFecha = !textoFecha || (item.fecha || "").indexOf(textoFecha) !== -1;
            return pasaFolio && pasaDepto && pasaFecha;
        });

        filtrados.forEach(function (item, idx) {
            var tr = document.createElement("tr");
            tr.className = "fila-requi";
            tr.setAttribute("data-id", item.id);
            tr.setAttribute("data-es-consolidada", item.esConsolidada ? "true" : "false");
            var badgeHtml = obtenerBadgeEstatusHtml(item.idEstatus || 0, item.estatus);
            var tipoHtml = item.esConsolidada
                ? '<span class="badge-estatus" style="background:#e0e7ff;color:#3730a3;font-size:10px;">Consolidada</span>'
                : '<span class="badge-estatus" style="background:#f0fdf4;color:#166534;font-size:10px;">Individual</span>';
            tr.innerHTML =
                '<td style="text-align:center">' + (idx + 1) + '</td>' +
                '<td><span class="folio-badge">' + (item.folio || "") + '</span></td>' +
                '<td>' + (item.fecha || "") + '</td>' +
                '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-building"></i>' + (item.departamento || "") + '</span></div></td>' +
                '<td><div class="tabla-meta-stack"><span class="tabla-meta-principal"><i class="fa-solid fa-user"></i>' + (item.responsable || "") + '</span></div></td>' +
                '<td style="text-align:center">' + (item.partidas || 0) + '</td>' +
                '<td>' + badgeHtml + '</td>' +
                '<td>' + tipoHtml + '</td>' +
                '<td><div class="acciones-grupo" style="justify-content:center">' +
                '<button class="btn-accion btn-ver" title="Ver expediente" onclick="verExpedienteFinancieros(' + item.id + ', ' + item.esConsolidada + ')">' +
                '<i class="fa-solid fa-folder-open"></i></button>' +
                '</div></td>';
            tbody.appendChild(tr);
        });

        if (!filtrados.length) {
            tbody.innerHTML = '<tr class="fila-vacia"><td colspan="9" class="text-center">Sin resultados para los filtros aplicados</td></tr>';
        }
    }

    window.cargarProcesoPago = function () {
        var tbody = document.getElementById("tbodyProcesoPago");
        if (!tbody || !urlProcesoPago) return;

        if (_datosProcesoPago) {
            renderProcesoPago();
            return;
        }

        fetch(urlProcesoPago, {
            method: "GET",
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                _datosProcesoPago = data;
                renderProcesoPago();
            })
            .catch(function () {
                tbody.innerHTML = '<tr class="fila-vacia"><td colspan="9" class="text-center">Error al cargar los datos</td></tr>';
            });
    };

    function aplicarFiltrosConsolidadas() {
        var tbody = document.getElementById("tbodyConsolidadas");
        if (!tbody) return;

        var textoFolio = ((document.getElementById("filtroNumReq") && document.getElementById("filtroNumReq").value) || "").toLowerCase().trim();
        var textoDepto = ((document.getElementById("filtroDepartamento") && document.getElementById("filtroDepartamento").value) || "").toLowerCase().trim();
        var textoEstado = ((document.getElementById("filtroEstado") && document.getElementById("filtroEstado").value) || "").toLowerCase().trim();

        var filas = tbody.querySelectorAll("tr.fila-requi");
        var visibles = 0;
        filas.forEach(function (fila) {
            var folio = fila.getAttribute("data-folio") || "";
            var deptos = fila.getAttribute("data-deptos") || "";
            var estado = (fila.querySelector("td:nth-child(6)")?.textContent || "").toLowerCase().trim();
            var fecha = (fila.querySelector("td:nth-child(5)")?.textContent || "").trim();

            var pasaFolio = !textoFolio || folio.indexOf(textoFolio) !== -1;
            var pasaDepto = !textoDepto || deptos.indexOf(textoDepto) !== -1;
            var pasaEstado = !textoEstado || estado.indexOf(textoEstado) !== -1;
            var pasaFecha = !fechaSeleccionada || fecha === fechaSeleccionada;

            if (pasaFolio && pasaDepto && pasaEstado && pasaFecha) {
                fila.style.display = "";
                visibles++;
            } else {
                fila.style.display = "none";
            }
        });

        var filaVacia = tbody.querySelector(".fila-vacia");
        if (visibles === 0 && !filaVacia) {
            var colCount = tbody.querySelector("tr")?.cells?.length || 9;
            var empty = document.createElement("tr");
            empty.className = "fila-vacia";
            empty.innerHTML = '<td colspan="' + colCount + '" class="text-center">Sin resultados para los filtros aplicados</td>';
            tbody.appendChild(empty);
        } else if (visibles > 0) {
            var fv = tbody.querySelector(".fila-vacia");
            if (fv) fv.remove();
        }
    }

    // Wire up consolidada and procesopago filters to the general filter inputs
    document.addEventListener("input", function (e) {
        if (e.target.id === "filtroNumReq" || e.target.id === "filtroDepartamento") {
            aplicarFiltrosConsolidadas();
            if (_datosProcesoPago) renderProcesoPago();
        }
    });
    document.addEventListener("change", function (e) {
        if (e.target.id === "filtroEstado") {
            aplicarFiltrosConsolidadas();
            if (_datosProcesoPago) renderProcesoPago();
        }
    });

    window.verDetalleConsolidadaFinancieros = function (idConsolidada) {
        var URL = urlDetalleConsolidada;
        if (!URL) return;

        // Hide "Req." column (only visible in consolidada atender mode)
        document.querySelectorAll(".col-requi").forEach(function (th) { th.style.display = "none"; });

        // Limpiar modal para contenido nuevo
        var tablaDetalle = document.getElementById("tablaDetalle");
        if (tablaDetalle) tablaDetalle.innerHTML = "";
        var archivosReadonly = document.getElementById("contenedorArchivosReadonly");
        if (archivosReadonly) archivosReadonly.innerHTML = "";
        var galeriaFotos = document.getElementById("galeriaFotosDetalle");
        if (galeriaFotos) galeriaFotos.innerHTML = "";
        var seccionFotos = document.getElementById("seccionFotosDetalle");
        if (seccionFotos) seccionFotos.style.display = "none";
        // Ocultar sección atender (no aplica para consolidada)
        document.querySelectorAll(".seccionAtender").forEach(function (s) { s.style.display = "none"; });
        var botonesAtender = document.getElementById("botonesAtender");
        if (botonesAtender) botonesAtender.style.display = "none";

        $.get(URL, { idConsolidada: idConsolidada }, function (data) {
            var modalTitle = document.querySelector("#modalDetalle .modal-titulo-premium");
            if (modalTitle) modalTitle.textContent = "Consolidada: " + (data.folioConsolidada || "");
            var subtitle = document.querySelector("#modalDetalle .modal-subtitulo-premium");
            if (subtitle) subtitle.textContent = "Detalle de la consolidada" + (data.articulos ? " · " + data.articulos.length + " partidas" : "");

            // ── Articles table ─────────────────────────────────────────────
            var articulosHtml = '<div class="consolidada-articulos-section"><h6 class="consolidada-section-title"><i class="fa-solid fa-layer-group"></i> Artículos consolidados</h6>' +
                '<div class="table-responsive-container"><table class="tabla-requisiciones" id="tablaDetalleConsolidada"><thead><tr>' +
                '<th>Requisición</th><th>Partida</th><th style="width:100px;">Cantidad</th><th style="width:135px;">Unidad</th><th>Descripción</th>' +
                '</tr></thead><tbody>';
            if (data.articulos && data.articulos.length) {
                data.articulos.forEach(function (a) {
                    var txtCompleto = a.descripcionDetallada || "";
                    var txtCorto = txtCompleto.length > 28 ? txtCompleto.substring(0, 28) + "…" : txtCompleto || "Sin descripción...";
                    var fullEsc = (txtCompleto || "").replace(/"/g, "&quot;");
                    articulosHtml += '<tr>' +
                        '<td>' + (a.numRequi || "") + '</td>' +
                        '<td>' + (a.numPartida || "") + '</td>' +
                        '<td>' + (a.cantidad || "") + '</td>' +
                        '<td>' + (a.unidadMedida || "") + '</td>' +
                        '<td>' + (a.descripcion || "") +
                        '<div class="desc-preview-modal" data-full="' + fullEsc + '" onclick="verDescDetalleModal(this)" style="margin-top:2px;">' +
                        '<span class="desc-texto-preview' + (txtCompleto ? " tiene-texto" : "") + '">' + txtCorto + '</span>' +
                        '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                        '</tr>';
                });
            } else {
                articulosHtml += '<tr class="fila-vacia"><td colspan="5" class="text-center">Sin artículos</td></tr>';
            }
            articulosHtml += '</tbody></table></div></div>';

            // ── Files ──────────────────────────────────────────────────────
            var archivosHtml = '<div style="margin-top:16px;">';
            if (window.ModalAdjuntos) {
                if (data.cuadroComparativo && data.cuadroComparativo.length) {
                    archivosHtml += '<div id="consolCuadroComparativo"></div>';
                }
                if (data.anexos && data.anexos.length) {
                    archivosHtml += '<div id="consolAnexos" style="margin-top:12px;"></div>';
                }
            }
            archivosHtml += '</div>';

            var contenidoCompleto = articulosHtml + archivosHtml;
            var contenedor = document.getElementById("tablaDetalle");
            if (contenedor) {
                // Instead of replacing tablaDetalle which is inside the table, wrap everything
                var parentTable = contenedor.closest(".table-responsive-container");
                if (parentTable) parentTable.style.display = "none";
            }
            // Insert into contenedorArchivosReadonly
            if (archivosReadonly) {
                archivosReadonly.innerHTML = contenidoCompleto;
            }

            // Render files using ModalAdjuntos
            if (window.ModalAdjuntos) {
                var cuadroEl = document.getElementById("consolCuadroComparativo");
                if (cuadroEl && data.cuadroComparativo && data.cuadroComparativo.length) {
                    cuadroEl.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                    window.ModalAdjuntos.enlazarEventosContenedor(cuadroEl);
                }
                var anexosEl = document.getElementById("consolAnexos");
                if (anexosEl && data.anexos && data.anexos.length) {
                    anexosEl.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
                    window.ModalAdjuntos.enlazarEventosContenedor(anexosEl);
                }
            }

            // Hide upload/observations section.
            document.querySelectorAll(".modal-body .seccionAtender").forEach(function (s) { s.style.display = "none"; });

            new bootstrap.Modal(document.getElementById("modalDetalle")).show();
        }).fail(function () {
            Swal.fire({ icon: "error", title: "No se pudo cargar el detalle de la consolidada." });
        });
    };

    window.atenderConsolidada = function (idConsolidada) {
        consolidadaActual = idConsolidada;
        requisicionActual = null;
        var URL = urlDetalleConsolidada;
        if (!URL) return;

        // Show "Req." column in thead
        document.querySelectorAll(".col-requi").forEach(function (th) { th.style.display = ""; });

        // Restore articles table visibility (may have been hidden by verDetalleConsolidadaFinancieros)
        var detalleTableContainer = document.querySelector("#tablaDetalle")?.closest(".table-responsive-container");
        if (detalleTableContainer) detalleTableContainer.style.display = "";

        // Limpiar al abrir
        var tablaDetalle = document.getElementById("tablaDetalle");
        if (tablaDetalle) tablaDetalle.innerHTML = "";
        var archivosReadonly = document.getElementById("contenedorArchivosReadonly");
        if (archivosReadonly) archivosReadonly.innerHTML = "";
        var galeriaFotos = document.getElementById("galeriaFotosDetalle");
        if (galeriaFotos) galeriaFotos.innerHTML = "";
        var seccionFotos = document.getElementById("seccionFotosDetalle");
        if (seccionFotos) seccionFotos.style.display = "none";
        var inputSiaf = document.getElementById("inputSiaf");
        var inputTablaApi = document.getElementById("inputTablaApi");
        if (inputSiaf) inputSiaf.value = "";
        if (inputTablaApi) inputTablaApi.value = "";

        // Restore modal title
        var modalTitle = document.querySelector("#modalDetalle .modal-titulo-premium");
        if (modalTitle) modalTitle.textContent = "Consolidada: " + idConsolidada;
        var modalSubtitle = document.querySelector("#modalDetalle .modal-subtitulo-premium");
        if (modalSubtitle) modalSubtitle.textContent = "Detalle de la consolidada";

        // Show selects section + buttons
        document.querySelectorAll(".seccionAtender").forEach(function (sec) {
            sec.style.display = "block";
        });
        // Show Editar API button for consolidada, hide individual one
        var btnIndiv = document.querySelector(".seccionAtender .boton-gris[onclick*='editarTablaApi']");
        if (btnIndiv) btnIndiv.style.display = "none";
        var btnConsol = document.getElementById("btnEditarApiConsolidada");
        if (btnConsol) btnConsol.style.display = "block";
        var botonesAtender = document.getElementById("botonesAtender");
        if (botonesAtender) botonesAtender.style.display = "flex";

        $.get(URL, { idConsolidada: idConsolidada }, function (data) {
            modalTitle.textContent = "Consolidada: " + (data.folioConsolidada || "");
            modalSubtitle.textContent = "Detalle de la consolidada" + (data.articulos ? " \u00B7 " + data.articulos.length + " partidas" : "");

            // ── Articles table with "Req." column ──────────────────────────
            var articulosHtml = "";
            if (data.articulos && data.articulos.length) {
                data.articulos.forEach(function (a) {
                    var txtCompleto = a.descripcionDetallada || "";
                    var txtCorto = txtCompleto.length > 28 ? txtCompleto.substring(0, 28) + "\u2026" : txtCompleto || "Sin descripci\u00f3n...";
                    var fullEsc = (txtCompleto || "").replace(/"/g, "&quot;");
                    articulosHtml += '<tr>' +
                        '<td>' + (a.numRequi || "") + '</td>' +
                        '<td>' + (a.numPartida || "") + '</td>' +
                        '<td>' + (a.cantidad || "") + '</td>' +
                        '<td>' + (a.unidadMedida || "") + '</td>' +
                        '<td>' + (a.descripcion || "") + '</td>' +
                        '<td><div class="desc-preview-modal" data-full="' + fullEsc + '" onclick="verDescDetalleModal(this)">' +
                        '<span class="desc-texto-preview' + (txtCompleto ? " tiene-texto" : "") + '">' + txtCorto + '</span>' +
                        '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                        '</tr>';
                });
            } else {
                articulosHtml = '<tr class="fila-vacia"><td colspan="6" class="text-center">Sin art\u00edculos</td></tr>';
            }
            $("#tablaDetalle").html(articulosHtml);

            // ── Files: Cuadro Comparativo + Anexos ─────────────────────────
            var archivosHtml = '<div style="margin-top:16px;">';
            if (window.ModalAdjuntos) {
                if (data.cuadroComparativo && data.cuadroComparativo.length) {
                    archivosHtml += '<div id="consolAtenderCuadro"></div>';
                }
                if (data.anexos && data.anexos.length) {
                    archivosHtml += '<div id="consolAtenderAnexos" style="margin-top:12px;"></div>';
                }
            }
            archivosHtml += '</div>';

            if (archivosReadonly) {
                archivosReadonly.innerHTML = archivosHtml;
            }

            // Render files using ModalAdjuntos
            if (window.ModalAdjuntos) {
                var cuadroEl = document.getElementById("consolAtenderCuadro");
                if (cuadroEl && data.cuadroComparativo && data.cuadroComparativo.length) {
                    cuadroEl.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo);
                    window.ModalAdjuntos.enlazarEventosContenedor(cuadroEl);
                }
                var anexosEl = document.getElementById("consolAtenderAnexos");
                if (anexosEl && data.anexos && data.anexos.length) {
                    anexosEl.innerHTML = window.ModalAdjuntos.renderGrupoHtml("Documentos Anexos", data.anexos);
                    window.ModalAdjuntos.enlazarEventosContenedor(anexosEl);
                }
            }

            // Update subtitle with count
            if (data.articulos) {
                modalSubtitle.textContent = "Detalle de la consolidada \u00B7 Total: " + data.articulos.length + " partidas";
            }

            // Init SelectRosaBuscable for PP, FF, TipoPrograma + Select2 for municipio
            ["actividadSeleccionada", "ffSelect", "tipoProgramaSelect"].forEach(function (id) {
                var el = document.getElementById(id);
                if (el && window.SelectRosaBuscable) {
                    window.SelectRosaBuscable.destruir(el);
                }
            });
            var $mun = $("#municipio");
            if ($mun.data("select2")) $mun.select2("destroy");

            ["actividadSeleccionada", "ffSelect", "tipoProgramaSelect"].forEach(function (id) {
                var el = document.getElementById(id);
                if (el && window.SelectRosaBuscable) {
                    window.SelectRosaBuscable.inicializar(el, {
                        placeholder: id === "actividadSeleccionada" ? "Buscar actividad..." :
                                     id === "ffSelect" ? "Buscar fuente..." :
                                     "Buscar tipo de programa...",
                        defaultText: false
                    });
                }
            });
            $mun.select2({
                dropdownParent: $("#modalDetalle"),
                width: "100%",
                language: "es"
            });

            if (data.idPp) {
                $("#actividadSeleccionada").val(data.idPp).trigger("change");
            }
            if (data.ff) {
                $("#ffSelect").val(data.ff).trigger("change");
            }
            if (data.tipoPrograma) {
                $("#tipoProgramaSelect option").filter(function () { return $(this).text().trim() === data.tipoPrograma; }).prop("selected", true);
                $("#tipoProgramaSelect").trigger("change");
            }

            // Disable selects (readonly) — only the "Autorizar" action will process them
            $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio")
                .prop("disabled", true);
            ["actividadSeleccionada", "ffSelect", "tipoProgramaSelect"].forEach(function (id) {
                var el = document.getElementById(id);
                if (el && window.SelectRosaBuscable) {
                    window.SelectRosaBuscable.actualizar(el);
                }
            });

            // Clear and enable observations
            $("#txtObservaciones")
                .prop("readonly", false)
                .val("");

            new bootstrap.Modal(document.getElementById("modalDetalle")).show();
        }).fail(function () {
            Swal.fire({ icon: "error", title: "No se pudo cargar la consolidada para atender." });
            // Re-hide Req. column on error
            document.querySelectorAll(".col-requi").forEach(function (th) { th.style.display = "none"; });
        });
    };

    if (modoTabs && container) {
        var tabBtns = container.querySelectorAll(".almacen-tabs-btn");
        var tabPanels = container.querySelectorAll(".almacen-tab-panel");
        tabBtns.forEach(function (btn) {
            btn.addEventListener("click", function () {
                var tab = this.getAttribute("data-tab");

                if (tab === "documentos" && urlObtenerArchivos) {
                    cargarRequisicionesConDocumentos();
                    if (urlConsolidadasDocumentos) {
                        cargarConsolidadasDocumentos();
                    }
                }
                if (tab === "consolidadas" && urlConsolidadas) {
                    cargarConsolidadas();
                }
                if (tab === "procesopago" && urlProcesoPago) {
                    cargarProcesoPago();
                }
                if (tab === "autorizadas" && urlConsolidadasAutorizadas) {
                    cargarConsolidadasAutorizadasFinancieros();
                }

                var panelActivoAnterior = container.querySelector(".almacen-tab-panel.activo");
                if (panelActivoAnterior && panelActivoAnterior.id !== "tab-" + tab) {
                    var filasVistas = panelActivoAnterior.querySelectorAll("tr.fila-nueva");
                    filasVistas.forEach(function(f) { f.classList.remove("fila-nueva"); });
                }

                if (window.TabsNotificacionesRequi) {
                    window.TabsNotificacionesRequi.limpiarBadgeNotificacionTab(this);
                }
                var panelDestinoClick = document.getElementById("tab-" + tab);

                tabBtns.forEach(function (b) { b.classList.remove("activo"); });
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

    aplicarPaginacionRequisiciones();

    if (container && window.TabsNotificacionesRequi && modoTabs) {
        window.TabsNotificacionesRequi.mount({
            container: container,
            onNovedadEnTabActivo: function () {
                aplicarPaginacionRequisiciones();
            }
        });
    }

    // ── Expandir/colapsar fila detalle ──────────────────────────────────────
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

    // ── Filtros ─────────────────────────────────────────────────────────────
    var filtroNumReq = document.getElementById("filtroNumReq");
    var filtroDepto = document.getElementById("filtroDepartamento");
    var filtroEstado = document.getElementById("filtroEstado");
    if (filtroNumReq) filtroNumReq.addEventListener("input", filtrarTabla);
    if (filtroDepto) filtroDepto.addEventListener("input", filtrarTabla);
    if (filtroEstado) filtroEstado.addEventListener("change", filtrarTabla);

    // ── Panel descripción detallada ─────────────────────────────────────────
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
        if (!_descPanelModalTrigger.contains(e.target) && panel && !panel.contains(e.target)) {
            cerrarDescPanelModal();
        }
    });

    // ── Historial ───────────────────────────────────────────────────────────
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
                case 'done': bCls = 'mbadge-done'; bTxt = 'Completado'; break;
                case 'active': bCls = 'mbadge-active'; bTxt = 'En curso'; break;
                case 'completed': bCls = 'mbadge-completed'; bTxt = 'Finalizado'; break;
                case 'cancelled': bCls = 'mbadge-cancelled'; bTxt = 'Cancelada'; break;
                default: bCls = 'mbadge-pending'; bTxt = 'Pendiente'; break;
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

        new bootstrap.Modal(document.getElementById("modalHistorial")).show();

        if (!urlObtenerProgreso) {
            document.getElementById('historialSummary').innerHTML =
                '<div style="color:#b91c1c;text-align:center;padding:1rem;">URL de progreso no configurada</div>';
            return;
        }

        $.get(urlObtenerProgreso, { idRequisicion: idRequi }, function (data) {
            var steps = (data || []).map(function (s) {
                return {
                    dept: s.dept || s.Dept || '',
                    date: s.date || s.Date || '—',
                    state: s.state || s.State || 'pending',
                    by: s.by || s.By || '—',
                    time: s.time || s.Time || '—',
                    action: s.action || s.Action || '',
                    comment: s.comment || s.Comment || ''
                };
            });
            renderHistorialSteps(steps);
        }).fail(function () {
            document.getElementById('historialSummary').innerHTML =
                '<div style="color:#b91c1c;text-align:center;padding:1rem;"><i class="fa-solid fa-triangle-exclamation"></i> Error al cargar el historial</div>';
        });
    };

    // ── Modal close: restore consolidated state ──────────────────────────
    var _expFinModal = document.getElementById("modalExpedienteFinancieros");
    if (_expFinModal) {
        _expFinModal.addEventListener("hidden.bs.modal", function () {
            if (_modoConsolidadaFinancieros) {
                _modoConsolidadaFinancieros = false;
                document.querySelectorAll(".col-requi").forEach(function (th) { th.style.display = "none"; });
            }
        });
    }

})();
