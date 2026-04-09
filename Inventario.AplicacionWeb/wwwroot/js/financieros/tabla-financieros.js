(function () {
    var container = document.querySelector(".tabla-requi-page");
    var obtenerDetallesUrl = container ? container.getAttribute("data-url-obtener-detalles") : "";
    var verPdfRequisicionUrl = container ? container.getAttribute("data-url-ver-pdf-requi") : "";
    var verPdfServicioUrl = container ? container.getAttribute("data-url-ver-pdf-servicio") : "";
    var urlUsuariosFinancieros = container ? container.getAttribute("data-url-usuarios-financieros") : "";
    var urlAsignar = container ? container.getAttribute("data-url-asignar") : "";
    var urlRechazar = container ? container.getAttribute("data-url-rechazar") : "";
    var urlObtenerProgreso = container ? container.getAttribute("data-url-obtener-progreso") : "";
    var expedienteFinActual = null;
    var urlFinalizarExpediente = container
        ? container.getAttribute("data-url-finalizar-expediente")
        : "";
    var urlDescargarTablaApi = container
        ? container.getAttribute("data-url-descargar-tabla-api")
        : "";

    // Variables al inicio del módulo
    var urlObtenerDocsProveedor = container
        ? container.getAttribute("data-url-obtener-docs-proveedor") : "";
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
    const contenedor = document.querySelector(".tabla-requi-page");
    const atenderUrl = contenedor ? contenedor.dataset.urlAtender : "";

    var _idRequiAsignar = null;
    var fechaSeleccionada = "";
    var requisicionActual = null;

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
    window.abrirModalAsignar = function (idRequi) {
        _idRequiAsignar = idRequi;

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
        $.post(urlAsignar, { idRequi: _idRequiAsignar, idUsuario: idUsuario }, function (res) {
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

        // Construir la URL con el parámetro y abrir en nueva pestaña
        // El endpoint devuelve el PDF con Content-Disposition: attachment
        // así que el navegador lo descargará directamente.
        var url = (urlDescargarTablaApi || "").replace(/\/$/, "")
            + "?idRequisicion=" + requisicionActual;

        window.open(url, "_blank");
    };

    // ── Ver detalle / atender ───────────────────────────────────────────────
    window.atenderRequisicion = function (idMaestro) {
        requisicionActual = idMaestro;
        verDetalle(idMaestro, "atender");
    };

    window.verDetalle = function (idMaestro, modo) {
        modo = modo || "ver";

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
        var inputNumeroApi = document.getElementById("inputNumeroApi");
        if (inputSiaf) inputSiaf.value = "";
        if (inputTablaApi) inputTablaApi.value = "";
        if (inputNumeroApi) inputNumeroApi.value = "";

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

        $.get(obtenerDetallesUrl, { idMaestro: idMaestro }, function (data) {
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

            // Inicializar select2 y precargar valores
            if (isAtender || isReadonly) {
                $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").each(function () {
                    if ($(this).data("select2")) $(this).select2("destroy");
                });

                $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio").select2({
                    dropdownParent: $("#modalDetalle"),
                    width: "100%",
                    language: "es"
                });

                // Precargar valores desde la data (PP, FF, Programa, Municipio)
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

                // --- CAMBIO AQUÍ: Deshabilitar campos y cargar archivos para AMBOS modos ---
                $("#actividadSeleccionada, #ffSelect, #tipoProgramaSelect, #municipio")
                    .prop("disabled", true);

                if (window.ModalAdjuntos && typeof window.ModalAdjuntos.renderizarArchivosReadonly === 'function') {
                    window.ModalAdjuntos.renderizarArchivosReadonly(
                        data.cotizaciones || [],
                        data.cuadroComparativo || []
                    );
                }

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

    window.verExpedienteFinancieros = function (idRequi) {
        expedienteFinActual = idRequi;

        // Limpiar
        document.getElementById("expFinObservaciones").value = "";
        document.getElementById("expFinArchivosBase").innerHTML = "";
        document.getElementById("expFinGrupoSiaf").innerHTML = "";
        document.getElementById("expFinGrupoTablaApi").innerHTML = "";
        document.getElementById("expFinGrupoNumeroApi").innerHTML = "";
        document.getElementById("expFinArchivosFinancieros").style.display = "none";
        document.getElementById("expFinGaleriaFotos").innerHTML = "";
        document.getElementById("expFinSeccionFotos").style.display = "none";
        document.getElementById("expFinTablaBody").innerHTML = "";
        document.getElementById("expFinSubtitulo").textContent = "Cargando...";

        $.get(obtenerDetallesUrl, { idMaestro: idRequi }, function (data) {
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
                $("#expFinFf option").filter(function () {
                    return $(this).text().trim() === data.ff;
                }).prop("selected", true);
                $("#expFinFf").trigger("change");
            }
            if (data.tipoPrograma) {
                $("#expFinTipoPrograma option").filter(function () {
                    return $(this).text().trim() === data.tipoPrograma;
                }).prop("selected", true);
                $("#expFinTipoPrograma").trigger("change");
            }
            if (data.claveRegion) $("#expFinMunicipio").val(data.claveRegion).trigger("change");

            // Cotizaciones / cuadro
            (function () {
                var c = document.getElementById("expFinArchivosBase");
                if (window.ModalAdjuntos) {
                    c.innerHTML = 
                        window.ModalAdjuntos.renderGrupoHtml("Cotizaciones", data.cotizaciones || []) +
                        window.ModalAdjuntos.renderGrupoHtml("Cuadro comparativo", data.cuadroComparativo || []);
                    window.ModalAdjuntos.enlazarEventosContenedor(c);
                } else {
                    c.innerHTML = "";
                }
            })();

            // Observaciones
            document.getElementById("expFinObservaciones").value = data.observaciones || "";

            // Documentos financieros
            (function () {
                var siaf = data.archivosSiaf || [];
                var tablaApi = data.archivosTablaApi || [];
                var numApi = data.numeroApi || null;
                if (!siaf.length && !tablaApi.length && !numApi) return;

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
            })();

            $.get(urlObtenerDocsProveedor, { idRequisicion: idRequi }, function (docs) {
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

                    var fila = document.createElement("div");
                    fila.style.cssText = "display:flex; align-items:center; gap:10px; padding:8px 12px;" +
                        "border-radius:8px; border:1px solid " +
                        (subido ? "#bbf7d0" : "var(--color-border-tertiary)") + ";" +
                        "background:" + (subido ? "#f0fdf4" : "var(--color-background-secondary)") + ";";

                    var icono = subido
                        ? '<i class="fa-solid fa-circle-check" style="color:#16a34a;font-size:16px;flex-shrink:0;"></i>'
                        : '<i class="fa-regular fa-circle" style="color:#9ca3af;font-size:16px;flex-shrink:0;"></i>';

                    var linkVer = subido && archivo
                        ? '<a href="' + archivo.ruta + '" target="_blank" ' +
                        'style="font-size:11px;color:var(--color-text-secondary);margin-left:auto;' +
                        'text-decoration:none;padding:3px 8px;border:1px solid var(--color-border-secondary);' +
                        'border-radius:6px;">' +
                        '<i class="fa-solid fa-eye"></i> Ver</a>'
                        : '<span style="font-size:11px;color:#9ca3af;margin-left:auto;">No subido</span>';

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
        Swal.fire({
            title: "¿Finalizar requisición?",
            text: "Se marcará como finalizada y enviada a proceso de pago.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#fe6291",
            cancelButtonColor: "var(--slate-500)",
            confirmButtonText: "Sí, finalizar",
            cancelButtonText: "Cancelar"
        }).then(function (result) {
            if (!result.isConfirmed) return;

            var formData = new FormData();
            formData.append("IdRequisicion", expedienteFinActual);

            var inputFactura = document.getElementById("inputFactura");
            if (inputFactura && inputFactura.files.length) {
                for (var i = 0; i < inputFactura.files.length; i++)
                    formData.append("Factura", inputFactura.files[i]);
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
                    if (inputFactura) inputFactura.value = "";
                    Swal.fire({
                        icon: "success",
                        title: "Requisición finalizada",
                        timer: 2000,
                        showConfirmButton: false
                    }).then(function () { location.reload(); });
                },
                error: function () {
                    Swal.fire({ icon: "error", title: "Error al finalizar la requisición." });
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
                '<input type="checkbox" value="' + doc.label + '" ' +
                'style="width:16px;height:16px;flex-shrink:0;">' +
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

        $.ajax({
            url: urlRebotarDocumentos,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                IdRequisicion: expedienteFinActual,
                Observaciones: nota,
                DocumentosObservados: docsObservados
            }),
            success: function (res) {
                if (res.success) {
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalRebotar")).hide();
                    bootstrap.Modal.getInstance(
                        document.getElementById("modalExpedienteFinancieros")).hide();
                    Swal.fire({
                        icon: "success",
                        title: "Documentos regresados al analista",
                        timer: 2000,
                        showConfirmButton: false
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
        var observaciones = document.getElementById("txtObservaciones").value.trim();
        if (!observaciones) {
            Swal.fire({ icon: "warning", title: "Debe escribir una observación.", confirmButtonText: "Ok" });
            return;
        }

        var formData = new FormData();
        formData.append("IdRequisicion", requisicionActual);
        formData.append("Observaciones", observaciones);

        // Solo intentar leer archivos si los inputs existen (rol 9)
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

        $.ajax({
            url: atenderUrl,
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function () {
                bootstrap.Modal.getInstance(document.getElementById("modalDetalle")).hide();
                if (inputSiaf) inputSiaf.value = "";
                if (inputTablaApi) inputTablaApi.value = "";
                document.getElementById("txtObservaciones").value = "";
                Swal.fire({
                    icon: "success", title: "Requisición atendida",
                    timer: 2000, showConfirmButton: false
                }).then(function () { location.reload(); });
            },
            error: function () {
                Swal.fire({ icon: "error", title: "Error al atender la requisición." });
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
        if (totalVisibles === 0) {
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

    // ── Tabs ────────────────────────────────────────────────────────────────
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

    // ── PDF ─────────────────────────────────────────────────────────────────
    window.verPdf = function (id) {
        var fila = document.querySelector('tr.fila-requi[data-requi-id="' + id + '"]');
        var esServicio = fila && fila.getAttribute('data-requi-servicio') === 'true';

        var urlBase = esServicio ? verPdfServicioUrl : verPdfRequisicionUrl;
        window.open(urlBase.replace(/\/$/, "") + "/" + id, "_blank");
    };

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

})();