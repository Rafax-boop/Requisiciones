/**
 * Almacén - Tabs, filtros, paginación inventario, modal de análisis
 * Requiere: jQuery, Bootstrap, Select2 (cargados en la vista).
 * URL de API desde data-url-obtener-requisicion en .tabla-requi-page
 */
(function () {
    var container = document.querySelector('.tabla-requi-page');
    var urlObtenerRequisicion = container ? container.getAttribute('data-url-obtener-requisicion') : '';

    const tabPanels = document.querySelectorAll('.almacen-tab-panel');
    const tabBtns = document.querySelectorAll('.almacen-tabs-btn');

    tabBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const tab = this.getAttribute('data-tab');
            tabBtns.forEach(b => b.classList.remove('activo'));
            tabPanels.forEach(p => {
                p.classList.remove('activo');
                if (p.id === 'tab-' + tab) p.classList.add('activo');
            });
            this.classList.add('activo');
        });
    });

    // Filtro tabla requisiciones
    const filtroBuscar = document.getElementById('filtroBuscar');
    const filtroEstado = document.getElementById('filtroEstado');
    const tbodyReq = document.querySelector('#tablaRequisiciones tbody');
    if (filtroBuscar) {
        filtroBuscar.addEventListener('input', filtrarTabla);
    }
    if (filtroEstado) {
        filtroEstado.addEventListener('change', filtrarTabla);
    }
    // Paginación tabla requisiciones (7 por página)
    const filaRequisicionesVacia = tbodyReq ? tbodyReq.querySelector('.fila-vacia') : null;
    const paginacionRequisiciones = document.getElementById('paginacionRequisiciones');
    const TAMANO_PAGINA_REQUISICIONES = 7;
    let paginaRequisicionActual = 1;

    function getFilasRequisicionesVisibles() {
        const texto = (filtroBuscar?.value || '').toLowerCase().trim();
        const estado = (filtroEstado?.value || '').trim();
        const filas = tbodyReq ? [].slice.call(tbodyReq.querySelectorAll('tr[data-id]')) : [];

        return filas.filter(function (tr) {
            const folio = (tr.getAttribute('data-folio') || '').toLowerCase();
            const depto = (tr.getAttribute('data-depto') || '').toLowerCase();
            const est = (tr.getAttribute('data-estatus') || '');
            const matchTexto = !texto || folio.includes(texto) || depto.includes(texto) || (tr.textContent || '').toLowerCase().includes(texto);
            const matchEstado = !estado || est === estado;
            return matchTexto && matchEstado;
        });
    }

    function aplicarPaginacionRequisiciones() {
        const visibles = getFilasRequisicionesVisibles();
        const total = visibles.length;
        const totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_REQUISICIONES));
        if (paginaRequisicionActual > totalPaginas) paginaRequisicionActual = totalPaginas;
        const inicio = (paginaRequisicionActual - 1) * TAMANO_PAGINA_REQUISICIONES;
        const fin = inicio + TAMANO_PAGINA_REQUISICIONES;

        if (tbodyReq) {
            tbodyReq.querySelectorAll('tr[data-id]').forEach(tr => { tr.style.display = 'none'; });
            visibles.forEach((tr, i) => {
                tr.style.display = (i >= inicio && i < fin) ? '' : 'none';
            });
        }

        // Manejar el mensaje vacio si existiera, o añadir uno si no
        let trVacio = tbodyReq ? tbodyReq.querySelector('.fila-vacia') : null;
        if (total === 0) {
            if (!trVacio && tbodyReq) {
                trVacio = document.createElement('tr');
                trVacio.className = 'fila-vacia';
                trVacio.innerHTML = '<td colspan="8" class="text-center">No hay requisiciones</td>';
                tbodyReq.appendChild(trVacio);
            } else if (trVacio) {
                trVacio.style.display = '';
            }
        } else if (trVacio) {
            trVacio.style.display = 'none';
        }

        if (paginacionRequisiciones) {
            if (total === 0) {
                paginacionRequisiciones.innerHTML = '';
                return;
            }
            var info = 'Mostrando ' + (inicio + 1) + '-' + Math.min(fin, total) + ' de ' + total + ' requisiciones';
            var html = '<div class="almacen-paginacion-info">' + info + '</div>';
            html += '<div class="almacen-paginacion-btns">';
            html += '<button type="button" class="almacen-paginacion-btn" data-pagina="prev" ' + (paginaRequisicionActual <= 1 ? 'disabled' : '') + '>Anterior</button>';
            html += ' <span class="almacen-paginacion-nums">';
            var PRIMEROS = 3, ULTIMOS = 3, totalPag = totalPaginas;
            var actual = paginaRequisicionActual;
            var set = {};
            for (var i = 1; i <= Math.min(PRIMEROS, totalPag); i++) set[i] = true;
            if (actual > 0 && actual <= totalPag) {
                set[actual] = true;
                if (actual - 1 >= 1) set[actual - 1] = true;
                if (actual + 1 <= totalPag) set[actual + 1] = true;
            }
            for (var j = Math.max(1, totalPag - ULTIMOS + 1); j <= totalPag; j++) set[j] = true;
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
            paginacionRequisiciones.innerHTML = html;

            paginacionRequisiciones.querySelectorAll('.almacen-paginacion-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    if (this.disabled) return;
                    var pg = this.getAttribute('data-pagina');
                    if (pg === 'prev') paginaRequisicionActual = Math.max(1, paginaRequisicionActual - 1);
                    else if (pg === 'next') paginaRequisicionActual = Math.min(totalPaginas, paginaRequisicionActual + 1);
                    else paginaRequisicionActual = parseInt(pg, 10);
                    aplicarPaginacionRequisiciones();
                });
            });
        }
    }

    function filtrarTabla() {
        paginaRequisicionActual = 1;
        aplicarPaginacionRequisiciones();
    }

    // Inicializar paginación de requisiciones
    aplicarPaginacionRequisiciones();

    // Filtro tabla inventario por unidad de medida + paginación (15 por página)
    const filtroUnidadMedida = document.getElementById('filtroUnidadMedida');
    const tbodyInv = document.querySelector('#tablaInventario tbody');
    const filaInventarioVacio = document.getElementById('filaInventarioVacio');
    const paginacionInventario = document.getElementById('paginacionInventario');
    const TAMANO_PAGINA_INVENTARIO = 15;
    let paginaInventarioActual = 1;

    function getFilasInventarioVisibles() {
        var unidad = ($('#filtroUnidadMedida').val() || '').toString().trim();
        const filas = tbodyInv ? [].slice.call(tbodyInv.querySelectorAll('tr[data-unidad]')) : [];
        return unidad
            ? filas.filter(function (tr) { return (tr.getAttribute('data-unidad') || '').trim() === unidad; })
            : filas;
    }

    function aplicarPaginacionInventario() {
        const visibles = getFilasInventarioVisibles();
        const total = visibles.length;
        const totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_INVENTARIO));
        if (paginaInventarioActual > totalPaginas) paginaInventarioActual = totalPaginas;
        const inicio = (paginaInventarioActual - 1) * TAMANO_PAGINA_INVENTARIO;
        const fin = inicio + TAMANO_PAGINA_INVENTARIO;

        if (tbodyInv) {
            tbodyInv.querySelectorAll('tr[data-unidad]').forEach(tr => { tr.style.display = 'none'; });
            visibles.forEach((tr, i) => {
                tr.style.display = (i >= inicio && i < fin) ? '' : 'none';
            });
        }
        if (filaInventarioVacio) {
            filaInventarioVacio.style.display = total === 0 ? '' : 'none';
        }

        if (paginacionInventario) {
            if (total === 0) {
                paginacionInventario.innerHTML = '';
                return;
            }
            var info = 'Mostrando ' + (inicio + 1) + '-' + Math.min(fin, total) + ' de ' + total + ' materiales';
            var html = '<div class="almacen-paginacion-info">' + info + '</div>';
            html += '<div class="almacen-paginacion-btns">';
            html += '<button type="button" class="almacen-paginacion-btn" data-pagina="prev" ' + (paginaInventarioActual <= 1 ? 'disabled' : '') + '>Anterior</button>';
            html += ' <span class="almacen-paginacion-nums">';
            var PRIMEROS = 3, ULTIMOS = 3, totalPag = totalPaginas;
            var actual = paginaInventarioActual;
            var set = {};
            for (var i = 1; i <= Math.min(PRIMEROS, totalPag); i++) set[i] = true;
            if (actual > 0 && actual <= totalPag) {
                set[actual] = true;
                if (actual - 1 >= 1) set[actual - 1] = true;
                if (actual + 1 <= totalPag) set[actual + 1] = true;
            }
            for (var j = Math.max(1, totalPag - ULTIMOS + 1); j <= totalPag; j++) set[j] = true;
            var nums = Object.keys(set).map(Number).sort(function (a, b) { return a - b; });
            var prev = 0;
            for (var n = 0; n < nums.length; n++) {
                var p = nums[n];
                if (prev !== 0 && p > prev + 1) html += '<span class="almacen-paginacion-ellipsis">…</span>';
                html += '<button type="button" class="almacen-paginacion-btn almacen-paginacion-num ' + (p === paginaInventarioActual ? 'activo' : '') + '" data-pagina="' + p + '">' + p + '</button>';
                prev = p;
            }
            html += '</span> ';
            html += '<button type="button" class="almacen-paginacion-btn" data-pagina="next" ' + (paginaInventarioActual >= totalPaginas ? 'disabled' : '') + '>Siguiente</button>';
            html += '</div>';
            paginacionInventario.innerHTML = html;

            paginacionInventario.querySelectorAll('.almacen-paginacion-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    if (this.disabled) return;
                    var pg = this.getAttribute('data-pagina');
                    if (pg === 'prev') paginaInventarioActual = Math.max(1, paginaInventarioActual - 1);
                    else if (pg === 'next') paginaInventarioActual = Math.min(totalPaginas, paginaInventarioActual + 1);
                    else paginaInventarioActual = parseInt(pg, 10);
                    aplicarPaginacionInventario();
                });
            });
        }
    }

    function filtrarTablaInventario() {
        paginaInventarioActual = 1;
        aplicarPaginacionInventario();
    }

    aplicarPaginacionInventario();

    // Modal: tabs internos
    const modalTabsBtns = document.querySelectorAll('.almacen-modal-tabs-btn');
    const modalBody = document.getElementById('modalAlmacenBody');
    let reqCompleta = null;

    modalTabsBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            if (!reqCompleta) return;
            modalTabsBtns.forEach(b => b.classList.remove('activo'));
            this.classList.add('activo');
            const i = parseInt(this.getAttribute('data-modal-tab'), 10);
            renderModalTab(i);
        });
    });

    function renderModalTab(i) {
        if (!reqCompleta || !modalBody) return;
        if (i === 0) {
            const articulos = reqCompleta.articulos || [];
            let html = '<div class="almacen-resumen-cards"><div class="almacen-resumen-card"><span>Total Items</span><strong>' + articulos.length + '</strong></div></div>';
            html += '<table class="tabla-requisiciones"><thead><tr><th>Material</th><th>Cantidad</th><th>Unidad</th><th>Descripción</th></tr></thead><tbody>';
            articulos.forEach(a => {
                html += '<tr><td>' + (a.descripcion || '') + '</td><td>' + (a.cantidad ?? '') + '</td><td>' + (a.unidadMedida || '') + '</td><td>' + (a.descripcionDetallada || '') + '</td></tr>';
            });
            html += '</tbody></table>';
            modalBody.innerHTML = html;
        } else if (i === 1) {
            const r = reqCompleta;
            let html = '<div class="almacen-grid2"><div><h4 style="margin-top:0">Información del Solicitante</h4>';
            html += '<div class="almacen-campo"><label>Departamento</label><input type="text" disabled value="' + (r.departamento || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Responsable</label><input type="text" disabled value="' + (r.nomResponsableDepartamento || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Correo</label><input type="text" disabled value="' + (r.correo || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Teléfono</label><input type="text" disabled value="' + (r.telefono || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Lugar de entrega</label><input type="text" disabled value="' + (r.lugarEntrega || '') + '" /></div>';
            html += '</div><div><h4 style="margin-top:0">Justificación</h4>';
            html += '<div class="almacen-campo"><label>Uso Específico</label><textarea rows="3" disabled>' + (r.usoEspecifico || '') + '</textarea></div>';
            html += '<div class="almacen-campo"><label>Justificación</label><textarea rows="5" disabled>' + (r.justificacion || '') + '</textarea></div>';
            html += '</div></div>';
            modalBody.innerHTML = html;
        } else {
            modalBody.innerHTML = '<div class="almacen-alerta">Las observaciones son obligatorias para rechazar una requisición.</div><div class="almacen-campo"><label>Observaciones del Almacén</label><textarea id="obs-textarea" rows="6" placeholder="Escribe observaciones..."></textarea></div>';
        }
    }

    window.abrirModal = function (id, folio, departamento, estatus) {
        document.getElementById('modalAlmacenTitulo').textContent = (folio || '') + ' — ' + (departamento || '');
        document.getElementById('modalAlmacenSubtitulo').textContent = 'Estado: ' + (estatus || '');
        modalBody.innerHTML = '<p class="text-muted">Cargando...</p>';
        reqCompleta = null;
        const modal = new bootstrap.Modal(document.getElementById('modalAlmacen'));
        modal.show();

        var url = (urlObtenerRequisicion || '').replace(/\/$/, '') + '?id=' + id;
        fetch(url)
            .then(res => res.ok ? res.json() : Promise.reject())
            .then(data => {
                reqCompleta = data;
                document.querySelectorAll('.almacen-modal-tabs-btn').forEach(b => b.classList.remove('activo'));
                document.querySelector('.almacen-modal-tabs-btn[data-modal-tab="0"]').classList.add('activo');
                renderModalTab(0);
            })
            .catch(() => {
                modalBody.innerHTML = '<p class="text-danger">No se pudo cargar la requisición.</p>';
            });
    };

    document.body.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-almacen-analizar]');
        if (btn) {
            e.preventDefault();
            const id = parseInt(btn.getAttribute('data-id'), 10);
            abrirModal(id, btn.getAttribute('data-folio') || '', btn.getAttribute('data-depto') || '', btn.getAttribute('data-estatus') || '');
        }
    });

    // Select2 para filtros Estado e Inventario (Unidad de medida)
    $(function () {
        $('#filtroEstado').select2({
            width: '100%',
            language: 'es',
            minimumResultsForSearch: Infinity,
            placeholder: 'Todos los estados'
        });
        $('#filtroUnidadMedida').select2({
            width: '100%',
            language: 'es',
            minimumResultsForSearch: Infinity,
            placeholder: 'Todas las unidades',
            allowClear: true
        });
        $('#filtroUnidadMedida').on('change select2:select', function () {
            filtrarTablaInventario();
        });
    });
})();
