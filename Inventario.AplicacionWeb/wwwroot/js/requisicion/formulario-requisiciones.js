(function () {
    var container = document.querySelector('[data-url-buscar-articulos]');
    var urlBuscarArticulos = container ? container.getAttribute('data-url-buscar-articulos') : '';
    var urlObtenerInfoArticulo = container ? container.getAttribute('data-url-obtener-info-articulo') : '';
    var esMensual = container ? container.getAttribute('data-tipo-requisicion') === 'mensual' : false;
    var esServicio = container ? container.getAttribute('data-tipo-requisicion') === 'servicio' : false;
    var urlBuscarCogs = container ? container.getAttribute("data-url-buscar-cogs") : "";

    var contadorArticulos = 0;
    let archivosSeleccionados = [];

    // Tipos permitidos
    const tiposPermitidos = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp', 'image/bmp'];

    $(document).ready(function () {
        if (window.articulosIniciales && window.articulosIniciales.length > 0) {
            window.articulosIniciales.forEach(function (art) {
                cargarArticuloExistente(art);
            });
        } else {
            agregarArticulo();
        }

        if (esServicio) {

            var elemento = document.getElementById('fechaServicio');

            if (elemento && typeof flatpickr !== 'undefined') {

                flatpickr(elemento, {
                    locale: 'es',
                    dateFormat: 'Y-m-d',
                    altInput: true,
                    altFormat: 'd / m / Y',
                    defaultDate: elemento.value || null,
                    allowInput: false,
                    disableMobile: true
                });
            }

            if (typeof $.fn.select2 !== 'undefined') {
                $('.select2-tipo-servicio').select2({
                    placeholder: '-- Seleccionar tipo --',
                    allowClear: true
                });
            }

            $('#tipoServicio').on('select2:select select2:clear change', function () {
                const seccionFotos = document.getElementById('seccionFotos');
                const valor = $(this).val();

                if (valor === 'Imprenta') {
                    seccionFotos.style.display = 'block';
                } else {
                    seccionFotos.style.display = 'none';
                    archivosSeleccionados = [];
                    actualizarInput();
                    document.getElementById('previsualizacionFotos').innerHTML = '';
                }
            });

            if ($('#tipoServicio').val() === 'Servicio Impresion') {
                document.getElementById('seccionFotos').style.display = 'block';
            }
        }
    });    

    var inputFotos = document.getElementById('inputFotos');
    if (inputFotos) {
        inputFotos.addEventListener('change', function () {
            Array.from(this.files).forEach(file => {
                const yaExiste = archivosSeleccionados.find(f => f.name === file.name && f.size === file.size);
                if (!yaExiste && tiposPermitidos.includes(file.type)) {
                    archivosSeleccionados.push(file);
                }
            });
            actualizarInput();
            renderizarPrevisualizacion();
        });
    }

    function actualizarInput() {
        var input = document.getElementById('inputFotos');
        if (!input) return;
        const dt = new DataTransfer();
        archivosSeleccionados.forEach(file => dt.items.add(file));
        input.files = dt.files;
    }

    function renderizarPrevisualizacion() {
        const contenedor = document.getElementById('previsualizacionFotos');
        contenedor.innerHTML = '';

        archivosSeleccionados.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const wrapper = document.createElement('div');
                wrapper.style.cssText = 'position:relative; display:inline-block;';

                const img = document.createElement('img');
                img.src = e.target.result;
                img.style.cssText = 'width:80px; height:80px; object-fit:cover; border-radius:6px; border:1px solid #ccc;';

                // Nombre del archivo debajo
                const nombre = document.createElement('div');
                nombre.textContent = file.name.length > 12 ? file.name.substring(0, 10) + '…' : file.name;
                nombre.style.cssText = 'font-size:10px; text-align:center; max-width:80px; word-break:break-all; color:#555;';

                const btnEliminar = document.createElement('span');
                btnEliminar.textContent = '✕';
                btnEliminar.style.cssText = `
                position:absolute; top:-5px; right:-5px;
                background:red; color:white; border-radius:50%;
                width:18px; height:18px; font-size:11px;
                display:flex; align-items:center; justify-content:center;
                cursor:pointer;
            `;

                //elimina del array real y re-renderiza
                btnEliminar.addEventListener('click', function () {
                    archivosSeleccionados.splice(index, 1);
                    actualizarInput();
                    renderizarPrevisualizacion();
                });

                wrapper.appendChild(img);
                wrapper.appendChild(nombre);
                wrapper.appendChild(btnEliminar);
                contenedor.appendChild(wrapper);
            };
            reader.readAsDataURL(file);
        });
    }

    function cargarArticuloExistente(art) {
        var index = contadorArticulos++;

        var fila = [
            '<tr data-index="', index, '">',
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
        $('#tablaArticulos tbody tr').each(function (nuevoIndex) {
            $(this).find('[name]').each(function () {
                var name = $(this).attr('name');
                if (name && name.startsWith('Articulos[')) {
                    $(this).attr('name', name.replace(/Articulos\[\d+\]/, 'Articulos[' + nuevoIndex + ']'));
                }
            });
        });

        var valido = true;
        var mensajes = [];

        if ($('#tablaArticulos tbody tr').length === 0) {
            valido = false;
            mensajes.push('Debe agregar al menos un artículo.');
        }
        $('#tablaArticulos tbody tr').each(function () {
            var select = $(this).find('.select-articulo');
            var cantidad = $(this).find('.cantidad-input');
            var descripcionDetallada = $(this).find('.desc-hidden');
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
            if (!descripcionDetallada.val() || descripcionDetallada.val().trim() === '') {
                valido = false;
                mensajes.push('La descripción detallada es obligatoria para todos los artículos.');
                return false;
            }
        });

        if (esServicio) {
            var tipoServicio = $('[name="TipoServicio"]').val();
            var fechaServicio = $('[name="FechaServicio"]').val();

            if (!tipoServicio) {
                valido = false;
                mensajes.push('Debe seleccionar un tipo de servicio.');
            }
            if (!fechaServicio) {
                valido = false;
                mensajes.push('Debe seleccionar la fecha de prestación del servicio.');
            }
        }

        if (!valido) {
            e.preventDefault();
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: 'Campos incompletos',
                    html: '<ul style="text-align:left;">' + mensajes.map(function (m) { return '<li>' + m + '</li>'; }).join('') + '</ul>',
                    confirmButtonText: 'Entendido',
                    confirmButtonColor: 'var(--rosa-400)'
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

    window.eliminarFotoExistente = function (idFoto) {
        Swal.fire({
            title: '¿Eliminar esta foto?',
            text: 'Esta acción no se puede deshacer.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#e53e3e',
            cancelButtonColor: 'var(--slate-500)',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then(function (result) {
            if (result.isConfirmed) {
                $.post('/Servicios/EliminarFoto', { idFoto: idFoto }, function (res) {
                    if (res.success) {
                        // Quitar el wrapper de la foto de la vista
                        var wrapper = document.getElementById('foto-wrapper-' + idFoto);
                        if (wrapper) wrapper.remove();

                        // Si ya no quedan fotos existentes, ocultar el contenedor
                        var fotosRestantes = document.querySelectorAll('.foto-existente-wrapper');
                        if (fotosRestantes.length === 0) {
                            var contenedor = document.getElementById('fotosExistentes');
                            if (contenedor) contenedor.closest('div').remove();
                        }

                        Swal.fire({
                            icon: 'success',
                            title: 'Foto eliminada',
                            timer: 1500,
                            showConfirmButton: false
                        });
                    } else {
                        Swal.fire({ icon: 'error', title: 'No se pudo eliminar la foto' });
                    }
                });
            }
        });
    };
})();