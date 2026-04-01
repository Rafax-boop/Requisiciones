/**
 * Adjuntos en modales de requisición: miniatura + visor para imágenes;
 * PDF/Excel u otros con icono y descarga directa (sin nueva pestaña cuando el origen lo permite).
 */
(function (window, document) {
    'use strict';

    var visorListenersHechos = false;

    function esImagen(nombreArchivo) {
        var ext = (nombreArchivo || '').split('.').pop().toLowerCase();
        return ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].indexOf(ext) !== -1;
    }

    function escAttr(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;');
    }

    function descargarArchivo(url, nombreSugerido) {
        if (!url) return;
        nombreSugerido = nombreSugerido || 'archivo';
        fetch(url, { credentials: 'same-origin' })
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP');
                return res.blob();
            })
            .then(function (blob) {
                var objUrl = URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = objUrl;
                a.download = nombreSugerido;
                a.style.display = 'none';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(objUrl);
            })
            .catch(function () {
                var a = document.createElement('a');
                a.href = url;
                a.download = nombreSugerido;
                a.style.display = 'none';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
            });
    }

    function obtenerContenedorArchivosReadonly() {
        var contenedor = document.getElementById('contenedorArchivosReadonly');
        if (contenedor) return contenedor;
        contenedor = document.createElement('div');
        contenedor.id = 'contenedorArchivosReadonly';
        contenedor.className = 'modal-adjuntos-archivos';
        contenedor.style.cssText = 'margin-top:16px;';
        var seccion = document.querySelector('.modal-body .seccionAtender');
        if (seccion) seccion.appendChild(contenedor);
        return contenedor;
    }

    /**
     * Resuelve el par overlay/img del lightbox.
     * Preferencia: #visor-imagenes-tabla (tablas + formularios unificados),
     * alternativa: #visor-imagenes-global (vistas legacy).
     */
    function getVisorPair() {
        var oTabla = document.getElementById('visor-imagenes-tabla');
        var iTabla = document.getElementById('visor-imagenes-tabla-img');
        if (oTabla && iTabla) return { overlay: oTabla, img: iTabla };
        var oGlobal = document.getElementById('visor-imagenes-global');
        var iGlobal = document.getElementById('visor-imagenes-img');
        if (oGlobal && iGlobal) return { overlay: oGlobal, img: iGlobal };
        return null;
    }

    function limpiarSrcVisoresInactivos() {
        setTimeout(function () {
            var o1 = document.getElementById('visor-imagenes-tabla');
            var i1 = document.getElementById('visor-imagenes-tabla-img');
            if (i1 && o1 && !o1.classList.contains('activo')) i1.src = '';
            var o2 = document.getElementById('visor-imagenes-global');
            var i2 = document.getElementById('visor-imagenes-img');
            if (i2 && o2 && !o2.classList.contains('activo')) i2.src = '';
        }, 300);
    }

    window.cerrarVisorImagenTabla = function () {
        var t = document.getElementById('visor-imagenes-tabla');
        var g = document.getElementById('visor-imagenes-global');
        if (t) t.classList.remove('activo');
        if (g) g.classList.remove('activo');
        limpiarSrcVisoresInactivos();
    };

    window.abrirVisorImagenTabla = function (src) {
        var v = getVisorPair();
        if (!v) return;
        v.img.src = src || '';
        v.overlay.classList.add('activo');
    };

    function initVisorListenersOnce() {
        if (visorListenersHechos) return;
        visorListenersHechos = true;
        function bindOverlayClick(overlayId) {
            var overlay = document.getElementById(overlayId);
            if (!overlay) return;
            overlay.addEventListener('click', function (e) {
                if (e.target === overlay) window.cerrarVisorImagenTabla();
            });
        }
        bindOverlayClick('visor-imagenes-tabla');
        bindOverlayClick('visor-imagenes-global');
        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape') return;
            var t = document.getElementById('visor-imagenes-tabla');
            var g = document.getElementById('visor-imagenes-global');
            var abierto = (t && t.classList.contains('activo')) || (g && g.classList.contains('activo'));
            if (abierto) window.cerrarVisorImagenTabla();
        });
    }

    function renderGrupoHtml(titulo, archivos) {
        if (!archivos.length) return '';
        var html = '<div class="modal-adjuntos-grupo">';
        html += '<label class="modal-adjuntos-grupo-titulo">' + escAttr(titulo) + '</label>';
        html += '<div class="modal-adjuntos-fila">';
        archivos.forEach(function (a) {
            var nombre = a.nombreArchivo || a.NombreArchivo || 'Archivo';
            var ruta = a.ruta || a.Ruta || '';
            var ext = (nombre || '').split('.').pop().toLowerCase();
            var esPdf = ext === 'pdf';
            var esExcel = ['xls', 'xlsx', 'xlsm', 'csv'].indexOf(ext) !== -1;
            var esImg = esImagen(nombre);

            if (esImg) {
                html += '<div class="archivo-thumb-visor modal-adjunto-thumb" role="button" tabindex="0" data-ruta="' + escAttr(ruta) + '" title="Ver imagen">';
                html += '<img src="' + escAttr(ruta) + '" alt="" loading="lazy" />';
                html += '<span class="modal-adjunto-thumb-nombre">' + escAttr(nombre) + '</span>';
                html += '</div>';
            } else {
                var icono;
                var color;
                if (esPdf) {
                    icono = 'fa-file-pdf';
                    color = '#e74c3c';
                } else if (esExcel) {
                    icono = 'fa-file-excel';
                    color = '#217346';
                } else {
                    icono = 'fa-file';
                    color = '#6b7280';
                }
                html += '<button type="button" class="modal-adjunto-chip" data-url="' + escAttr(ruta) + '" data-nombre="' + escAttr(nombre) + '">';
                html += '<i class="fa-solid ' + icono + '" style="color:' + color + ';font-size:1.25rem;" aria-hidden="true"></i>';
                html += '<span class="modal-adjunto-chip-texto">' + escAttr(nombre) + '</span>';
                html += '<i class="fa-solid fa-download modal-adjunto-chip-dl" aria-hidden="true"></i>';
                html += '</button>';
            }
        });
        html += '</div></div>';
        return html;
    }

    function enlazarEventosContenedor(contenedor) {
        contenedor.querySelectorAll('.archivo-thumb-visor').forEach(function (el) {
            var abrir = function () {
                window.abrirVisorImagenTabla(el.getAttribute('data-ruta'));
            };
            el.addEventListener('click', abrir);
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    abrir();
                }
            });
        });
        contenedor.querySelectorAll('.modal-adjunto-chip').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var url = btn.getAttribute('data-url');
                var nom = btn.getAttribute('data-nombre') || 'archivo';
                if (url) descargarArchivo(url, nom);
            });
        });
    }

    function renderizarArchivosReadonly(cotizaciones, cuadro) {
        initVisorListenersOnce();
        var contenedor = document.getElementById('contenedorArchivosReadonly') || obtenerContenedorArchivosReadonly();
        if (!contenedor) return;

        contenedor.innerHTML =
            renderGrupoHtml('Cotizaciones', cotizaciones || []) +
            renderGrupoHtml('Cuadro comparativo', cuadro || []);

        enlazarEventosContenedor(contenedor);
    }

    window.ModalAdjuntos = {
        esImagen: esImagen,
        renderizarArchivosReadonly: renderizarArchivosReadonly,
        descargarArchivo: descargarArchivo,
        renderGrupoHtml: renderGrupoHtml,
        enlazarEventosContenedor: enlazarEventosContenedor
    };

    initVisorListenersOnce();
})(window, document);

