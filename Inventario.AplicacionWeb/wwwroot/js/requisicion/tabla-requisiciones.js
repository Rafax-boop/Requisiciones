/**
 * Tabla de Requisiciones - Filtros (Flatpickr), modal de detalle, exportar PDF
 * Requiere: jQuery, Bootstrap, Flatpickr (cargados en layout/vista).
 * URLs desde data-url-obtener-detalles y data-url-ver-pdf en .tabla-requi-page
 */
(function () {
    var container = document.querySelector('.tabla-requi-page');
    var obtenerDetallesUrl = container ? container.getAttribute('data-url-obtener-detalles') : '';
    var verPdfUrl = container ? container.getAttribute('data-url-ver-pdf') : '';

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

    function filtrarTabla() {
        var textoNumReq = (document.getElementById('filtroNumReq') && document.getElementById('filtroNumReq').value || '').toLowerCase().trim();
        var textoDepto = (document.getElementById('filtroDepartamento') && document.getElementById('filtroDepartamento').value || '').toLowerCase().trim();

        var filas = document.querySelectorAll('.tabla-requisiciones:not(#tablaModalDetalle) tbody tr');

        filas.forEach(function (fila) {
            if (fila.classList.contains('fila-vacia')) return;
            var celdas = fila.querySelectorAll('td');
            if (!celdas.length) return;

            var folio = (celdas[0] && celdas[0].textContent.toLowerCase()) || '';
            var fecha = (celdas[1] && celdas[1].textContent.trim()) || '';
            var depto = (celdas[2] && celdas[2].textContent.toLowerCase()) || '';

            var pasaNumReq = !textoNumReq || folio.indexOf(textoNumReq) !== -1;
            var pasaFecha = !fechaSeleccionada || fecha === fechaSeleccionada;
            var pasaDepto = !textoDepto || depto.indexOf(textoDepto) !== -1;

            fila.style.display = (pasaNumReq && pasaFecha && pasaDepto) ? '' : 'none';
        });

        mostrarMensajeVacio();
    }

    function mostrarMensajeVacio() {
        var tbody = document.querySelector('.tabla-requisiciones:not(#tablaModalDetalle) tbody');
        if (!tbody) return;
        var filasVisibles = [].slice.call(tbody.querySelectorAll('tr')).filter(function (f) {
            return f.style.display !== 'none' && !f.classList.contains('fila-vacia');
        });

        var filaVacia = tbody.querySelector('.fila-vacia');
        if (filasVisibles.length === 0) {
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
