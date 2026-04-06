(function () {
    var container = document.querySelector('.tabla-requi-page');
    var urlObtenerRequisicion = container ? container.getAttribute('data-url-obtener-requisicion') : '';
    var urlAprobarCompleta = container ? container.getAttribute('data-url-aprobar-completa') : '';
    var urlAprobarParcial = container ? container.getAttribute('data-url-aprobar-parcial') : '';
    var urlRechazar = container ? container.getAttribute('data-url-rechazar') : '';
    var urlRegistrarIngreso = container ? container.getAttribute('data-url-registrar-ingreso') : '';
    var urlConsultarStock = container ? container.getAttribute('data-url-consultar-stock') : '';
    var urlProcesarRequisicion = container ? container.getAttribute('data-url-procesar-requisicion') : '';
    var urlConfirmarEntrega = container ? container.getAttribute('data-url-confirmar-entrega') : '';

    /* ========== HELPERS ========== */

    function postJson(url, body) {
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body || {})
        }).then(async function (r) {
            var data = await r.json().catch(function () { return {}; });
            if (!r.ok) {
                return Promise.reject(data && data.error ? data.error : 'Error en la petición.');
            }
            return data;
        });
    }

    function swalError(msg) {
        Swal.fire({ icon: 'error', title: 'Error', text: msg || 'Ocurrió un error.', confirmButtonColor: '#e11d48' });
    }

    function swalExito(msg) {
        return Swal.fire({ icon: 'success', title: '¡Listo!', text: msg || 'Operación exitosa.', confirmButtonColor: '#e11d48' });
    }

    function swalWarning(msg) {
        Swal.fire({ icon: 'warning', title: 'Atención', text: msg, confirmButtonColor: '#e11d48' });
    }

    function swalConfirmar(titulo, texto, botonTexto) {
        return Swal.fire({
            title: titulo,
            text: texto,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#e11d48',
            cancelButtonColor: '#64748b',
            confirmButtonText: botonTexto || 'Sí, continuar',
            cancelButtonText: 'Cancelar'
        });
    }

    /* ========== TABS PRINCIPALES ========== */

    var tabPanels = document.querySelectorAll('.almacen-tab-panel');
    var tabBtns = document.querySelectorAll('.almacen-tabs-btn');

    tabBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var tab = this.getAttribute('data-tab');
            
            // Al salir de un tab activo (antes de cambiarlo), limpiamos sus filas nuevas
            var panelActivoAnterior = document.querySelector(".almacen-tab-panel.activo");
            if (panelActivoAnterior && panelActivoAnterior.id !== "tab-" + tab) {
                var filasVistas = panelActivoAnterior.querySelectorAll("tr.fila-nueva");
                filasVistas.forEach(function(f) { f.classList.remove("fila-nueva"); });
            }
            
            // Reiniciamos el badge del tab destino
            var badgeDestino = this.querySelector(".badge-almacen-tab");
            if (badgeDestino) {
                badgeDestino.textContent = "0";
                badgeDestino.style.display = "none";
            }

            tabBtns.forEach(function (b) { b.classList.remove('activo'); });
            tabPanels.forEach(function (p) {
                p.classList.remove('activo');
                if (p.id === 'tab-' + tab) p.classList.add('activo');
            });
            this.classList.add('activo');
        });
    });

    // --- LÓGICA DE NOTIFICACIONES EN TIEMPO REAL ---
    window.recibirNotificacionRequi = function (idRequi, tabDestino) {
        // En almacén la fila es tr[data-id]
        var fila = document.querySelector('tr[data-id="' + idRequi + '"]');
        if (fila) {
            fila.classList.add("fila-nueva");
        }

        var panelDestino = document.getElementById("tab-" + tabDestino);
        if (panelDestino && !panelDestino.classList.contains("activo")) {
            var btnTab = document.querySelector('.almacen-tabs-btn[data-tab="' + tabDestino + '"]');
            if (btnTab) {
                var badge = btnTab.querySelector(".badge-almacen-tab");
                if (badge) {
                    var conteoActual = parseInt(badge.textContent || "0", 10);
                    badge.textContent = conteoActual + 1;
                    badge.style.display = "flex";
                    
                    badge.style.animation = 'none';
                    badge.offsetHeight; /* trigger reflow */
                    badge.style.animation = null; 
                }
            }
        }
    };

    /* ========== FILTROS + PAGINACIÓN REQUISICIONES ========== */

    var filtroBuscar = document.getElementById('filtroBuscar');
    var filtroEstado = document.getElementById('filtroEstado');
    var tbodyReq = document.querySelector('#tablaRequisiciones tbody');
    var paginacionRequisiciones = document.getElementById('paginacionRequisiciones');
    var TAMANO_PAGINA_REQUISICIONES = 7;
    var paginaRequisicionActual = 1;

    if (filtroBuscar) filtroBuscar.addEventListener('input', filtrarTabla);
    if (filtroEstado) filtroEstado.addEventListener('change', filtrarTabla);

    function getFilasRequisicionesVisibles() {
        var texto = (filtroBuscar ? filtroBuscar.value : '').toLowerCase().trim();
        var estado = (filtroEstado ? filtroEstado.value : '').trim();
        var filas = tbodyReq ? [].slice.call(tbodyReq.querySelectorAll('tr[data-id]')) : [];
        return filas.filter(function (tr) {
            var folio = (tr.getAttribute('data-folio') || '').toLowerCase();
            var depto = (tr.getAttribute('data-depto') || '').toLowerCase();
            var est = (tr.getAttribute('data-estatus') || '');
            var matchTexto = !texto || folio.includes(texto) || depto.includes(texto) || (tr.textContent || '').toLowerCase().includes(texto);
            var matchEstado = !estado || est === estado;
            return matchTexto && matchEstado;
        });
    }

    function aplicarPaginacionRequisiciones() {
        var visibles = getFilasRequisicionesVisibles();
        var total = visibles.length;
        var totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_REQUISICIONES));
        if (paginaRequisicionActual > totalPaginas) paginaRequisicionActual = totalPaginas;
        var inicio = (paginaRequisicionActual - 1) * TAMANO_PAGINA_REQUISICIONES;
        var fin = inicio + TAMANO_PAGINA_REQUISICIONES;

        if (tbodyReq) {
            tbodyReq.querySelectorAll('tr[data-id]').forEach(function (tr) { tr.style.display = 'none'; });
            visibles.forEach(function (tr, i) {
                tr.style.display = (i >= inicio && i < fin) ? '' : 'none';
            });
        }

        var trVacio = tbodyReq ? tbodyReq.querySelector('.fila-vacia') : null;
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

        renderPaginacion(paginacionRequisiciones, total, inicio, fin, totalPaginas, paginaRequisicionActual,
            function (p) { paginaRequisicionActual = p; aplicarPaginacionRequisiciones(); }, 'requisiciones');
    }

    function filtrarTabla() {
        paginaRequisicionActual = 1;
        aplicarPaginacionRequisiciones();
    }

    aplicarPaginacionRequisiciones();

    /* ========== FILTROS + PAGINACIÓN ENTREGAS ========== */

    var filtroBuscarEntregas = document.getElementById('filtroBuscarEntregas');
    var tbodyEntregas = document.querySelector('#tablaEntregas tbody');
    var paginacionEntregas = document.getElementById('paginacionEntregas');
    var TAMANO_PAGINA_ENTREGAS = 7;
    var paginaEntregasActual = 1;

    if (filtroBuscarEntregas) filtroBuscarEntregas.addEventListener('input', filtrarTablaEntregas);

    function getFilasEntregasVisibles() {
        var texto = (filtroBuscarEntregas ? filtroBuscarEntregas.value : '').toLowerCase().trim();
        var filas = tbodyEntregas ? [].slice.call(tbodyEntregas.querySelectorAll('tr[data-requi]')) : [];
        return filas.filter(function (tr) {
            var folio = (tr.getAttribute('data-folio') || '').toLowerCase();
            var depto = (tr.getAttribute('data-depto') || '').toLowerCase();
            return !texto || folio.includes(texto) || depto.includes(texto);
        });
    }

    function aplicarPaginacionEntregas() {
        var visibles = getFilasEntregasVisibles();
        var total = visibles.length;
        var totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_ENTREGAS));
        if (paginaEntregasActual > totalPaginas) paginaEntregasActual = totalPaginas;
        var inicio = (paginaEntregasActual - 1) * TAMANO_PAGINA_ENTREGAS;
        var fin = inicio + TAMANO_PAGINA_ENTREGAS;

        if (tbodyEntregas) {
            tbodyEntregas.querySelectorAll('tr[data-requi]').forEach(function (tr) { tr.style.display = 'none'; });
            visibles.forEach(function (tr, i) {
                tr.style.display = (i >= inicio && i < fin) ? '' : 'none';
            });
        }

        renderPaginacion(paginacionEntregas, total, inicio, fin, totalPaginas, paginaEntregasActual,
            function (p) { paginaEntregasActual = p; aplicarPaginacionEntregas(); }, 'artículos');
    }

    function filtrarTablaEntregas() {
        paginaEntregasActual = 1;
        aplicarPaginacionEntregas();
    }

    aplicarPaginacionEntregas();

    /* ========== FILTROS + PAGINACIÓN INVENTARIO ========== */

    var tbodyInv = document.querySelector('#tablaInventario tbody');
    var filaInventarioVacio = document.getElementById('filaInventarioVacio');
    var paginacionInventario = document.getElementById('paginacionInventario');
    var filtroBuscarInventario = document.getElementById('filtroBuscarInventario');
    var TAMANO_PAGINA_INVENTARIO = 15;
    var paginaInventarioActual = 1;

    if (filtroBuscarInventario) filtroBuscarInventario.addEventListener('input', filtrarTablaInventario);

    function getFilasInventarioVisibles() {
        var unidad = ($('#filtroUnidadMedida').val() || '').toString().trim();
        var texto = (filtroBuscarInventario ? filtroBuscarInventario.value : '').toLowerCase().trim();
        var filas = tbodyInv ? [].slice.call(tbodyInv.querySelectorAll('tr[data-unidad]')) : [];
        return filas.filter(function (tr) {
            var matchUnidad = !unidad || (tr.getAttribute('data-unidad') || '').trim() === unidad;
            var matchTexto = !texto || (tr.textContent || '').toLowerCase().includes(texto);
            return matchUnidad && matchTexto;
        });
    }

    function aplicarPaginacionInventario() {
        var visibles = getFilasInventarioVisibles();
        var total = visibles.length;
        var totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA_INVENTARIO));
        if (paginaInventarioActual > totalPaginas) paginaInventarioActual = totalPaginas;
        var inicio = (paginaInventarioActual - 1) * TAMANO_PAGINA_INVENTARIO;
        var fin = inicio + TAMANO_PAGINA_INVENTARIO;

        if (tbodyInv) {
            tbodyInv.querySelectorAll('tr[data-unidad]').forEach(function (tr) { tr.style.display = 'none'; });
            visibles.forEach(function (tr, i) { tr.style.display = (i >= inicio && i < fin) ? '' : 'none'; });
        }
        if (filaInventarioVacio) filaInventarioVacio.style.display = total === 0 ? '' : 'none';

        renderPaginacion(paginacionInventario, total, inicio, fin, totalPaginas, paginaInventarioActual,
            function (p) { paginaInventarioActual = p; aplicarPaginacionInventario(); }, 'materiales');
    }

    function filtrarTablaInventario() {
        paginaInventarioActual = 1;
        aplicarPaginacionInventario();
    }

    aplicarPaginacionInventario();

    /* ========== HELPER PAGINACIÓN REUTILIZABLE ========== */

    function renderPaginacion(contenedor, total, inicio, fin, totalPaginas, actual, onCambio, etiqueta) {
        if (!contenedor) return;
        if (total === 0) { contenedor.innerHTML = ''; return; }

        var info = 'Mostrando ' + (inicio + 1) + '-' + Math.min(fin, total) + ' de ' + total + ' ' + etiqueta;
        var html = '<div class="almacen-paginacion-info">' + info + '</div>';
        html += '<div class="almacen-paginacion-btns">';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="prev" ' + (actual <= 1 ? 'disabled' : '') + '>Anterior</button>';
        html += ' <span class="almacen-paginacion-nums">';

        var PRIMEROS = 3, ULTIMOS = 3;
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
            html += '<button type="button" class="almacen-paginacion-btn almacen-paginacion-num ' + (p === actual ? 'activo' : '') + '" data-pagina="' + p + '">' + p + '</button>';
            prev = p;
        }

        html += '</span> ';
        html += '<button type="button" class="almacen-paginacion-btn" data-pagina="next" ' + (actual >= totalPaginas ? 'disabled' : '') + '>Siguiente</button>';
        html += '</div>';
        contenedor.innerHTML = html;

        contenedor.querySelectorAll('.almacen-paginacion-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (this.disabled) return;
                var pg = this.getAttribute('data-pagina');
                var nueva = actual;
                if (pg === 'prev') nueva = Math.max(1, actual - 1);
                else if (pg === 'next') nueva = Math.min(totalPaginas, actual + 1);
                else nueva = parseInt(pg, 10);
                onCambio(nueva);
            });
        });
    }

    /* ========== MODAL ANÁLISIS: TABS INTERNOS + DESCRIPCIÓN ========== */

    var modalTabsBtns = document.querySelectorAll('.almacen-modal-tabs-btn');
    var modalBody = document.getElementById('modalAlmacenBody');
    var reqCompleta = null;
    var reqActualId = null;
    var stockData = {};

    function obtenerArticulosDeReq(req) {
        if (!req) return [];
        var list = req.articulos != null ? req.articulos : req.Articulos;
        return Array.isArray(list) ? list : [];
    }

    function idDetalleArticulo(a) {
        if (!a) return 0;
        var id = a.idRequisicionDetalle != null ? a.idRequisicionDetalle : a.IdRequisicionDetalle;
        var n = Number(id);
        return isNaN(n) ? 0 : n;
    }

    function parsearRespuestaStock(json) {
        if (json == null) return [];
        if (Array.isArray(json)) return json;
        return [];
    }

    function normalizarFilaStock(s) {
        var id = s.idRequisicionDetalle != null ? s.idRequisicionDetalle : s.IdRequisicionDetalle;
        var n = Number(id);
        if (isNaN(n) || n <= 0) return null;
        var disp = s.stockDisponible != null ? s.stockDisponible : s.StockDisponible;
        var existe = s.existeEnInventario != null ? s.existeEnInventario : s.ExisteEnInventario;
        return {
            idRequisicionDetalle: n,
            descripcion: s.descripcion != null ? s.descripcion : (s.Descripcion || ''),
            unidadMedida: s.unidadMedida != null ? s.unidadMedida : (s.UnidadMedida || ''),
            cantidadSolicitada: s.cantidadSolicitada != null ? s.cantidadSolicitada : (s.CantidadSolicitada != null ? s.CantidadSolicitada : 0),
            stockDisponible: disp != null ? Number(disp) : 0,
            existeEnInventario: !!existe
        };
    }

    function getStockInfo(idDetalle) {
        var n = Number(idDetalle);
        if (isNaN(n) || n <= 0) return null;
        return stockData[n] != null ? stockData[n] : (stockData[idDetalle] != null ? stockData[idDetalle] : null);
    }

    function buildStockBadge(info, cantSolicitada) {
        if (!info) return '<span class="stock-badge stock-badge-no-existe"><i class="fa-solid fa-triangle-exclamation"></i> Sin datos de inventario</span>';
        if (!info.existeEnInventario) return '<span class="stock-badge stock-badge-no-existe"><i class="fa-solid fa-xmark"></i> No existe</span>';
        if (info.stockDisponible <= 0) return '<span class="stock-badge stock-badge-sin"><i class="fa-solid fa-triangle-exclamation"></i> Sin stock (0)</span>';
        if (info.stockDisponible < cantSolicitada) return '<span class="stock-badge stock-badge-parcial"><i class="fa-solid fa-exclamation"></i> Parcial (' + info.stockDisponible + ')</span>';
        return '<span class="stock-badge stock-badge-ok"><i class="fa-solid fa-check"></i> Disponible (' + info.stockDisponible + ')</span>';
    }

    function actualizarBotonCompleta() {
        var btnCompleta = document.getElementById('btnAprobarCompleta');
        if (!btnCompleta || !reqCompleta) return;
        var articulos = obtenerArticulosDeReq(reqCompleta);
        var puedeCompleta = articulos.length > 0;
        articulos.forEach(function (a) {
            var info = getStockInfo(idDetalleArticulo(a));
            var cantSolicitada = (a.cantidad != null ? a.cantidad : a.Cantidad) || 0;
            if (!info || !info.existeEnInventario || info.stockDisponible < cantSolicitada) {
                puedeCompleta = false;
            }
        });
        btnCompleta.disabled = !puedeCompleta;
        btnCompleta.title = !puedeCompleta ? 'No hay stock suficiente para todos los materiales.' : '';
    }

    /* ========== RENDER MODAL TABS ANÁLISIS ========== */

    function renderModalTab(i) {
        if (!reqCompleta || !modalBody) return;

        if (i === 0) {
            var articulos = obtenerArticulosDeReq(reqCompleta);
            var countOk = 0, countParcial = 0, countSin = 0;

            articulos.forEach(function (a) {
                var info = getStockInfo(idDetalleArticulo(a));
                var cantSol = (a.cantidad != null ? a.cantidad : a.Cantidad) || 0;
                if (!info || !info.existeEnInventario || info.stockDisponible <= 0) countSin++;
                else if (info.stockDisponible < cantSol) countParcial++;
                else countOk++;
            });

            var html = '<div class="almacen-resumen-cards">';
            html += '<div class="almacen-resumen-card"><span>Total Items</span><strong>' + articulos.length + '</strong></div>';
            html += '<div class="almacen-resumen-card" style="border-color:#a7f3d0"><span style="color:#059669">Disponibles</span><strong style="color:#059669">' + countOk + '</strong></div>';
            if (countParcial > 0) html += '<div class="almacen-resumen-card" style="border-color:#fde68a"><span style="color:#d97706">Stock parcial</span><strong style="color:#d97706">' + countParcial + '</strong></div>';
            if (countSin > 0) html += '<div class="almacen-resumen-card" style="border-color:#fecaca"><span style="color:#dc2626">Sin stock</span><strong style="color:#dc2626">' + countSin + '</strong></div>';
            html += '</div>';

            html += '<div class="table-responsive-container">';
            html += '<table class="tabla-requisiciones"><thead><tr>';
            html += '<th>Material</th><th>Solicitado</th><th>Unidad</th><th>Stock</th>';
            html += '<th>Entregar</th><th>Comprar</th><th>Cant. Compra</th><th>Detalle</th>';
            html += '</tr></thead><tbody>';

            articulos.forEach(function (a) {
                var textoCompleto = a.descripcionDetallada || a.DescripcionDetallada || '';
                var textoCorto = textoCompleto.length > 28 ? textoCompleto.substring(0, 28) + '…' : (textoCompleto || 'Sin descripción...');
                var tieneTexto = textoCompleto ? 'tiene-texto' : '';
                var fullEscapado = (textoCompleto || '').replace(/"/g, '&quot;');
                var cantSolicitada = (a.cantidad != null ? a.cantidad : a.Cantidad) || 0;
                var idDetalle = idDetalleArticulo(a);
                var info = getStockInfo(idDetalle);

                var valorInput = cantSolicitada;
                var inputClass = 'input-app almacen-aprob-cant';
                var inputDisabled = '';

                if (info) {
                    if (!info.existeEnInventario || info.stockDisponible <= 0) {
                        valorInput = 0;
                        inputClass += ' stock-insuficiente';
                        inputDisabled = ' disabled';
                    } else if (info.stockDisponible < cantSolicitada) {
                        valorInput = info.stockDisponible;
                        inputClass += ' stock-parcial-warn';
                    }
                }

                var faltante = cantSolicitada - valorInput;
                var cantCompraDefault = faltante > 0 ? faltante : '';

                html += '<tr>';
                html += '<td>' + (a.descripcion || a.Descripcion || '') + '</td>';
                html += '<td style="text-align:center;font-weight:600;">' + cantSolicitada + '</td>';
                html += '<td>' + (a.unidadMedida || a.UnidadMedida || '') + '</td>';
                html += '<td>' + buildStockBadge(info, cantSolicitada) + '</td>';
                html += '<td><input type="number" class="' + inputClass + '" min="0" step="1" value="' + valorInput + '" max="' + cantSolicitada + '" data-detalle="' + idDetalle + '" data-stock="' + (info ? info.stockDisponible : 0) + '" style="width:90px;padding:8px 10px;"' + inputDisabled + ' /></td>';
                html += '<td style="text-align:center"><input type="checkbox" class="almacen-chk-compra" data-detalle="' + idDetalle + '" style="width:18px;height:18px;cursor:pointer;" /></td>';
                html += '<td><input type="number" class="input-app almacen-cant-compra" min="1" step="1" value="' + cantCompraDefault + '" data-detalle="' + idDetalle + '" style="width:90px;padding:8px 10px;" disabled /></td>';
                html += '<td><div class="desc-preview-modal" data-full="' + fullEscapado + '" onclick="verDescDetalleModal(this)"><span class="desc-texto-preview ' + tieneTexto + '">' + textoCorto + '</span><i class="fa-solid fa-eye desc-icon"></i></div></td>';
                html += '</tr>';
            });

            html += '</tbody></table></div>';
            modalBody.innerHTML = html;

            modalBody.querySelectorAll('.almacen-chk-compra').forEach(function (chk) {
                chk.addEventListener('change', function () {
                    var detId = this.getAttribute('data-detalle');
                    var cantInput = modalBody.querySelector('.almacen-cant-compra[data-detalle="' + detId + '"]');
                    if (cantInput) {
                        cantInput.disabled = !this.checked;
                        if (!this.checked) cantInput.value = '';
                    }
                });
            });

            actualizarBotonCompleta();

        } else if (i === 1) {
            var r = reqCompleta;
            var html = '<div class="almacen-grid2">';
            html += '<div><h4 style="margin-top:0">Información del Solicitante</h4>';
            html += '<div class="almacen-campo"><label>Departamento</label><input type="text" disabled value="' + (r.departamento || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Responsable</label><input type="text" disabled value="' + (r.nomResponsableDepartamento || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Correo</label><input type="text" disabled value="' + (r.correo || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Teléfono</label><input type="text" disabled value="' + (r.telefono || '') + '" /></div>';
            html += '<div class="almacen-campo"><label>Lugar de entrega</label><input type="text" disabled value="' + (r.lugarEntrega || '') + '" /></div>';
            html += '</div>';
            html += '<div><h4 style="margin-top:0">Justificación</h4>';
            html += '<div class="almacen-campo"><label>Uso Específico</label><textarea rows="3" disabled>' + (r.usoEspecifico || '') + '</textarea></div>';
            html += '<div class="almacen-campo"><label>Justificación</label><textarea rows="5" disabled>' + (r.justificacion || '') + '</textarea></div>';
            html += '</div></div>';
            modalBody.innerHTML = html;

        } else {
            modalBody.innerHTML = '<div class="almacen-alerta">Las observaciones son obligatorias para rechazar una requisición.</div>' +
                '<div class="almacen-campo"><label>Observaciones del Almacén</label>' +
                '<textarea id="obs-textarea" rows="6" placeholder="Escribe observaciones..."></textarea></div>';
        }
    }

    /* ========== PANEL DESCRIPCIÓN DETALLADA ========== */

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
        window._descPanelModalTrigger = el;
        textarea.focus();
    };

    window.cerrarDescPanelModal = function () {
        var panel = document.getElementById('desc-panel-modal');
        if (panel) panel.style.display = 'none';
        window._descPanelModalTrigger = null;
    };

    document.addEventListener('mousedown', function (e) {
        if (!window._descPanelModalTrigger) return;
        var panel = document.getElementById('desc-panel-modal');
        if (!window._descPanelModalTrigger.contains(e.target) && panel && !panel.contains(e.target)) {
            cerrarDescPanelModal();
        }
    });

    /* ========== TABS INTERNOS DEL MODAL ANÁLISIS ========== */

    modalTabsBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            if (!reqCompleta) return;
            modalTabsBtns.forEach(function (b) { b.classList.remove('activo'); });
            this.classList.add('activo');
            renderModalTab(parseInt(this.getAttribute('data-modal-tab'), 10));
        });
    });

    /* ========== ABRIR MODAL ANÁLISIS ========== */

    window.abrirModal = function (id, folio, departamento, estatus) {
        reqActualId = id;
        stockData = {};
        reqCompleta = null;

        document.getElementById('modalAlmacenTitulo').textContent = (folio || '') + ' — ' + (departamento || '');
        document.getElementById('modalAlmacenSubtitulo').textContent = 'Estado: ' + (estatus || '');
        modalBody.innerHTML = '<p class="text-muted">Cargando requisición e inventario...</p>';

        var btnCompleta = document.getElementById('btnAprobarCompleta');
        if (btnCompleta) btnCompleta.disabled = true;

        var modal = new bootstrap.Modal(document.getElementById('modalAlmacen'));
        modal.show();

        var urlReq = (urlObtenerRequisicion || '').replace(/\/$/, '') + '?id=' + encodeURIComponent(id);
        var urlStock = (urlConsultarStock || '').replace(/\/$/, '') + '?id=' + encodeURIComponent(id);
        var fetchOpts = { credentials: 'same-origin', headers: { 'Accept': 'application/json' } };

        Promise.all([
            fetch(urlReq, fetchOpts).then(function (r) { return r.ok ? r.json() : Promise.reject('req'); }),
            fetch(urlStock, fetchOpts)
                .then(function (r) { return r.ok ? r.json().then(parsearRespuestaStock).catch(function () { return []; }) : []; })
                .catch(function () { return []; })
        ]).then(function (results) {
            reqCompleta = results[0];
            var stockArr = parsearRespuestaStock(results[1]);
            stockData = {};
            stockArr.forEach(function (s) {
                var norm = normalizarFilaStock(s);
                if (norm) stockData[norm.idRequisicionDetalle] = norm;
            });
            modalTabsBtns.forEach(function (b) { b.classList.remove('activo'); });
            document.querySelector('.almacen-modal-tabs-btn[data-modal-tab="0"]').classList.add('activo');
            renderModalTab(0);
        }).catch(function () {
            modalBody.innerHTML = '<p class="text-danger">No se pudo cargar la requisición.</p>';
        });
    };

    document.body.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-almacen-analizar]');
        if (btn) {
            e.preventDefault();
            abrirModal(
                parseInt(btn.getAttribute('data-id'), 10),
                btn.getAttribute('data-folio') || '',
                btn.getAttribute('data-depto') || '',
                btn.getAttribute('data-estatus') || ''
            );
        }
    });

    /* ========== MODAL ENTREGA FÍSICA ========== */

    var entregaActualId = null;     // IdRequisicion en el modal de entrega
    var entregaArticulos = [];      // Artículos (movimientos) del modal de entrega

    // Abrir modal de entrega al hacer clic en el botón del tab "A Entregar"
    document.body.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-abrir-entrega]');
        if (!btn) return;
        e.preventDefault();

        entregaActualId = parseInt(btn.getAttribute('data-entrega-id'), 10);
        var folio = btn.getAttribute('data-entrega-folio') || '';
        var depto = btn.getAttribute('data-entrega-depto') || '';

        document.getElementById('modalEntregaTitulo').textContent = folio + ' — ' + depto;
        document.getElementById('modalEntregaSubtitulo').textContent = 'Confirma los artículos que se entregarán físicamente';
        document.getElementById('tablaEntregaBody').innerHTML = '<tr><td colspan="4" class="text-center text-muted">Cargando...</td></tr>';
        document.getElementById('chkEntregaTodos').checked = false;

        // Buscar los artículos de esta requisición del modelo ya cargado en el tab
        // (los datos vienen del servidor en el HTML, los leemos de las filas del tab)
        // Para mayor frescura, usamos el endpoint que ya tenemos
        var fetchOpts = { credentials: 'same-origin', headers: { 'Accept': 'application/json' } };
        fetch('/Almacen/ObtenerEntregasPendientes', fetchOpts)
            .then(function (r) { return r.ok ? r.json() : Promise.reject(); })
            .then(function (data) {
                // Buscar la requisición actual
                var grupo = (data || []).find(function (g) {
                    return (g.idRequisicion || g.IdRequisicion) === entregaActualId;
                });

                if (!grupo) {
                    document.getElementById('tablaEntregaBody').innerHTML =
                        '<tr><td colspan="4" class="text-center text-muted">Sin artículos pendientes</td></tr>';
                    return;
                }

                entregaArticulos = (grupo.articulos || grupo.Articulos || []);
                renderTablaEntrega(entregaArticulos);
            })
            .catch(function () {
                document.getElementById('tablaEntregaBody').innerHTML =
                    '<tr><td colspan="4" class="text-danger text-center">Error al cargar los artículos</td></tr>';
            });

        new bootstrap.Modal(document.getElementById('modalEntrega')).show();
    });

    function renderTablaEntrega(articulos) {
        if (!articulos.length) {
            document.getElementById('tablaEntregaBody').innerHTML =
                '<tr><td colspan="4" class="text-center text-muted">Sin artículos pendientes</td></tr>';
            return;
        }

        var html = '';
        articulos.forEach(function (a) {
            var idMov = a.idMovimiento || a.IdMovimiento;
            var desc = a.descripcion || a.Descripcion || '';
            var cant = a.cantidadMovimiento || a.CantidadMovimiento || 0;
            var unidad = a.unidadMedida || a.UnidadMedida || '';

            html += '<tr data-id-mov="' + idMov + '">';
            html += '<td style="text-align:center;">';
            html += '<input type="checkbox" class="chk-entrega-articulo" data-id-mov="' + idMov + '" style="width:16px;height:16px;cursor:pointer;" checked />';
            html += '</td>';
            html += '<td>' + desc + '</td>';
            html += '<td style="text-align:center;font-weight:600;">' + cant + '</td>';
            html += '<td>' + unidad + '</td>';
            html += '</tr>';
        });

        document.getElementById('tablaEntregaBody').innerHTML = html;

        // Checkbox "seleccionar todos"
        var chkTodos = document.getElementById('chkEntregaTodos');
        chkTodos.checked = true;
        chkTodos.addEventListener('change', function () {
            document.querySelectorAll('.chk-entrega-articulo').forEach(function (chk) {
                chk.checked = chkTodos.checked;
            });
        });
    }

    // Confirmar entrega física
    document.getElementById('btnConfirmarEntrega').addEventListener('click', function () {
        if (!entregaActualId) { swalError('No se identificó la requisición.'); return; }

        var seleccionados = [];
        document.querySelectorAll('.chk-entrega-articulo:checked').forEach(function (chk) {
            seleccionados.push(parseInt(chk.getAttribute('data-id-mov'), 10));
        });

        if (seleccionados.length === 0) {
            swalWarning('Selecciona al menos un artículo para confirmar la entrega.');
            return;
        }

        var totalArticulos = entregaArticulos.length;
        var texto = seleccionados.length === totalArticulos
            ? 'Se confirmarán todos los artículos como entregados al departamento solicitante.'
            : 'Se confirmarán ' + seleccionados.length + ' de ' + totalArticulos + ' artículos como entregados.';

        swalConfirmar('Confirmar entrega', texto, 'Sí, confirmar')
            .then(function (result) {
                if (!result.isConfirmed) return;
                Swal.fire({ title: 'Procesando...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });

                postJson(urlConfirmarEntrega, {
                    idRequisicion: entregaActualId,
                    idsMovimientos: seleccionados
                }).then(function (r) {
                    if (r.ok) {
                        var modalEl = document.getElementById('modalEntrega');
                        bootstrap.Modal.getInstance(modalEl).hide();
                        swalExito(r.mensaje || 'Entrega confirmada correctamente.')
                            .then(function () { location.reload(); });
                    } else {
                        swalError(r.error || 'No se pudo confirmar la entrega.');
                    }
                }).catch(function (err) {
                    swalError(typeof err === 'string' ? err : 'No se pudo confirmar la entrega.');
                });
            });
    });

    /* ========== ACCIONES DEL MODAL ANÁLISIS ========== */

    function cerrarModalAlmacen() {
        var modalEl = document.getElementById('modalAlmacen');
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    document.body.addEventListener('click', function (e) {
        var btnAprobar = e.target.closest('.btn-almacen-aprobar');
        var btnProcesar = e.target.closest('.btn-almacen-parcial');
        var btnRechazar = e.target.closest('.btn-almacen-rechazar');
        if (!btnAprobar && !btnProcesar && !btnRechazar) return;
        if (!reqActualId) { swalError('No se pudo identificar la requisición.'); return; }

        /* ---- APROBAR COMPLETA ---- */
        if (btnAprobar) {
            if (!urlAprobarCompleta) { swalError('URL de aprobación no configurada.'); return; }
            swalConfirmar('Aprobar completa', 'Se descontará del inventario el total de materiales solicitados. ¿Desea continuar?', 'Sí, aprobar')
                .then(function (result) {
                    if (!result.isConfirmed) return;
                    Swal.fire({ title: 'Procesando...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });
                    postJson(urlAprobarCompleta, { idRequisicion: reqActualId })
                        .then(function (r) {
                            if (r.ok) {
                                cerrarModalAlmacen();
                                swalExito(r.mensaje || 'Requisición autorizada completa.').then(function () { location.reload(); });
                            } else {
                                swalError(r.error || 'No se pudo aprobar.');
                            }
                        })
                        .catch(function (err) { swalError(typeof err === 'string' ? err : 'No se pudo aprobar.'); });
                });
        }

        /* ---- PROCESAR (entregas + compras) ---- */
        if (btnProcesar) {
            if (!urlProcesarRequisicion) { swalError('URL de procesamiento no configurada.'); return; }

            var entregas = [];
            var compras = [];
            var invalido = false;
            var errorMsg = '';

            document.querySelectorAll('.almacen-aprob-cant[data-detalle]').forEach(function (inp) {
                var idDetalle = parseInt(inp.getAttribute('data-detalle'), 10);
                var val = parseInt(inp.value || '0', 10);
                var max = parseInt(inp.getAttribute('max') || '0', 10);
                var stockDisp = parseInt(inp.getAttribute('data-stock') || '0', 10);
                if (isNaN(val) || val < 0) { invalido = true; errorMsg = 'Las cantidades de entrega no pueden ser negativas.'; return; }
                if (val > 0) {
                    if (max > 0 && val > max) { invalido = true; errorMsg = 'Una cantidad de entrega excede la solicitada.'; return; }
                    if (val > stockDisp) { invalido = true; errorMsg = 'La cantidad (' + val + ') excede el stock disponible (' + stockDisp + ').'; return; }
                    entregas.push({ idRequisicionDetalle: idDetalle, cantidadAprobada: val });
                }
            });

            document.querySelectorAll('.almacen-chk-compra:checked').forEach(function (chk) {
                var idDetalle = parseInt(chk.getAttribute('data-detalle'), 10);
                var cantInput = document.querySelector('.almacen-cant-compra[data-detalle="' + idDetalle + '"]');
                var cantVal = cantInput ? parseInt(cantInput.value || '0', 10) : 0;
                if (cantVal <= 0) { invalido = true; errorMsg = 'Ingrese la cantidad a comprar para todos los artículos marcados.'; return; }
                compras.push({ idRequisicionDetalle: idDetalle, cantidadComprar: cantVal });
            });

            if (invalido) { swalWarning(errorMsg); return; }
            if (entregas.length === 0 && compras.length === 0) {
                swalWarning('Debe indicar al menos una entrega o marcar al menos un artículo para compra.');
                return;
            }

            var textoConfirm = '';
            if (entregas.length > 0) textoConfirm += entregas.length + ' material(es) se prepararán para entrega física. ';
            if (compras.length > 0) textoConfirm += compras.length + ' material(es) se enviarán a compra. ';
            textoConfirm += '¿Desea continuar?';

            swalConfirmar('Procesar requisición', textoConfirm, 'Sí, procesar')
                .then(function (result) {
                    if (!result.isConfirmed) return;
                    Swal.fire({ title: 'Procesando...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });
                    postJson(urlProcesarRequisicion, { idRequisicion: reqActualId, entregas: entregas, compras: compras })
                        .then(function (r) {
                            if (r.ok) {
                                cerrarModalAlmacen();
                                swalExito(r.mensaje || 'Requisición procesada correctamente.').then(function () { location.reload(); });
                            } else {
                                swalError(r.error || 'No se pudo procesar.');
                            }
                        })
                        .catch(function (err) { swalError(typeof err === 'string' ? err : 'No se pudo procesar.'); });
                });
        }

        /* ---- RECHAZAR ---- */
        if (btnRechazar) {
            if (!urlRechazar) { swalError('URL de rechazo no configurada.'); return; }
            var obsArea = document.getElementById('obs-textarea');
            var obs = obsArea ? (obsArea.value || '').trim() : '';

            if (!obs) {
                modalTabsBtns.forEach(function (b) { b.classList.remove('activo'); });
                var btnObs = document.querySelector('.almacen-modal-tabs-btn[data-modal-tab="2"]');
                if (btnObs) btnObs.classList.add('activo');
                renderModalTab(2);
                setTimeout(function () {
                    var ta = document.getElementById('obs-textarea');
                    if (ta) ta.focus();
                }, 150);
                swalWarning('Escribe las observaciones para poder rechazar la requisición.');
                return;
            }

            swalConfirmar('Rechazar requisición', 'La requisición será rechazada con el motivo capturado. ¿Desea continuar?', 'Sí, rechazar')
                .then(function (result) {
                    if (!result.isConfirmed) return;
                    Swal.fire({ title: 'Procesando...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });
                    postJson(urlRechazar, { idRequisicion: reqActualId, motivo: obs })
                        .then(function (r) {
                            if (r.ok) {
                                cerrarModalAlmacen();
                                swalExito(r.mensaje || 'Requisición rechazada.').then(function () { location.reload(); });
                            } else {
                                swalError(r.error || 'No se pudo rechazar.');
                            }
                        })
                        .catch(function (err) { swalError(typeof err === 'string' ? err : 'No se pudo rechazar.'); });
                });
        }
    });

    /* ========== INGRESO DE INVENTARIO ========== */

    var btnIngreso = document.getElementById('btnRegistrarIngreso');
    if (btnIngreso) {
        btnIngreso.addEventListener('click', function () {
            if (!urlRegistrarIngreso) { swalError('URL de ingreso no configurada.'); return; }

            var clave = (document.getElementById('ingresoClave') ? document.getElementById('ingresoClave').value : '').trim();
            var descripcion = (document.getElementById('ingresoDescripcion') ? document.getElementById('ingresoDescripcion').value : '').trim();
            var unidadMedida = (document.getElementById('ingresoUnidad') ? document.getElementById('ingresoUnidad').value : '').trim();
            var cantidad = parseInt(document.getElementById('ingresoCantidad') ? document.getElementById('ingresoCantidad').value : '0', 10);
            var motivo = (document.getElementById('ingresoMotivo') ? document.getElementById('ingresoMotivo').value : '').trim();

            if (!descripcion) { swalWarning('La descripción es obligatoria.'); return; }
            if (!unidadMedida) { swalWarning('La unidad de medida es obligatoria.'); return; }
            if (!cantidad || cantidad <= 0) { swalWarning('La cantidad debe ser mayor a 0.'); return; }
            if (!motivo) { swalWarning('El motivo es obligatorio.'); return; }

            swalConfirmar('Registrar ingreso', 'Se registrará: ' + descripcion + ' (' + unidadMedida + ') x' + cantidad + '. ¿Continuar?', 'Sí, registrar')
                .then(function (result) {
                    if (!result.isConfirmed) return;
                    Swal.fire({ title: 'Procesando...', allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });
                    postJson(urlRegistrarIngreso, { clave: clave, descripcion: descripcion, unidadMedida: unidadMedida, cantidad: cantidad, motivo: motivo })
                        .then(function (r) {
                            if (r.ok) {
                                swalExito(r.mensaje || 'Ingreso registrado correctamente.').then(function () { location.reload(); });
                            } else {
                                swalError(r.error || 'No se pudo registrar el ingreso.');
                            }
                        })
                        .catch(function (err) { swalError(typeof err === 'string' ? err : 'No se pudo registrar el ingreso.'); });
                });
        });
    }

    /* ========== SELECT2 ========== */

    $(function () {
        $('#filtroEstado').select2({ width: '100%', language: 'es', minimumResultsForSearch: Infinity, placeholder: 'Todos los estados' });
        $('#filtroUnidadMedida').select2({ width: '100%', language: 'es', minimumResultsForSearch: 10, placeholder: 'Todas las unidades', allowClear: true });
        $('#filtroUnidadMedida').on('change select2:select', function () { filtrarTablaInventario(); });
    });

})();