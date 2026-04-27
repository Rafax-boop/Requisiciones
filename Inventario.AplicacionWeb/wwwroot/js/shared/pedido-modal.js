(function () {
    'use strict';

    var MX = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', minimumFractionDigits: 2 });
    var _idRequisicionPedido = null;

    function obtenerUrls() {
        var c = document.querySelector('.tabla-requi-page');
        return {
            obtener: c ? c.getAttribute('data-url-obtener-pedido') : null,
            generar: c ? c.getAttribute('data-url-generar-pedido-pdf') : null
        };
    }

    function obtenerIdRequisicionActual() {
        // tabla-requisiciones.js lo expone en window._expedienteActualGlobal al abrir un expediente
        return window._expedienteActualGlobal || null;
    }

    window.abrirModalPedido = function () {
        var id = obtenerIdRequisicionActual();
        if (!id) {
            alert('No se pudo determinar la requisición activa. Abre un expediente primero.');
            return;
        }
        _idRequisicionPedido = id;

        var urls = obtenerUrls();
        if (!urls.obtener) { console.error('data-url-obtener-pedido no configurado'); return; }

        // Establecer la URL del form
        var form = document.getElementById('formPedido');
        if (form && urls.generar) form.action = urls.generar;

        // Mostrar modal y estado de carga
        document.getElementById('pedidoCargando').style.display = 'block';
        document.getElementById('formPedido').style.display = 'none';
        document.getElementById('pedidoSubtitulo').textContent = 'Cargando...';

        var modal = new bootstrap.Modal(document.getElementById('modalPedido'));
        modal.show();

        // Cargar datos
        fetch(urls.obtener + '?idRequisicion=' + id)
            .then(function (r) { return r.json(); })
            .then(function (resp) {
                if (!resp.success) {
                    document.getElementById('pedidoCargando').innerHTML =
                        '<p class="text-danger"><i class="fa-solid fa-circle-exclamation"></i> ' +
                        (resp.mensaje || 'Error al cargar el pedido') + '</p>';
                    return;
                }
                poblarFormularioPedido(resp.data);
            })
            .catch(function (err) {
                document.getElementById('pedidoCargando').innerHTML =
                    '<p class="text-danger">Error de red: ' + err.message + '</p>';
            });
    };

    function poblarFormularioPedido(d) {
        document.getElementById('pedIdRequisicion').value = d.idRequisicion;
        document.getElementById('pedidoSubtitulo').textContent = 'Requisición: ' + (d.numRequisicion || d.idRequisicion);
        document.getElementById('pedProveedorNombre').value = d.proveedorNombre || '';
        document.getElementById('pedProveedorRfc').value = d.proveedorRfc || '';
        document.getElementById('pedProveedorDir').value = d.proveedorDireccion || '';
        document.getElementById('pedDepartamento').value = d.departamento || '';
        document.getElementById('pedResponsable').value = d.responsable || '';
        document.getElementById('pedLugarEntrega').value = d.lugarEntrega || '';
        document.getElementById('pedPartida').value = d.partidaPresupuestal || '';

        // Tabla de artículos
        var tbody = document.getElementById('pedTablaBody');
        tbody.innerHTML = '';
        var suma = 0;
        (d.partidas || []).forEach(function (p) {
            var total = p.precioUnitario * p.cantidad;
            suma += total;
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td style="text-align:center">' + p.numero + '</td>' +
                '<td>' + (p.clave || '') + '</td>' +
                '<td>' + (p.descripcion || '') + '</td>' +
                '<td style="text-align:center">' + p.cantidad + '</td>' +
                '<td style="text-align:center">' + (p.unidadMedida || '') + '</td>' +
                '<td style="text-align:right">' + MX.format(p.precioUnitario) + '</td>' +
                '<td style="text-align:right">' + MX.format(total) + '</td>' +
                '<td style="text-align:center">' + (p.tieneIva ? 'Sí' : 'No') + '</td>';
            tbody.appendChild(tr);
        });

        // Totales
        var iva = suma * 0.16;
        var subtotal = suma + iva;
        var retencion = subtotal * 0.005;
        var total = subtotal - retencion;

        document.getElementById('pedSuma').textContent = MX.format(suma);
        document.getElementById('pedIva').textContent = MX.format(iva);
        document.getElementById('pedSubtotal').textContent = MX.format(subtotal);
        document.getElementById('pedRetencion').textContent = MX.format(retencion);
        document.getElementById('pedTotal').textContent = MX.format(total);

        // Mostrar form
        document.getElementById('pedidoCargando').style.display = 'none';
        document.getElementById('formPedido').style.display = 'block';
    }
})();
