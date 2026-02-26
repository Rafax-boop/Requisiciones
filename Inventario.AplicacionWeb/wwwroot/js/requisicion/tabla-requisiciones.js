/**
 * Tabla de Requisiciones - Filtros (Flatpickr), modal de detalle, exportar PDF
 * Requiere: jQuery, Bootstrap, Flatpickr (cargados en layout/vista).
 * URLs desde data-url-obtener-detalles y data-url-ver-pdf en .tabla-requi-page
 */
(function () {
    var container = document.querySelector('.tabla-requi-page');
    var obtenerDetallesUrl = container ? container.getAttribute('data-url-obtener-detalles') : '';
    var verPdfUrl = container ? container.getAttribute('data-url-ver-pdf') : '';
    var urlUsuariosMateriales = container ? container.getAttribute('data-url-usuarios-materiales') : '';

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.filtro-dropdown')) {
            document.querySelectorAll('.filtro-dropdown').forEach(d => d.classList.remove('open'));
        }
    });

    var fechaSeleccionada = '';

    var fpInstance = flatpickr('#filtroFecha', {
        locale: 'es',
        dateFormat: 'd/m/Y',
        allowInput: false,
        disableMobile: true,
        onChange: function (selectedDates, dateStr) {
            fechaSeleccionada = dateStr;
            var btnLimpiar = document.getElementById('btnLimpiarFecha');
            if (btnLimpiar) btnLimpiar.style.display = dateStr ? 'inline' : 'none';
            filtrarTabla();
        }
    });

    window.limpiarFecha = function () {
        fpInstance.clear();
        fechaSeleccionada = '';
        var btnLimpiar = document.getElementById('btnLimpiarFecha');
        if (btnLimpiar) btnLimpiar.style.display = 'none';
        filtrarTabla();
    };

    const TAMANO_PAGINA_REQUISICIONES = 7;
    let paginaRequisicionActual = 1;
    let paginacionRequisicionesContainer = null;

    function filtrarTabla() {
        paginaRequisicionActual = 1;
        aplicarPaginacionRequisiciones();
    }

    function aplicarPaginacionRequisiciones() {
        if (!paginacionRequisicionesContainer) {
            paginacionRequisicionesContainer = document.getElementById('paginacionRequisiciones');
        }

        var textoNumReq = (document.getElementById('filtroNumReq') && document.getElementById('filtroNumReq').value || '').toLowerCase().trim();
        var textoDepto = (document.getElementById('filtroDepartamento') && document.getElementById('filtroDepartamento').value || '').toLowerCase().trim();

        var todasLasFilas = document.querySelectorAll('.tabla-requisiciones:not(#tablaModalDetalle) tbody tr');
        var filasVisibles = [];

        todasLasFilas.forEach(function (fila) {
            if (fila.classList.contains('fila-vacia')) return;
            var celdas = fila.querySelectorAll('td');
            if (!celdas.length) return;

            var folio = (celdas[0] && celdas[0].textContent.toLowerCase()) || '';
            var fecha = (celdas[1] && celdas[1].textContent.trim()) || '';
            var depto = (celdas[2] && celdas[2].textContent.toLowerCase()) || '';

            var pasaNumReq = !textoNumReq || folio.indexOf(textoNumReq) !== -1;
            var pasaFecha = !fechaSeleccionada || fecha === fechaSeleccionada;
            var pasaDepto = !textoDepto || depto.indexOf(textoDepto) !== -1;

            if (pasaNumReq && pasaFecha && pasaDepto) {
                filasVisibles.push(fila);
            } else {
                fila.style.display = 'none';
            }
        });

        const total = filasVisibles.length;
        const totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_REQUISICIONES));
        if (paginaRequisicionActual > totalPaginas) paginaRequisicionActual = totalPaginas;
        const inicio = (paginaRequisicionActual - 1) * TAMANO_PAGINA_REQUISICIONES;
        const fin = inicio + TAMANO_PAGINA_REQUISICIONES;

        filasVisibles.forEach(function (fila, i) {
            fila.style.display = (i >= inicio && i < fin) ? '' : 'none';
        });

        mostrarMensajeVacio(total);
        renderizarControlesPaginacion(total, inicio, fin, totalPaginas);
    }

    function renderizarControlesPaginacion(total, inicio, fin, totalPaginas) {
        if (!paginacionRequisicionesContainer) return;

        if (total === 0) {
            paginacionRequisicionesContainer.innerHTML = '';
            return;
        }

        var resFinal = Math.min(fin, total);
        var info = 'Mostrando ' + (inicio + 1) + '-' + resFinal + ' de ' + total + ' requisiciones';
        var html = '<div class="almacen-paginacion-info">' + info + '</div>';
        html += '<div class="almacen-paginacion-btns">';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="prev" ' + (paginaRequisicionActual <= 1 ? 'disabled' : '') + '>Anterior</button>';
        html += ' <span class="almacen-paginacion-nums">';

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
            html += '<button type="button" class="almacen-paginacion-btn almacen-paginacion-num ' + (p === paginaRequisicionActual ? 'activo' : '') + '" data-pagina="' + p + '">' + p + '</button>';
            prev = p;
        }
        html += '</span> ';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="next" ' + (paginaRequisicionActual >= totalPaginas ? 'disabled' : '') + '>Siguiente</button>';
        html += '</div>';

        paginacionRequisicionesContainer.innerHTML = html;

        var botones = paginacionRequisicionesContainer.querySelectorAll('.almacen-paginacion-btn');
        for (var k = 0; k < botones.length; k++) {
            botones[k].addEventListener('click', function () {
                if (this.disabled) return;
                var pg = this.getAttribute('data-pagina');
                if (pg === 'prev') {
                    paginaRequisicionActual = Math.max(1, paginaRequisicionActual - 1);
                } else if (pg === 'next') {
                    paginaRequisicionActual = Math.min(totalPaginas, paginaRequisicionActual + 1);
                } else {
                    paginaRequisicionActual = parseInt(pg, 10);
                }
                aplicarPaginacionRequisiciones();
            });
        }
    }

    window.abrirModalAsignar = function (idRequi) {
        _idRequiAsignar = idRequi;
        var select = document.getElementById('selectUsuarioAsignar');
        select.innerHTML = '<option value="">Cargando...</option>';

        $.get(urlUsuariosMateriales, function (data) {
            select.innerHTML = '<option value="">-- Seleccionar responsable --</option>';
            data.forEach(function (u) {
                select.innerHTML += '<option value="' + u.id + '">' + u.nombre + '</option>';
            });
        });

        var modal = new bootstrap.Modal(document.getElementById('modalAsignar'));
        modal.show();
    };

    function mostrarMensajeVacio(totalVisibles) {
        var tbody = document.querySelector('.tabla-requisiciones:not(#tablaModalDetalle) tbody');
        if (!tbody) return;

        var filaVacia = tbody.querySelector('.fila-vacia');
        if (totalVisibles === 0) {
            if (!filaVacia) {
                filaVacia = document.createElement('tr');
                filaVacia.className = 'fila-vacia';
                filaVacia.innerHTML = '<td colspan="6" class="text-center">Sin resultados para los filtros aplicados</td>';
                tbody.appendChild(filaVacia);
            }
        } else {
            if (filaVacia) filaVacia.remove();
        }
    }

    // Inicializar paginación al cargar el script
    aplicarPaginacionRequisiciones();

    var filtroNumReq = document.getElementById('filtroNumReq');
    var filtroDepto = document.getElementById('filtroDepartamento');
    if (filtroNumReq) filtroNumReq.addEventListener('input', filtrarTabla);
    if (filtroDepto) filtroDepto.addEventListener('input', filtrarTabla);

    window.verPdf = function (id) {
        var url = (verPdfUrl || '').replace(/\/$/, '') + '/' + id;
        window.open(url, '_blank');
    };

    var _descPanelModalTrigger = null;

    window.verDescDetalleModal = function (el) {
        var panel = document.getElementById('desc-panel-modal');
        var textarea = document.getElementById('desc-textarea-modal');
        if (!panel || !textarea) return;
        textarea.value = el.dataset.full || '(Sin descripción detallada)';
        var rect = el.getBoundingClientRect();
        panel.style.top = (rect.bottom + 4) + 'px';
        panel.style.left = rect.left + 'px';
        panel.style.minWidth = Math.max(rect.width, 320) + 'px';
        panel.style.display = 'block';
        _descPanelModalTrigger = el;
        textarea.focus();
    };

    window.cerrarDescPanelModal = function () {
        var panel = document.getElementById('desc-panel-modal');
        if (panel) panel.style.display = 'none';
        _descPanelModalTrigger = null;
    };

    $(document).on('mousedown', function (e) {
        if (!_descPanelModalTrigger) return;
        var panel = document.getElementById('desc-panel-modal');
        if (!_descPanelModalTrigger.contains(e.target) && panel && !panel.contains(e.target)) {
            cerrarDescPanelModal();
        }
    });

    window.verDetalle = function (idMaestro) {
        if (!obtenerDetallesUrl) return;
        $.get(obtenerDetallesUrl, { idMaestro: idMaestro }, function (data) {
            var contenido = '';
            var articulos = data.articulos || [];

            if (articulos.length === 0) {
                contenido = '<tr><td colspan="6" class="text-center">Sin artículos</td></tr>';
            } else {
                articulos.forEach(function (item) {
                    var textoCompleto = item.descripcionDetallada || '';
                    var textoCorto = textoCompleto.length > 28
                        ? textoCompleto.substring(0, 28) + '…'
                        : (textoCompleto || 'Sin descripción...');
                    var tieneTexto = textoCompleto ? 'tiene-texto' : '';
                    var fullEscapado = (textoCompleto || '').replace(/"/g, '&quot;');

                    contenido += '<tr>' +
                        '<td>' + (item.numPartida || '') + '</td>' +
                        '<td>' + (item.idArticulo || '') + '</td>' +
                        '<td>' + (item.cantidad || '') + '</td>' +
                        '<td>' + (item.unidadMedida || '') + '</td>' +
                        '<td>' + (item.descripcion || '') + '</td>' +
                        '<td><div class="desc-preview-modal" data-full="' + fullEscapado + '" onclick="verDescDetalleModal(this)">' +
                        '<span class="desc-texto-preview ' + tieneTexto + '">' + textoCorto + '</span>' +
                        '<i class="fa-solid fa-eye desc-icon"></i></div></td>' +
                        '</tr>';
                });
            }

            $('#tablaDetalle').html(contenido);
            var modalEl = document.getElementById('modalDetalle');
            if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                var modal = new bootstrap.Modal(modalEl);
                modal.show();
            } else if ($.fn.modal) {
                $('#modalDetalle').modal('show');
            }
        });
    };
})();
