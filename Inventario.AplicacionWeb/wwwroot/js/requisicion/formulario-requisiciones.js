(function () {
    var container = document.querySelector('[data-url-buscar-articulos]');
    var urlBuscarArticulos = container ? container.getAttribute('data-url-buscar-articulos') : '';
    var urlObtenerInfoArticulo = container ? container.getAttribute('data-url-obtener-info-articulo') : '';
    var esMensual = container ? container.getAttribute('data-tipo-requisicion') === 'mensual' : false;
    var esServicio = container ? container.getAttribute('data-tipo-requisicion') === 'servicio' : false;
    var flujoContinuar = !!document.getElementById('btnContinuar');
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
                img.style.cssText = 'width:80px; height:80px; object-fit:cover; border-radius:6px; border:1px solid #ccc; cursor:pointer;';
                img.onclick = function() {
                    if (window.abrirVisorImagen) window.abrirVisorImagen(e.target.result);
                };

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

    function reindexarArticulos() {
        $('#tablaArticulos tbody tr').each(function (nuevoIndex) {
            $(this).find('[name]').each(function () {
                var name = $(this).attr('name');
                if (name && name.startsWith('Articulos[')) {
                    $(this).attr('name', name.replace(/Articulos\[\d+\]/, 'Articulos[' + nuevoIndex + ']'));
                }
            });
        });
    }

    function validarFormulario() {
        reindexarArticulos();
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
            if (!tipoServicio) { valido = false; mensajes.push('Debe seleccionar un tipo de servicio.'); }
            if (!fechaServicio) { valido = false; mensajes.push('Debe seleccionar la fecha de prestación del servicio.'); }
        }

        return { valido: valido, mensajes: mensajes };
    }

    function mostrarErroresValidacion(mensajes) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: 'Campos incompletos',
                html: '<ul style="text-align:left;">' + mensajes.map(function (m) { return '<li>' + m + '</li>'; }).join('') + '</ul>',
                confirmButtonText: 'Entendido',
                confirmButtonColor: 'var(--rosa-400)'
            });
        }
    }

    function enviarFormulario() {
        var loader = document.getElementById('page-loader');
        if (loader) loader.classList.remove('oculto');
        document.querySelector('form').submit();
    }

    $('form').on('submit', function (e) {
        var resultado = validarFormulario();

        if (!resultado.valido) {
            e.preventDefault();
            mostrarErroresValidacion(resultado.mensajes);
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
            }).then(function (result) {
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

    // ═══════════════════════════════════════════
    //  WIZARD DE PROGRAMACIÓN (flujo "Continuar")
    // ═══════════════════════════════════════════

    if (flujoContinuar) {
        var btnContinuar = document.getElementById('btnContinuar');
        var btnGuardarFinal = document.getElementById('btnGuardarFinal');

        if (btnContinuar) {
            btnContinuar.addEventListener('click', function () {
                var resultado = validarFormulario();
                if (!resultado.valido) {
                    mostrarErroresValidacion(resultado.mensajes);
                    return;
                }

                Swal.fire({
                    title: '¿Programar requisición?',
                    icon: 'question',
                    iconColor: 'var(--rosa-400)',
                    showDenyButton: true,
                    showCancelButton: false,
                    confirmButtonText: 'Sí',
                    denyButtonText: 'No',
                    confirmButtonColor: 'var(--rosa-400)',
                    denyButtonColor: 'var(--slate-500)'
                }).then(function (result) {
                    if (result.isConfirmed) {
                        iniciarWizardProgramacion();
                    } else if (result.isDenied) {
                        Swal.fire({
                            title: 'Se guardará la requisición',
                            text: 'La requisición se guardará sin programación.',
                            icon: 'info',
                            iconColor: 'var(--rosa-400)',
                            confirmButtonText: 'Aceptar',
                            confirmButtonColor: 'var(--rosa-400)'
                        }).then(function (r) {
                            if (r.isConfirmed) enviarFormulario();
                        });
                    }
                });
            });
        }
    }

    function obtenerSnapshotArticulos() {
        var articulos = [];
        $('#tablaArticulos tbody tr').each(function (i) {
            var $row = $(this);
            var idx = $row.data('index');
            articulos.push({
                numero: i + 1,
                idArticulo: $row.find('.select-articulo').val(),
                descripcionDetallada: $row.find('.desc-hidden').val() || 'Sin descripción',
                unidadMedida: $row.find('.unidad-hidden-' + idx).val() || $row.find('.unidad-' + idx).text().trim() || '-',
                cantidad: parseFloat($row.find('.cantidad-input').val()) || 0
            });
        });
        return articulos;
    }

    function iniciarWizardProgramacion() {
        var articulos = obtenerSnapshotArticulos();
        if (articulos.length === 0) return;
        mostrarPasoArticulo({
            articulos: articulos,
            currentIndex: 0,
            lastTipo: null,
            lastWasChanged: false
        });
    }

    function mostrarPasoArticulo(state) {
        var art = state.articulos[state.currentIndex];
        var esUltimo = state.currentIndex === state.articulos.length - 1;
        var preseleccionado = (state.lastTipo && !state.lastWasChanged) ? state.lastTipo : null;
        var html = construirHtmlPaso(art, preseleccionado);

        Swal.fire({
            title: 'Artículo ' + art.numero + ' de ' + state.articulos.length,
            html: html,
            width: '95%',
            customClass: { popup: 'wizard-popup' },
            confirmButtonText: esUltimo
                ? '<i class="fa-solid fa-check"></i> Finalizar'
                : 'Siguiente <i class="fa-solid fa-arrow-right"></i>',
            confirmButtonColor: 'var(--rosa-400)',
            showCancelButton: true,
            cancelButtonText: 'Cancelar',
            cancelButtonColor: 'var(--slate-500)',
            allowOutsideClick: false,
            didOpen: function () {
                configurarEventosPaso(art);
            },
            preConfirm: function () {
                var tipoSel = document.querySelector('input[name="wizardTipo"]:checked');
                if (!tipoSel) {
                    Swal.showValidationMessage('Debe seleccionar Mensual o Anual');
                    return false;
                }
                var total = calcularTotalPaso();
                if (Math.abs(total - art.cantidad) > 0.01) {
                    Swal.showValidationMessage(
                        'El total (' + total + ') debe ser igual a la cantidad requerida (' + art.cantidad + ')'
                    );
                    return false;
                }
                return { tipo: tipoSel.value };
            }
        }).then(function (result) {
            if (result.isConfirmed) {
                var chosen = result.value.tipo;
                state.lastWasChanged = preseleccionado !== null && chosen !== preseleccionado;
                state.lastTipo = chosen;
                state.currentIndex++;
                if (state.currentIndex < state.articulos.length) {
                    mostrarPasoArticulo(state);
                } else {
                    finalizarWizard();
                }
            }
        });
    }

    function construirHtmlPaso(art, preseleccionado) {
        var h = '<div class="wizard-paso">';

        h += '<div class="wizard-tipo-grupo">';
        h += '<label class="wizard-tipo-btn' + (preseleccionado === 'mensual' ? ' activo' : '') + '">';
        h += '<input type="radio" name="wizardTipo" value="mensual"' + (preseleccionado === 'mensual' ? ' checked' : '') + '>';
        h += '<span>Mensual</span></label>';
        h += '<label class="wizard-tipo-btn' + (preseleccionado === 'anual' ? ' activo' : '') + '">';
        h += '<input type="radio" name="wizardTipo" value="anual"' + (preseleccionado === 'anual' ? ' checked' : '') + '>';
        h += '<span>Anual</span></label>';
        h += '</div>';

        h += '<div class="wizard-info-cantidad">Cantidad requerida: <strong>' + art.cantidad + '</strong></div>';

        h += '<div id="wizardTablaContainer" data-cantidad-requerida="' + art.cantidad + '">';
        if (preseleccionado) {
            h += generarTablaDistribucion(art, preseleccionado);
        } else {
            h += '<p class="wizard-placeholder">Seleccione el tipo de distribución para continuar</p>';
        }
        h += '</div>';

        h += '</div>';
        return h;
    }

    function generarTablaDistribucion(art, tipo) {
        var encabezados, claves;
        if (tipo === 'mensual') {
            encabezados = ['Sem 1', 'Sem 2', 'Sem 3', 'Sem 4'];
            claves = ['sem1', 'sem2', 'sem3', 'sem4'];
        } else {
            encabezados = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
            claves = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
        }

        var h = '<div class="wizard-tabla-scroll"><table class="wizard-tabla">';
        h += '<thead><tr><th>No.</th><th>Descripción Detallada</th><th>Unidad</th>';
        for (var i = 0; i < encabezados.length; i++) h += '<th>' + encabezados[i] + '</th>';
        h += '<th>Total</th></tr></thead>';

        var descCorta = art.descripcionDetallada.length > 80
            ? art.descripcionDetallada.substring(0, 80) + '…'
            : art.descripcionDetallada;

        h += '<tbody><tr>';
        h += '<td>' + art.numero + '</td>';
        h += '<td class="wizard-desc-cell">' + escapeHtmlWizard(descCorta) + '</td>';
        h += '<td>' + escapeHtmlWizard(art.unidadMedida) + '</td>';
        for (var j = 0; j < claves.length; j++) {
            h += '<td><input type="number" class="wizard-periodo-input" data-clave="' + claves[j] + '" min="0" step="1" value="0"></td>';
        }
        h += '<td class="wizard-total-cell"><strong>0 / ' + art.cantidad + '</strong></td>';
        h += '</tr></tbody></table></div>';
        return h;
    }

    function escapeHtmlWizard(str) {
        if (!str) return '';
        var d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    function configurarEventosPaso(art) {
        document.querySelectorAll('input[name="wizardTipo"]').forEach(function (radio) {
            radio.addEventListener('change', function () {
                document.querySelectorAll('.wizard-tipo-btn').forEach(function (btn) {
                    btn.classList.remove('activo');
                });
                this.closest('.wizard-tipo-btn').classList.add('activo');
                actualizarTablaPaso(art, this.value);
            });
        });
        document.querySelectorAll('.wizard-periodo-input').forEach(function (input) {
            input.addEventListener('input', calcularYMostrarTotal);
        });
    }

    function actualizarTablaPaso(art, tipo) {
        var cont = document.getElementById('wizardTablaContainer');
        if (!cont) return;
        cont.setAttribute('data-cantidad-requerida', art.cantidad);
        cont.innerHTML = generarTablaDistribucion(art, tipo);
        cont.querySelectorAll('.wizard-periodo-input').forEach(function (input) {
            input.addEventListener('input', calcularYMostrarTotal);
        });
    }

    function calcularTotalPaso() {
        var total = 0;
        document.querySelectorAll('.wizard-periodo-input').forEach(function (input) {
            total += parseFloat(input.value) || 0;
        });
        return total;
    }

    function calcularYMostrarTotal() {
        var total = calcularTotalPaso();
        var cont = document.getElementById('wizardTablaContainer');
        var requerida = cont ? parseFloat(cont.getAttribute('data-cantidad-requerida')) || 0 : 0;
        var celda = document.querySelector('.wizard-total-cell strong');
        if (celda) {
            celda.textContent = total + ' / ' + requerida;
            celda.style.color = Math.abs(total - requerida) < 0.01 ? '#22c55e' : '#ef4444';
        }
    }

    function finalizarWizard() {
        var btnC = document.getElementById('btnContinuar');
        var btnG = document.getElementById('btnGuardarFinal');
        if (btnC) btnC.style.display = 'none';
        if (btnG) btnG.style.display = '';

        Swal.fire({
            title: 'Programación completada',
            text: 'Todos los artículos han sido distribuidos. Presione "Crear Requisición" para guardar.',
            icon: 'success',
            iconColor: 'var(--rosa-400)',
            confirmButtonText: 'Entendido',
            confirmButtonColor: 'var(--rosa-400)'
        });
    }

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

    // --- Lógica del Lightbox de Imágenes ---
    window.abrirVisorImagen = function (src) {
        var overlay = document.getElementById('visor-imagenes-global');
        var img = document.getElementById('visor-imagenes-img');
        if (overlay && img) {
            img.src = src;
            overlay.classList.add('activo');
        }
    };

    window.cerrarVisorImagen = function () {
        var overlay = document.getElementById('visor-imagenes-global');
        if (overlay) {
            overlay.classList.remove('activo');
            setTimeout(function () {
                var img = document.getElementById('visor-imagenes-img');
                // vaciar el src solo si no se volvió a abrir
                if (img && !overlay.classList.contains('activo')) {
                    img.src = '';
                }
            }, 300); // 300ms debe coincidir con la transición css
        }
    };
})();