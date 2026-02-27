(function () {
    var container = document.querySelector('[data-url-buscar-articulos]');
    var urlBuscarArticulos = container ? container.getAttribute('data-url-buscar-articulos') : '';
    var urlObtenerInfoArticulo = container ? container.getAttribute('data-url-obtener-info-articulo') : '';
    var esMensual = container ? container.getAttribute('data-tipo-requisicion') === 'mensual' : false;

    flatpickr('#fechaEmisionPicker', {
        locale: 'es',
        dateFormat: 'Y-m-d',
        altInput: true,
        altFormat: 'd / m / Y',
        defaultDate: new Date(),
        allowInput: false,
        disableMobile: true
    });

    var contadorArticulos = 0;

    $(document).ready(function () {
        if (window.articulosIniciales && window.articulosIniciales.length > 0) {
            window.articulosIniciales.forEach(function (art) {
                cargarArticuloExistente(art);
            });
        } else {
            agregarArticulo();
        }
    });

    function cargarArticuloExistente(art) {
        var index = contadorArticulos++;

        var fila = [
            '<tr data-index="', index, '">',
            '<td class="id-' + index + '">' + (art.idArticulo || '-') + '</td>',
            '<td class="cog-' + index + '">' + (art.cog || '-') + '</td>',
            '<td class="clave-' + index + '">-</td>',
            '<td>',
            '<select name="Articulos[' + index + '].IdArticulo" class="form-select select-articulo" data-index="' + index + '" style="width: 240px;" required></select>',
            '<input type="hidden" name="Articulos[' + index + '].Descripcion" class="descripcion-hidden-' + index + '" value="' + (art.descripcion || '') + '" />',
            '<input type="hidden" name="Articulos[' + index + '].Cog" class="cog-hidden-' + index + '" value="' + (art.cog || '') + '" />',
            '<input type="hidden" name="Articulos[' + index + '].ClaveMaterial" class="clave-hidden-' + index + '" />',
            '<input type="hidden" name="Articulos[' + index + '].UnidadMedida" class="unidad-hidden-' + index + '" value="' + (art.unidadMedida || '') + '" />',
            '</td>',
            '<td class="unidad-' + index + '">' + (art.unidadMedida || '-') + '</td>',
            '<td><input type="number" name="Articulos[' + index + '].Cantidad" class="form-control cantidad-input" min="1" value="' + (art.cantidad || 1) + '" required /></td>',
            '<td class="descripcion-detallada-cell"><div class="desc-wrapper">',
            '<div class="desc-preview" onclick="expandirDesc(this)"><span class="desc-texto-preview' + (art.descripcionDetallada ? ' tiene-texto' : '') + '">' + (art.descripcionDetallada ? (art.descripcionDetallada.length > 40 ? art.descripcionDetallada.substring(0, 40) + '…' : art.descripcionDetallada) : 'Sin descripción...') + '</span><i class="fa-solid fa-pen-to-square desc-icon"></i></div>',
            '<input type="hidden" name="Articulos[' + index + '].DescripcionDetallada" class="desc-hidden" value="' + (art.descripcionDetallada || '') + '" />',
            '</div></td>',
            '<td class="text-center"><button type="button" class="btn btn-danger btn-sm" onclick="eliminarArticulo(this)"><i class="fa-solid fa-circle-minus"></i></button></td>',
            '</tr>'
        ].join('');

        $('#tablaArticulos tbody').append(fila);

        var $select = $('.select-articulo[data-index="' + index + '"]');
        inicializarSelect2($select);

        if (art.idArticulo && art.descripcion) {
            var option = new Option(art.descripcion, art.idArticulo, true, true);
            $select.append(option).trigger('change');
        }
    }

    window.agregarArticulo = function () {
        var index = contadorArticulos++;

        var fila = [
            '<tr data-index="', index, '">',
            '<td class="id-' + index + '">-</td>',
            '<td class="cog-' + index + '">-</td>',
            '<td class="clave-' + index + '">-</td>',
            '<td>',
            '<select name="Articulos[' + index + '].IdArticulo" class="form-select select-articulo" data-index="' + index + '" style="width: 240px;" required></select>',
            '<input type="hidden" name="Articulos[' + index + '].Descripcion" class="descripcion-hidden-' + index + '" />',
            '<input type="hidden" name="Articulos[' + index + '].Cog" class="cog-hidden-' + index + '" />',
            '<input type="hidden" name="Articulos[' + index + '].ClaveMaterial" class="clave-hidden-' + index + '" />',
            '<input type="hidden" name="Articulos[' + index + '].UnidadMedida" class="unidad-hidden-' + index + '" />',
            '</td>',
            '<td class="unidad-' + index + '">-</td>',
            '<td><input type="number" name="Articulos[' + index + '].Cantidad" class="form-control cantidad-input" min="1" value="1" required /></td>',
            '<td class="descripcion-detallada-cell"><div class="desc-wrapper">',
            '<div class="desc-preview" onclick="expandirDesc(this)"><span class="desc-texto-preview">Sin descripción...</span><i class="fa-solid fa-pen-to-square desc-icon"></i></div>',
            '<input type="hidden" name="Articulos[' + index + '].DescripcionDetallada" class="desc-hidden" />',
            '</div></td>',
            '<td class="text-center"><button type="button" class="btn btn-danger btn-sm" onclick="eliminarArticulo(this)"><i class="fa-solid fa-circle-minus"></i></button></td>',
            '</tr>'
        ].join('');

        $('#tablaArticulos tbody').append(fila);
        inicializarSelect2($('.select-articulo[data-index="' + index + '"]'));
    };

    window.eliminarArticulo = function (btn) {
        $(btn).closest('tr').remove();
    };

    function inicializarSelect2($elemento) {
        $elemento.select2({
            width: 'resolve',
            placeholder: 'Buscar artículo...',
            minimumInputLength: 2,
            language: 'es',
            ajax: {
                url: urlBuscarArticulos,
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return { term: params.term, mensual: esMensual };
                },
                processResults: function (data) {
                    return { results: data };
                },
                cache: true
            }
        });
    }

    $('#tablaArticulos').on('change', '.select-articulo', function () {
        var select = $(this);
        var index = select.data('index');
        var idArticulo = select.val();

        if (idArticulo && urlObtenerInfoArticulo) {
            $.get(urlObtenerInfoArticulo, { id: idArticulo }, function (data) {
                $('.id-' + index).text(data.id);
                $('.cog-' + index).text(data.cog);
                $('.clave-' + index).text(data.clave);
                $('.unidad-' + index).text(data.unidadMedida);
                $('.descripcion-hidden-' + index).val(data.descripcion);
                $('.cog-hidden-' + index).val(data.cog);
                $('.clave-hidden-' + index).val(data.clave);
                $('.unidad-hidden-' + index).val(data.unidadMedida);
            });
        }
    });

    $('#tablaArticulos').on('input', '.cantidad-input', function () {
        var index = $(this).data('index');
        if (typeof calcularSubtotal === 'function') calcularSubtotal(index);
    });

    var _descWrapperActivo = null;

    window.expandirDesc = function (previewEl) {
        if (_descWrapperActivo && _descWrapperActivo !== previewEl.closest('.desc-wrapper')) {
            colapsarDesc(_descWrapperActivo);
        }

        var wrapper = previewEl.closest('.desc-wrapper');
        var hidden = wrapper.querySelector('.desc-hidden');
        var panel = document.getElementById('desc-panel-global');
        var textarea = document.getElementById('desc-textarea-global');

        _descWrapperActivo = wrapper;
        textarea.value = hidden ? hidden.value : '';

        var rect = previewEl.getBoundingClientRect();
        panel.style.top = (rect.bottom + window.scrollY + 4) + 'px';
        panel.style.left = rect.left + 'px';
        panel.style.minWidth = Math.max(rect.width, 320) + 'px';
        panel.style.display = 'block';

        previewEl.style.visibility = 'hidden';
        textarea.focus();
    };

    function colapsarDesc(wrapper) {
        if (!wrapper) return;
        var preview = wrapper.querySelector('.desc-preview');
        var hidden = wrapper.querySelector('.desc-hidden');
        var span = wrapper.querySelector('.desc-texto-preview');
        var panel = document.getElementById('desc-panel-global');
        var textarea = document.getElementById('desc-textarea-global');

        var valor = textarea ? textarea.value.trim() : '';
        if (hidden) hidden.value = valor;

        if (span) {
            if (valor) {
                span.textContent = valor.length > 40 ? valor.substring(0, 40) + '…' : valor;
                span.classList.add('tiene-texto');
            } else {
                span.textContent = 'Sin descripción...';
                span.classList.remove('tiene-texto');
            }
        }

        if (panel) panel.style.display = 'none';
        if (preview) preview.style.visibility = '';
        _descWrapperActivo = null;
    }

    $(document).on('mousedown', function (e) {
        if (!_descWrapperActivo) return;
        var panel = document.getElementById('desc-panel-global');
        if (!_descWrapperActivo.contains(e.target) && panel && !panel.contains(e.target)) {
            colapsarDesc(_descWrapperActivo);
        }
    });

    $('form').on('submit', function (e) {
        var valido = true;
        var mensajes = [];

        if ($('#tablaArticulos tbody tr').length === 0) {
            valido = false;
            mensajes.push('Debe agregar al menos un artículo.');
        }
        $('#tablaArticulos tbody tr').each(function () {
            var select = $(this).find('.select-articulo');
            var cantidad = $(this).find('.cantidad-input');
            if (!select.val()) {
                valido = false;
                mensajes.push('Debe seleccionar un artículo en todas las filas.');
                return false;
            }
            if (!cantidad.val() || parseFloat(cantidad.val()) <= 0) {
                valido = false;
                mensajes.push('La cantidad debe ser mayor a cero.');
                return false;
            }
        });

        if (!valido) {
            e.preventDefault();
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: 'Campos incompletos',
                    html: '<ul style="text-align:left;">' + mensajes.map(function (m) { return '<li>' + m + '</li>'; }).join('') + '</ul>',
                    confirmButtonText: 'Entendido'
                });
            }
            return;
        }

        e.preventDefault();
        var form = e.target;
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: '¿Seguro que quieres guardar?',
                text: "Se guardarán los datos de la requisición.",
                icon: 'question',
                iconColor: 'var(--rosa-400)',
                showCancelButton: true,
                confirmButtonColor: 'var(--rosa-400)',
                cancelButtonColor: 'var(--slate-500)',
                confirmButtonText: 'Sí, guardar',
                cancelButtonText: 'No, cancelar'
            }).then((result) => {
                if (result.isConfirmed) {
                    var loader = document.getElementById('page-loader');
                    if (loader) loader.classList.remove('oculto');
                    form.submit();
                }
            });
        } else {
            form.submit();
        }
    });
})();
