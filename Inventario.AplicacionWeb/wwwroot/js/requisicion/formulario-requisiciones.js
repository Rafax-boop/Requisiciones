(function () {
  var container = document.querySelector("[data-url-buscar-articulos]");
  var urlBuscarArticulos = container
    ? container.getAttribute("data-url-buscar-articulos")
    : "";
  var urlObtenerInfoArticulo = container
    ? container.getAttribute("data-url-obtener-info-articulo")
    : "";
  var tipoRequisicion = container
    ? container.getAttribute("data-tipo-requisicion")
    : "normal";
  var esServicio = tipoRequisicion === "servicio";
  var flujoContinuar = !!document.getElementById("btnContinuar");
  var urlBuscarCogs = container
    ? container.getAttribute("data-url-buscar-cogs")
    : "";
  var unidadesMedidaOpciones = [
    "PIEZA",
    "ROLLO",
    "PAQUETE",
    "LITRO",
    "KILO",
    "BULTO",
  ];

  var contadorArticulos = 0;
  var programacionWizard = {};
  let archivosSeleccionados = [];
  var wizardSeleccionarEnfoquePorTab = false;
  var programacionFinalizada = false;
  var selectorRosaDisponible =
    !!window.SelectRosaBuscable &&
    typeof window.SelectRosaBuscable.inicializar === "function";

  // Tipos permitidos
  const tiposPermitidos = [
    "image/jpeg",
    "image/jpg",
    "image/png",
    "image/gif",
    "image/webp",
    "image/bmp",
  ];

  $(document).ready(function () {
    inicializarSelectsFormulario();

    if (window.articulosIniciales && window.articulosIniciales.length > 0) {
      window.articulosIniciales.forEach(function (art) {
        cargarArticuloExistente(art);
      });
    } else {
      agregarArticulo();
    }

    if (esServicio) {
      var elemento = document.getElementById("fechaServicio");

      if (elemento && typeof flatpickr !== "undefined") {
        flatpickr(elemento, {
          locale: "es",
          dateFormat: "Y-m-d",
          altInput: true,
          altFormat: "d / m / Y",
          defaultDate: elemento.value || null,
          allowInput: false,
          disableMobile: true,
        });
      }

      $("#tipoServicio").on("change", function () {
        const seccionFotos = document.getElementById("seccionFotos");
        const valor = $(this).val();

        if (valor === "Servicio Impresion") {
          seccionFotos.style.display = "block";
        } else {
          seccionFotos.style.display = "none";
          archivosSeleccionados = [];
          actualizarInput();
          document.getElementById("previsualizacionFotos").innerHTML = "";
        }
      });

      if ($("#tipoServicio").val() === "Servicio Impresion") {
        document.getElementById("seccionFotos").style.display = "block";
      }
    }
  });

  function inicializarSelectsFormulario() {
    var $tipoServicio = $("#tipoServicio");
    if ($tipoServicio.length) {
      $tipoServicio.select2({
        width: "100%",
        placeholder: esServicio
          ? "Buscar tipo de servicio..."
          : "Buscar lugar de entrega...",
        allowClear: true,
      });
    }
  }

  var inputFotos = document.getElementById("inputFotos");
  if (inputFotos) {
    inputFotos.addEventListener("change", function () {
      Array.from(this.files).forEach((file) => {
        const yaExiste = archivosSeleccionados.find(
          (f) => f.name === file.name && f.size === file.size,
        );
        if (!yaExiste && tiposPermitidos.includes(file.type)) {
          archivosSeleccionados.push(file);
        }
      });
      actualizarInput();
      renderizarPrevisualizacion();
    });
  }

  function actualizarInput() {
    var input = document.getElementById("inputFotos");
    if (!input) return;
    const dt = new DataTransfer();
    archivosSeleccionados.forEach((file) => dt.items.add(file));
    input.files = dt.files;
  }

  function renderizarPrevisualizacion() {
    const contenedor = document.getElementById("previsualizacionFotos");
    contenedor.innerHTML = "";

    archivosSeleccionados.forEach((file, index) => {
      const reader = new FileReader();
      reader.onload = function (e) {
        const wrapper = document.createElement("div");
        wrapper.style.cssText = "position:relative; display:inline-block;";

        const img = document.createElement("img");
        img.src = e.target.result;
        img.classList.add("modal-galeria-foto-thumb");
        img.style.width = "80px";
        img.style.height = "80px";
        img.onclick = function () {
          if (typeof window.abrirVisorImagenTabla === "function") {
            window.abrirVisorImagenTabla(e.target.result);
          } else if (window.abrirVisorImagen) {
            window.abrirVisorImagen(e.target.result);
          }
        };

        // Nombre del archivo debajo
        const nombre = document.createElement("div");
        nombre.textContent =
          file.name.length > 12 ? file.name.substring(0, 10) + "…" : file.name;
        nombre.style.cssText =
          "font-size:10px; text-align:center; max-width:80px; word-break:break-all; color:#555;";

        const btnEliminar = document.createElement("span");
        btnEliminar.textContent = "✕";
        btnEliminar.style.cssText = `
                position:absolute; top:-5px; right:-5px;
                background:red; color:white; border-radius:50%;
                width:18px; height:18px; font-size:11px;
                display:flex; align-items:center; justify-content:center;
                cursor:pointer;
            `;

        //elimina del array real y re-renderiza
        btnEliminar.addEventListener("click", function () {
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
    var unidadMedida = (art.unidadMedida || "").toString().trim().toLowerCase();

    var fila = [
      '<tr data-index="',
      index,
      '">',
      "<td>",
      '<select name="Articulos[' +
        index +
        '].IdArticulo" class="form-select select-articulo" data-index="' +
        index +
        '" style="width: 240px;" required></select>',
      '<input type="hidden" name="Articulos[' +
        index +
        '].Descripcion" class="descripcion-hidden-' +
        index +
        '" value="' +
        (art.descripcion || "") +
        '" />',
      '<input type="hidden" name="Articulos[' +
        index +
        '].Cog" class="cog-hidden-' +
        index +
        '" value="' +
        (art.cog || "") +
        '" />',
      '<input type="hidden" name="Articulos[' +
        index +
        '].ClaveMaterial" class="clave-hidden-' +
        index +
        '" />',
      "</td>",
      "<td>",
      construirSelectUnidad(index, unidadMedida, !art.idArticulo),
      "</td>",
      '<td><input type="number" name="Articulos[' +
        index +
        '].Cantidad" class="form-control cantidad-input" min="1" step="1" inputmode="numeric" value="' +
        (art.cantidad || 1) +
        '" required /></td>',
      '<td class="descripcion-detallada-cell"><div class="desc-wrapper">',
      '<div class="desc-preview" onclick="expandirDesc(this)"><span class="desc-texto-preview' +
        (art.descripcionDetallada ? " tiene-texto" : "") +
        '">' +
        (art.descripcionDetallada
          ? art.descripcionDetallada.length > 40
            ? art.descripcionDetallada.substring(0, 40) + "…"
            : art.descripcionDetallada
          : "Sin descripción...") +
        '</span><i class="fa-solid fa-pen-to-square desc-icon"></i></div>',
      '<input type="hidden" name="Articulos[' +
        index +
        '].DescripcionDetallada" class="desc-hidden" value="' +
        (art.descripcionDetallada || "") +
        '" />',
      "</div></td>",
      '<td class="text-center"><button type="button" class="btn btn-danger btn-sm" onclick="eliminarArticulo(this)"><i class="fa-solid fa-circle-minus"></i></button></td>',
      "</tr>",
    ].join("");

    $("#tablaArticulos tbody").append(fila);

    var $select = $('.select-articulo[data-index="' + index + '"]');
    inicializarSelect2($select);
    inicializarSelectUnidad($('.select-unidad[data-index="' + index + '"]'));

    if (art.idArticulo && art.descripcion) {
      var option = new Option(art.descripcion, art.idArticulo, true, true);
      $select.append(option);

      // Llenar celdas y hiddens directamente con los datos que ya tienes
      $(".descripcion-hidden-" + index).val(art.descripcion || "");
      $(".cog-hidden-" + index).val(art.cog || "");
      $(".clave-hidden-" + index).val(art.claveMaterial || "");
      establecerUnidadFila(index, unidadMedida);

      // Trigger SOLO para que Select2 muestre la opción seleccionada visualmente
      // pero necesitamos evitar que el evento change dispare el AJAX
      $select.trigger("change");
    }
  }

  window.agregarArticulo = function () {
    if (programacionFinalizada) return;
    var index = contadorArticulos++;

    var fila = [
      '<tr data-index="',
      index,
      '">',
      "<td>",
      '<select name="Articulos[' +
        index +
        '].IdArticulo" class="form-select select-articulo" data-index="' +
        index +
        '" style="width: 240px;" required></select>',
      '<input type="hidden" name="Articulos[' +
        index +
        '].Descripcion" class="descripcion-hidden-' +
        index +
        '" />',
      '<input type="hidden" name="Articulos[' +
        index +
        '].Cog" class="cog-hidden-' +
        index +
        '" />',
      '<input type="hidden" name="Articulos[' +
        index +
        '].ClaveMaterial" class="clave-hidden-' +
        index +
        '" />',
      "</td>",
      "<td>",
      construirSelectUnidad(index, "", true),
      "</td>",
      '<td><input type="number" name="Articulos[' +
        index +
        '].Cantidad" class="form-control cantidad-input" min="1" step="1" inputmode="numeric" value="1" required /></td>',
      '<td class="descripcion-detallada-cell"><div class="desc-wrapper">',
      '<div class="desc-preview" onclick="expandirDesc(this)"><span class="desc-texto-preview">Sin descripción...</span><i class="fa-solid fa-pen-to-square desc-icon"></i></div>',
      '<input type="hidden" name="Articulos[' +
        index +
        '].DescripcionDetallada" class="desc-hidden" />',
      "</div></td>",
      '<td class="text-center"><button type="button" class="btn btn-danger btn-sm" onclick="eliminarArticulo(this)"><i class="fa-solid fa-circle-minus"></i></button></td>',
      "</tr>",
    ].join("");

    $("#tablaArticulos tbody").append(fila);
    inicializarSelect2($('.select-articulo[data-index="' + index + '"]'));
    inicializarSelectUnidad($('.select-unidad[data-index="' + index + '"]'));
  };

  window.eliminarArticulo = function (btn) {
    if (programacionFinalizada) return;
    $(btn).closest("tr").remove();
  };

  function inicializarSelect2($elemento) {
    $elemento.select2({
      width: "resolve",
      placeholder: "Buscar artículo...",
      minimumInputLength: 2,
      language: "es",
      ajax: {
        url: urlBuscarArticulos,
        dataType: "json",
        delay: 250,
        data: function (params) {
          return { term: params.term, tipo: tipoRequisicion };
        },
        processResults: function (data) {
          return { results: data };
        },
        cache: true,
      },
    });
  }

  function construirSelectUnidad(index, valorSeleccionado, deshabilitado) {
    var opciones = ['<option value=""></option>'];
    unidadesMedidaOpciones.forEach(function (unidad) {
      var selected = valorSeleccionado === unidad ? " selected" : "";
      opciones.push(
        '<option value="' +
          unidad +
          '"' +
          selected +
          ">" +
          unidad +
          "</option>",
      );
    });

    return (
      '<select name="Articulos[' +
      index +
      '].UnidadMedida" class="form-select select-unidad" data-index="' +
      index +
      '" style="width: 100%;"' +
      (deshabilitado ? " disabled" : "") +
      " required>" +
      opciones.join("") +
      "</select>"
    );
  }

  function inicializarSelectUnidad($elemento) {
    if (!$elemento.length) return;

    $elemento.select2({
      width: "100%",
      placeholder: "Selecciona unidad...",
      allowClear: true,
    });
  }

  function establecerUnidadFila(index, unidad) {
    var unidadNormalizada = (unidad || "").toString().trim().toLowerCase();
    var $selectUnidad = $('.select-unidad[data-index="' + index + '"]');
    if (!$selectUnidad.length) return;

    $selectUnidad.prop("disabled", false);
    $selectUnidad.val(
      unidadesMedidaOpciones.indexOf(unidadNormalizada) >= 0
        ? unidadNormalizada
        : "",
    );
    $selectUnidad.trigger("change");
  }

  function limpiarUnidadFila(index) {
    var $selectUnidad = $('.select-unidad[data-index="' + index + '"]');
    if (!$selectUnidad.length) return;

    $selectUnidad.val("").trigger("change");
    $selectUnidad.prop("disabled", true);
  }

  $("#tablaArticulos").on("change", ".select-articulo", function () {
    var select = $(this);
    var index = select.data("index");
    var idArticulo = select.val();

    if (idArticulo && urlObtenerInfoArticulo) {
      $.get(urlObtenerInfoArticulo, { id: idArticulo }, function (data) {
        $(".descripcion-hidden-" + index).val(data.descripcion);
        $(".cog-hidden-" + index).val(data.cog);
        $(".clave-hidden-" + index).val(data.clave);
        establecerUnidadFila(index, data.unidadMedida);
      });
    } else {
      $(".descripcion-hidden-" + index).val("");
      $(".cog-hidden-" + index).val("");
      $(".clave-hidden-" + index).val("");
      limpiarUnidadFila(index);
    }
  });

  $("#tablaArticulos").on("input", ".cantidad-input", function () {
    this.value = this.value.replace(/\D/g, "");
    var index = $(this).data("index");
    if (typeof calcularSubtotal === "function") calcularSubtotal(index);
  });

  $("#tablaArticulos").on("keydown", ".cantidad-input", function (e) {
    if (["e", "E", "+", "-", ".", ","].includes(e.key)) {
      e.preventDefault();
    }
  });

  var _descWrapperActivo = null;

  window.expandirDesc = function (previewEl) {
    if (programacionFinalizada) return;
    if (
      _descWrapperActivo &&
      _descWrapperActivo !== previewEl.closest(".desc-wrapper")
    ) {
      colapsarDesc(_descWrapperActivo);
    }

    var wrapper = previewEl.closest(".desc-wrapper");
    var hidden = wrapper.querySelector(".desc-hidden");
    var panel = document.getElementById("desc-panel-global");
    var textarea = document.getElementById("desc-textarea-global");

    _descWrapperActivo = wrapper;
    textarea.value = hidden ? hidden.value : "";

    var rect = previewEl.getBoundingClientRect();
    panel.style.top = rect.bottom + window.scrollY + 4 + "px";
    panel.style.left = rect.left + "px";
    panel.style.minWidth = Math.max(rect.width, 320) + "px";
    panel.style.display = "block";

    previewEl.style.visibility = "hidden";
    textarea.focus();
  };

  function colapsarDesc(wrapper) {
    if (!wrapper) return;
    var preview = wrapper.querySelector(".desc-preview");
    var hidden = wrapper.querySelector(".desc-hidden");
    var span = wrapper.querySelector(".desc-texto-preview");
    var panel = document.getElementById("desc-panel-global");
    var textarea = document.getElementById("desc-textarea-global");

    var valor = textarea ? textarea.value.trim() : "";
    if (hidden) hidden.value = valor;

    if (span) {
      if (valor) {
        span.textContent =
          valor.length > 40 ? valor.substring(0, 40) + "…" : valor;
        span.classList.add("tiene-texto");
      } else {
        span.textContent = "Sin descripción...";
        span.classList.remove("tiene-texto");
      }
    }

    if (panel) panel.style.display = "none";
    if (preview) preview.style.visibility = "";
    _descWrapperActivo = null;
  }

  $(document).on("mousedown", function (e) {
    if (!_descWrapperActivo) return;
    var panel = document.getElementById("desc-panel-global");
    if (
      !_descWrapperActivo.contains(e.target) &&
      panel &&
      !panel.contains(e.target)
    ) {
      colapsarDesc(_descWrapperActivo);
    }
  });

  function reindexarArticulos() {
    $("#tablaArticulos tbody tr").each(function (nuevoIndex) {
      $(this)
        .find("[name]")
        .each(function () {
          var name = $(this).attr("name");
          if (name && name.startsWith("Articulos[")) {
            $(this).attr(
              "name",
              name.replace(/Articulos\[\d+\]/, "Articulos[" + nuevoIndex + "]"),
            );
          }
        });
    });
  }

  function validarFormulario() {
    reindexarArticulos();
    var valido = true;
    var mensajes = [];
    var mensajesUnicos = {};

    function agregarMensaje(msg) {
      if (!mensajesUnicos[msg]) {
        mensajesUnicos[msg] = true;
        mensajes.push(msg);
      }
    }

    if ($("#tablaArticulos tbody tr").length === 0) {
      valido = false;
      mensajes.push("Debe agregar al menos un artículo.");
    }
    $("#tablaArticulos tbody tr").each(function () {
      var select = $(this).find(".select-articulo");
      var unidad = $(this).find(".select-unidad");
      var cantidad = $(this).find(".cantidad-input");
      var descripcionDetallada = $(this).find(".desc-hidden");
      if (!select.val()) {
        valido = false;
        mensajes.push("Debe seleccionar un artículo en todas las filas.");
        return false;
      }
      if (!unidad.val()) {
        valido = false;
        mensajes.push(
          "Debe seleccionar la unidad de medida en todas las filas.",
        );
        return false;
      }
      if (!cantidad.val() || parseFloat(cantidad.val()) <= 0) {
        valido = false;
        mensajes.push("La cantidad debe ser mayor a cero.");
        return false;
      }
      if (
        !descripcionDetallada.val() ||
        descripcionDetallada.val().trim() === ""
      ) {
        valido = false;
        mensajes.push(
          "La descripción detallada es obligatoria para todos los artículos.",
        );
        return false;
      }
    });

    if (esServicio) {
      var tipoServicio = $('[name="TipoServicio"]').val();
      var fechaServicio = $('[name="FechaServicio"]').val();
      if (!tipoServicio) {
        valido = false;
        mensajes.push("Debe seleccionar un tipo de servicio.");
      }
      if (!fechaServicio) {
        valido = false;
        mensajes.push("Debe seleccionar la fecha de prestación del servicio.");
      }
    }

    var correo = ($('[name="Correo"]').val() || "").trim();
    var telefono = ($('[name="Telefono"]').val() || "").trim();
    var justificacion = ($('[name="Justificacion"]').val() || "").trim();
    var lugarEntrega = ($('[name="LugarEntrega"]').val() || "").trim();
    var mensajesDetallados = [];
    var mensajesDetalladosSet = {};

    function agregarDetalle(msg) {
      if (!mensajesDetalladosSet[msg]) {
        mensajesDetalladosSet[msg] = true;
        mensajesDetallados.push(msg);
      }
    }

    if (!correo) agregarDetalle("Falta capturar el correo.");
    if (!telefono) agregarDetalle("Falta capturar el teléfono.");
    if (!justificacion) {
      agregarDetalle(
        esServicio
          ? "Falta capturar la justificación del servicio solicitado."
          : "Falta capturar la justificación de la requisición.",
      );
    }
    if (!lugarEntrega) {
      agregarDetalle(
        esServicio
          ? "Falta capturar el lugar de prestación del servicio."
          : "Falta seleccionar el lugar de entrega.",
      );
    }

    if (!esServicio) {
      var usoEspecifico = ($('[name="UsoEspecifico"]').val() || "").trim();
      var cuentaPrograma = $(
        'input[name="CuentaProgramaPresupuestario"]:checked',
      ).val();
      var usoMaterial = $('input[name="UsoMaterial"]:checked').val();

      if (!usoEspecifico) {
        agregarDetalle("Falta capturar el uso específico del material.");
      }
      if (typeof cuentaPrograma === "undefined") {
        agregarDetalle(
          "Falta indicar si está programado en el presupuesto y programa de adquisiciones.",
        );
      }
      if (typeof usoMaterial === "undefined") {
        agregarDetalle(
          "Falta indicar si el uso del material es administrativo o donativo.",
        );
      }
    } else {
      if (!tipoServicio) agregarDetalle("Falta seleccionar el tipo de servicio.");
      if (!fechaServicio) {
        agregarDetalle("Falta seleccionar la fecha de prestación del servicio.");
      }
    }

    $("#tablaArticulos tbody tr").each(function (index) {
      var select = $(this).find(".select-articulo");
      var unidad = $(this).find(".select-unidad");
      var cantidad = $(this).find(".cantidad-input");
      var descripcionDetallada = $(this).find(".desc-hidden");
      var numeroPartida = index + 1;

      if (!select.val()) {
        agregarDetalle(
          "Partida " + numeroPartida + ": falta seleccionar el artículo.",
        );
      }
      if (!unidad.val()) {
        agregarDetalle(
          "Partida " + numeroPartida + ": falta seleccionar la unidad de medida.",
        );
      }
      if (!cantidad.val() || parseFloat(cantidad.val()) <= 0) {
        agregarDetalle(
          "Partida " + numeroPartida + ": la cantidad debe ser mayor a cero.",
        );
      }
      if (
        !descripcionDetallada.val() ||
        descripcionDetallada.val().trim() === ""
      ) {
        agregarDetalle(
          "Partida " + numeroPartida + ": falta capturar la descripción detallada.",
        );
      }
    });

    if (mensajesDetallados.length > 0) {
      valido = false;
      mensajes = mensajesDetallados;
    }

    return { valido: valido, mensajes: mensajes };
  }

  function mostrarErroresValidacion(mensajes) {
    if (typeof Swal !== "undefined") {
      Swal.fire({
        icon: "error",
        title: "Campos incompletos",
        html:
          '<ul style="text-align:left;">' +
          mensajes
            .map(function (m) {
              return "<li>" + m + "</li>";
            })
            .join("") +
          "</ul>",
        confirmButtonText: "Entendido",
        confirmButtonColor: "var(--rosa-400)",
      });
    }
  }

  function enviarFormulario() {
    var loader = document.getElementById("page-loader");
    if (loader) loader.classList.remove("oculto");
    document.querySelector("form").submit();
  }

  $("form").on("submit", function (e) {
    var resultado = validarFormulario();

    if (!resultado.valido) {
      e.preventDefault();
      mostrarErroresValidacion(resultado.mensajes);
      return;
    }

    e.preventDefault();
    var form = e.target;
    if (typeof Swal !== "undefined") {
      Swal.fire({
        title: "\u00bfSeguro que quieres guardar?",
        text: "Se guardar\u00e1n los datos de la requisici\u00f3n.",
        icon: "question",
        iconColor: "var(--rosa-400)",
        showCancelButton: true,
        confirmButtonColor: "var(--rosa-400)",
        cancelButtonColor: "var(--slate-500)",
        confirmButtonText: "S\u00ed, guardar",
        cancelButtonText: "No, cancelar",
      }).then(function (result) {
        if (result.isConfirmed) {
          var loader = document.getElementById("page-loader");
          if (loader) loader.classList.remove("oculto");
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
    var btnContinuar = document.getElementById("btnContinuar");
    var btnGuardarFinal = document.getElementById("btnGuardarFinal");

    if (btnContinuar) {
      btnContinuar.addEventListener("click", function () {
        var resultado = validarFormulario();
        if (!resultado.valido) {
          mostrarErroresValidacion(resultado.mensajes);
          return;
        }
        iniciarWizardMunicipios(function () {
          preguntarProgramacion();
        });
      });
    }
  }

  function preguntarProgramacion() {
    Swal.fire({
      title: "\u00bfProgramar requisici\u00f3n?",
      icon: "question",
      iconColor: "var(--rosa-400)",
      showDenyButton: true,
      showCancelButton: false,
      confirmButtonText: "S\u00ed",
      denyButtonText: "No",
      confirmButtonColor: "var(--rosa-400)",
      denyButtonColor: "var(--slate-500)",
    }).then(function (result) {
      if (result.isConfirmed) {
        iniciarWizardProgramacion();
      } else if (result.isDenied) {
        completarFlujoContinuar();
      }
    });
  }

  function obtenerSnapshotArticulos() {
    var articulos = [];
    $("#tablaArticulos tbody tr").each(function (i) {
      var $row = $(this);
      var idx = $row.data("index");
      articulos.push({
        numero: i + 1,
        idArticulo: $row.find(".select-articulo").val(),
        descripcionDetallada:
          $row.find(".desc-hidden").val() || "Sin descripción",
        unidadMedida:
          $row.find(".unidad-hidden-" + idx).val() ||
          $row
            .find(".unidad-" + idx)
            .text()
            .trim() ||
          "-",
        cantidad: parseFloat($row.find(".cantidad-input").val()) || 0,
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
      lastWasChanged: false,
      omitidos: 0,
    });
  }

  function mostrarPasoArticulo(state) {
    var art = state.articulos[state.currentIndex];
    var esUltimo = state.currentIndex === state.articulos.length - 1;
    var preseleccionado =
      state.lastTipo && !state.lastWasChanged ? state.lastTipo : null;
    var html = construirHtmlPaso(art, preseleccionado);

    Swal.fire({
      title: "Art\u00edculo " + art.numero + " de " + state.articulos.length,
      html: html,
      width: "95%",
      customClass: { popup: "wizard-popup" },
      confirmButtonText: esUltimo
        ? '<i class="fa-solid fa-check"></i> Finalizar'
        : 'Siguiente <i class="fa-solid fa-arrow-right"></i>',
      confirmButtonColor: "var(--rosa-400)",
      showDenyButton: true,
      denyButtonText:
        '<i class="fa-solid fa-forward"></i> Omitir art\u00edculo',
      denyButtonColor: "var(--slate-500)",
      showCancelButton: true,
      cancelButtonText: "Cancelar",
      cancelButtonColor: "var(--slate-500)",
      allowOutsideClick: false,
      didOpen: function () {
        configurarEventosPaso(art);
      },
      preConfirm: function () {
        var tipoSel = document.querySelector(
          'input[name="wizardTipo"]:checked',
        );
        if (!tipoSel) {
          Swal.showValidationMessage("Debe seleccionar Mensual o Anual");
          return false;
        }
        var mesVal = null;
        if (tipoSel.value === "mensual") {
          var selectMes = document.getElementById("wizardMesSelect");
          if (!selectMes || !selectMes.value) {
            Swal.showValidationMessage("Debe seleccionar el mes");
            return false;
          }
          mesVal = parseInt(selectMes.value);
        }
        var total = calcularTotalPaso();
        if (Math.abs(total - art.cantidad) > 0.01) {
          Swal.showValidationMessage(
            "El total (" +
              total +
              ") debe ser igual a la cantidad requerida (" +
              art.cantidad +
              ")",
          );
          return false;
        }
        return { tipo: tipoSel.value, mes: mesVal };
      },
    }).then(function (result) {
      if (result.isConfirmed) {
        var chosen = result.value.tipo;
        var chosenMes = result.value.mes;

        var $row = $("#tablaArticulos tbody tr").eq(state.currentIndex);
        var idArticulo = $row.find(".select-articulo").val();

        // ← guardar distribución de este artículo
        var valores = [];
        document
          .querySelectorAll(".wizard-periodo-input")
          .forEach(function (inp) {
            valores.push(parseInt(inp.value) || 0);
          });
        programacionWizard[idArticulo] = {
          tipo: chosen,
          llenos: valores,
          mes: chosenMes,
        };
        state.lastWasChanged =
          preseleccionado !== null && chosen !== preseleccionado;
        state.lastTipo = chosen;
        state.currentIndex++;
        if (state.currentIndex < state.articulos.length) {
          mostrarPasoArticulo(state);
        } else {
          finalizarWizard(state);
        }
      } else if (result.isDenied) {
        state.omitidos++;
        state.currentIndex++;
        if (state.currentIndex < state.articulos.length) {
          mostrarPasoArticulo(state);
        } else {
          finalizarWizard(state);
        }
      }
    });
  }

  function construirHtmlPaso(art, preseleccionado) {
    var h = '<div class="wizard-paso">';

    h += '<div class="wizard-tipo-grupo">';
    h += window.crearCheckAnimado({
      type: "radio",
      name: "wizardTipo",
      value: "mensual",
      text: "Mensual",
      checked: preseleccionado === "mensual",
      className:
        "wizard-tipo-btn" + (preseleccionado === "mensual" ? " activo" : ""),
    });
    h += window.crearCheckAnimado({
      type: "radio",
      name: "wizardTipo",
      value: "anual",
      text: "Anual",
      checked: preseleccionado === "anual",
      className:
        "wizard-tipo-btn" + (preseleccionado === "anual" ? " activo" : ""),
    });
    h += "</div>";

    // Selector de mes (solo visible para mensual)
    var mostrarMes =
      preseleccionado === "mensual" ? "" : ' style="display:none"';
    h +=
      '<div id="wizardMesContainer" class="wizard-mes-container"' +
      mostrarMes +
      ">";
    h += '<label class="wizard-mes-label">Mes de distribución:</label>';
    h += '<div class="wizard-mes-select-wrap">';
    h +=
      '<select id="wizardMesSelect" class="form-select wizard-mes-select" style="width:100%">';
    h += '<option value="">— Seleccione —</option>';
    var nombresMes = [
      "Enero",
      "Febrero",
      "Marzo",
      "Abril",
      "Mayo",
      "Junio",
      "Julio",
      "Agosto",
      "Septiembre",
      "Octubre",
      "Noviembre",
      "Diciembre",
    ];
    for (var m = 0; m < 12; m++) {
      h +=
        '<option value="' +
        (m + 1) +
        '">' +
        (m + 1) +
        " - " +
        nombresMes[m] +
        "</option>";
    }
    h += "</select>";
    h += "</div></div>";

    h +=
      '<div class="wizard-info-cantidad">Cantidad requerida: <strong>' +
      art.cantidad +
      "</strong></div>";

    h +=
      '<div id="wizardTablaContainer" data-cantidad-requerida="' +
      art.cantidad +
      '">';
    if (preseleccionado) {
      h += generarTablaDistribucion(art, preseleccionado);
    } else {
      h +=
        '<p class="wizard-placeholder">Seleccione el tipo de distribución para continuar</p>';
    }
    h += "</div>";

    h += "</div>";
    return h;
  }

  function generarTablaDistribucion(art, tipo) {
    var encabezados, claves;
    if (tipo === "mensual") {
      encabezados = ["Sem 1", "Sem 2", "Sem 3", "Sem 4"];
      claves = ["sem1", "sem2", "sem3", "sem4"];
    } else {
      encabezados = [
        "Ene",
        "Feb",
        "Mar",
        "Abr",
        "May",
        "Jun",
        "Jul",
        "Ago",
        "Sep",
        "Oct",
        "Nov",
        "Dic",
      ];
      claves = [
        "ene",
        "feb",
        "mar",
        "abr",
        "may",
        "jun",
        "jul",
        "ago",
        "sep",
        "oct",
        "nov",
        "dic",
      ];
    }

    var h = '<div class="wizard-tabla-scroll"><table class="wizard-tabla">';
    h += "<thead><tr><th>No.</th><th>Descripción Detallada</th><th>Unidad</th>";
    for (var i = 0; i < encabezados.length; i++)
      h += "<th>" + encabezados[i] + "</th>";
    h += "<th>Total</th></tr></thead>";

    var descCorta =
      art.descripcionDetallada.length > 80
        ? art.descripcionDetallada.substring(0, 80) + "…"
        : art.descripcionDetallada;

    h += "<tbody><tr>";
    h += "<td>" + art.numero + "</td>";
    h +=
      '<td class="wizard-desc-cell">' + escapeHtmlWizard(descCorta) + "</td>";
    h += "<td>" + escapeHtmlWizard(art.unidadMedida) + "</td>";
    for (var j = 0; j < claves.length; j++) {
      h +=
        '<td><input type="number" class="wizard-periodo-input" data-clave="' +
        claves[j] +
        '" min="0" step="1" value="0"></td>';
    }
    h +=
      '<td class="wizard-total-cell"><strong>0 / ' +
      art.cantidad +
      "</strong></td>";
    h += "</tr></tbody></table></div>";
    return h;
  }

  function escapeHtmlWizard(str) {
    if (!str) return "";
    var d = document.createElement("div");
    d.textContent = str;
    return d.innerHTML;
  }

  function destruirWizardMesSelect2() {
    var $sel = $("#wizardMesSelect");
    if (!$sel.length) return;

    window.SelectRosaBuscable.destruir($sel[0]);
  }

  function inicializarWizardMesSelect2() {
    var $sel = $("#wizardMesSelect");
    if (!$sel.length || !$("#wizardMesContainer").is(":visible")) return;

    destruirWizardMesSelect2();

    window.SelectRosaBuscable.inicializar($sel[0], {
      placeholder: "Buscar mes...",
      defaultText: false,
    });
  }

  function configurarEventosPaso(art) {
    document
      .querySelectorAll('input[name="wizardTipo"]')
      .forEach(function (radio) {
        radio.addEventListener("change", function () {
          document.querySelectorAll(".wizard-tipo-btn").forEach(function (btn) {
            btn.classList.remove("activo");
          });
          this.closest(".wizard-tipo-btn").classList.add("activo");
          var mesContainer = document.getElementById("wizardMesContainer");
          if (mesContainer) {
            if (this.value === "mensual") {
              mesContainer.style.display = "";
              inicializarWizardMesSelect2();
            } else {
              destruirWizardMesSelect2();
              mesContainer.style.display = "none";
            }
          }
          actualizarTablaPaso(art, this.value);
        });
      });
    document
      .querySelectorAll(".wizard-periodo-input")
      .forEach(function (input) {
        input.addEventListener("input", calcularYMostrarTotal);
        enlazarSeleccionPorTab(input);
      });

    if ($("#wizardMesContainer").is(":visible")) {
      inicializarWizardMesSelect2();
    }
  }

  function actualizarTablaPaso(art, tipo) {
    var cont = document.getElementById("wizardTablaContainer");
    if (!cont) return;
    cont.setAttribute("data-cantidad-requerida", art.cantidad);
    cont.innerHTML = generarTablaDistribucion(art, tipo);
    cont.querySelectorAll(".wizard-periodo-input").forEach(function (input) {
      input.addEventListener("input", calcularYMostrarTotal);
      enlazarSeleccionPorTab(input);
    });
  }

  function enlazarSeleccionPorTab(input) {
    input.addEventListener("keydown", function (e) {
      if (e.key === "Tab") {
        wizardSeleccionarEnfoquePorTab = true;
      }
    });

    input.addEventListener("mousedown", function () {
      wizardSeleccionarEnfoquePorTab = false;
    });

    input.addEventListener("focus", function () {
      if (wizardSeleccionarEnfoquePorTab) {
        this.select();
        wizardSeleccionarEnfoquePorTab = false;
      }
    });
  }

  function calcularTotalPaso() {
    var total = 0;
    document
      .querySelectorAll(".wizard-periodo-input")
      .forEach(function (input) {
        total += parseFloat(input.value) || 0;
      });
    return total;
  }

  function calcularYMostrarTotal() {
    var total = calcularTotalPaso();
    var cont = document.getElementById("wizardTablaContainer");
    var requerida = cont
      ? parseFloat(cont.getAttribute("data-cantidad-requerida")) || 0
      : 0;
    var celda = document.querySelector(".wizard-total-cell strong");
    if (celda) {
      celda.textContent = total + " / " + requerida;
      celda.style.color =
        Math.abs(total - requerida) < 0.01 ? "#22c55e" : "#ef4444";
    }
  }

  function serializarProgramacion() {
    // Elimina inputs previos
    document.querySelectorAll(".wizard-hidden-prog").forEach(function (el) {
      el.remove();
    });

    var form = document.querySelector("form");
    $("#tablaArticulos tbody tr").each(function (i) {
      var $row = $(this);
      var idArticulo = $row.find(".select-articulo").val();
      if (!idArticulo) return;

      var prog = programacionWizard[idArticulo];
      if (!prog) return;

      function addHidden(name, value) {
        var inp = document.createElement("input");
        inp.type = "hidden";
        inp.name = name;
        inp.value = value !== null && value !== undefined ? value : "";
        inp.className = "wizard-hidden-prog";
        form.appendChild(inp);
      }

      addHidden("Articulos[" + i + "].TipoProgramacion", prog.tipo);
      if (prog.mes) {
        addHidden("Articulos[" + i + "].Mes", prog.mes);
      }
      for (var j = 0; j < prog.llenos.length; j++) {
        addHidden("Articulos[" + i + "].Llenado" + (j + 1), prog.llenos[j]);
      }
    });
  }

  function bloquearSeccionArticulos() {
    $("#tablaArticulos .select-articulo").each(function () {
      var $select = $(this);
      var name = $select.attr("name"); // "Articulos[0].IdArticulo"
      var val = $select.val();

      // Solo agregar si no existe ya un hidden de respaldo
      if (
        val &&
        !$select.siblings('input[type="hidden"][data-backup="1"]').length
      ) {
        $("<input>")
          .attr("type", "hidden")
          .attr("name", name)
          .attr("data-backup", "1")
          .val(val)
          .insertAfter($select);
      }
    });

    $("#tablaArticulos .select-unidad").each(function () {
      var $select = $(this);
      var name = $select.attr("name");
      var val = $select.val();

      if (
        val &&
        !$select.siblings('input[type="hidden"][data-backup-unidad="1"]').length
      ) {
        $("<input>")
          .attr("type", "hidden")
          .attr("name", name)
          .attr("data-backup-unidad", "1")
          .val(val)
          .insertAfter($select);
      }
    });

    var btnAgregar = document.querySelector(
      'button[onclick="agregarArticulo()"]',
    );
    if (btnAgregar) {
      btnAgregar.disabled = true;
      btnAgregar.classList.add("disabled");
    }

    $("#tablaArticulos .select-articulo")
      .prop("disabled", true)
      .trigger("change");
    $("#tablaArticulos .select-unidad")
      .prop("disabled", true)
      .trigger("change");
    $("#tablaArticulos .cantidad-input").prop("readonly", true);
    $('#tablaArticulos button[onclick^="eliminarArticulo"]')
      .prop("disabled", true)
      .addClass("disabled");
    $("#tablaArticulos .desc-preview").css({
      "pointer-events": "none",
      opacity: "0.7",
    });
  }

  function finalizarWizard(state) {
    reindexarArticulos();
    serializarProgramacion();
    completarFlujoContinuar();
  }

  // ── Wizard distribución por municipio ──────────────────────────────────
  var municipioWizardDatos = {}; // { idArticulo: [ {idMunicipio, cantidad}, ... ] }
  var municipioWizardArticulos = [];
  var municipioWizardIdx = 0;
  var _opcionesMuniHtml = "";

  function obtenerMunicipiosSeleccionados($actual) {
    var ids = [];
    $("#wizardMuniTbody .muni-fila-select").each(function () {
      if ($actual && this === $actual[0]) return;
      if (this.value) ids.push(String(this.value));
    });
    return ids;
  }

  function construirOpcionesMunicipio(idSeleccionado, idsExcluidos) {
    var html = '<option value="">— Seleccione —</option>';
    (window.municipiosOpciones || []).forEach(function (m) {
      var id = String(m.id);
      var esSeleccionado = idSeleccionado && id === String(idSeleccionado);
      if (idsExcluidos.indexOf(id) !== -1 && !esSeleccionado) return;
      html +=
        '<option value="' +
        id +
        '"' +
        (esSeleccionado ? " selected" : "") +
        ">" +
        m.nombre +
        "</option>";
    });
    return html;
  }

  function refrescarOpcionesMunicipios() {
    $("#wizardMuniTbody .muni-fila-select").each(function () {
      var $select = $(this);
      var valorActual = $select.val();
      var idsExcluidos = obtenerMunicipiosSeleccionados($select);

      destruirSelectorMunicipio(this);
      this.innerHTML = construirOpcionesMunicipio(valorActual, idsExcluidos);
      this.value = valorActual || "";
      inicializarSelectorMunicipio(this);
    });
  }

  function preguntarMunicipios() {
    Swal.fire({
      title: "¿Distribuir por municipio?",
      text: "Puedes asignar un municipio a cada partida de forma opcional.",
      icon: "question",
      iconColor: "var(--rosa-400)",
      showDenyButton: true,
      confirmButtonText: "Sí",
      denyButtonText: "No, guardar",
      confirmButtonColor: "var(--rosa-400)",
      denyButtonColor: "var(--slate-500)",
    }).then(function (r) {
      if (r.isConfirmed) {
        iniciarWizardMunicipios();
      } else {
        mostrarExitoYEnviar();
      }
    });
  }

  function iniciarWizardMunicipios(onComplete) {
    municipioWizardArticulos = [];
    $("#tablaArticulos tbody tr").each(function () {
      var $row = $(this);
      var idArticulo =
        $row.find('input[data-backup="1"]').val() ||
        $row.find(".select-articulo").val();
      var desc = $row.find(".desc-hidden").val() || "Sin descripción";
      var cantidad = parseFloat($row.find(".cantidad-input").val()) || 0;
      if (idArticulo)
        municipioWizardArticulos.push({
          idArticulo: idArticulo,
          descripcionDetallada: desc,
          cantidad: cantidad,
        });
    });

    // ← agrega este log para confirmar
    console.log("municipioWizardArticulos:", municipioWizardArticulos);

    if (!municipioWizardArticulos.length) {
      if (typeof onComplete === "function") onComplete();
      return;
    }

    municipioWizardDatos = {};
    municipioWizardIdx = 0;
    _opcionesMuniHtml =
      '<option value="">— Seleccione —</option>' +
      (window.municipiosOpciones || [])
        .map(function (m) {
          return '<option value="' + m.id + '">' + m.nombre + "</option>";
        })
        .join("");

    var modalEl = document.getElementById("modalMunicipiosWizard");
    if (!modalEl) {
      console.error("modalMunicipiosWizard no encontrado en el DOM");
      if (typeof onComplete === "function") onComplete();
      return;
    }

    // ← registrar listeners aquí, no al cargar el script
    var btnSig = document.getElementById("btnWizardMuniSiguiente");
    var btnAnt = document.getElementById("btnWizardMuniAnterior");
    var btnAgregar = document.getElementById("btnWizardMuniAgregarFila");

    // Clonar para limpiar listeners previos
    if (btnSig) {
      var nuevoSig = btnSig.cloneNode(true);
      btnSig.parentNode.replaceChild(nuevoSig, btnSig);
      nuevoSig.addEventListener("click", function () {
        onClickSiguienteMunicipio(onComplete);
      });
    }
    if (btnAnt) {
      var nuevoAnt = btnAnt.cloneNode(true);
      btnAnt.parentNode.replaceChild(nuevoAnt, btnAnt);
      nuevoAnt.addEventListener("click", onClickAnteriorMunicipio);
    }
    var modal =
      bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    $(modalEl).one("shown.bs.modal", function () {
      if (btnAgregar) {
        btnAgregar.addEventListener("click", function () {
          agregarFilaMunicipio();
          actualizarTotalMunicipio(
            municipioWizardArticulos[municipioWizardIdx].cantidad,
          );
        });
      }
      renderizarPasoMunicipio(0);
    });

    setTimeout(function () {
      modal.show();
    }, 200);
  }

  function onClickSiguienteMunicipio(onComplete) {
    var val = validarMunicipioActual();
    if (!val.valido) {
      Swal.fire({
        icon: "warning",
        title: "Cantidades incorrectas",
        text: val.mensaje,
        confirmButtonColor: "var(--rosa-400)",
      });
      return;
    }
    guardarMunicipioActual();
    if (municipioWizardIdx < municipioWizardArticulos.length - 1) {
      municipioWizardIdx++;
      renderizarPasoMunicipio(municipioWizardIdx);
    } else {
      serializarMunicipios();
      var modalEl = document.getElementById("modalMunicipiosWizard");
      $(modalEl).one("hidden.bs.modal", function () {
        if (typeof onComplete === "function") {
          onComplete();
        }
      });
      bootstrap.Modal.getInstance(modalEl).hide();
    }
  }

  function onClickAnteriorMunicipio() {
    guardarMunicipioActual();
    municipioWizardIdx--;
    renderizarPasoMunicipio(municipioWizardIdx);
  }

  function renderizarPasoMunicipio(idx) {
    var art = municipioWizardArticulos[idx];
    var total = municipioWizardArticulos.length;

    document.getElementById("wizardMuniSubtitulo").textContent =
      "Partida " + (idx + 1) + " de " + total;
    document.getElementById("wizardMuniNombrePartida").textContent =
      art.descripcionDetallada || "";
    document.getElementById("wizardMuniCantidadReq").textContent = art.cantidad;

    var tbody = document.getElementById("wizardMuniTbody");
    tbody.innerHTML = "";

    var filas = municipioWizardDatos[art.idArticulo] || [];
    if (filas.length === 0) {
      agregarFilaMunicipio();
    } else {
      filas.forEach(function (f) {
        agregarFilaMunicipio(f.idMunicipio, f.cantidad);
      });
    }

    actualizarTotalMunicipio(art.cantidad);

    document.getElementById("btnWizardMuniAnterior").style.display =
      idx > 0 ? "" : "none";

    var btnSig = document.getElementById("btnWizardMuniSiguiente");
    btnSig.innerHTML =
      idx === total - 1
        ? 'Guardar <i class="fa-solid fa-floppy-disk"></i>'
        : 'Siguiente <i class="fa-solid fa-chevron-right"></i>';
  }

  function agregarFilaMunicipio(idMuniVal, cantidadVal) {
    var tbody = document.getElementById("wizardMuniTbody");
    var tr = document.createElement("tr");

    var tdSelect = document.createElement("td");
    var select = document.createElement("select");
    select.className =
      "form-select form-select-sm muni-fila-select municipios-input";
    select.innerHTML = construirOpcionesMunicipio(
      idMuniVal,
      obtenerMunicipiosSeleccionados(),
    );
    if (idMuniVal) select.value = idMuniVal;
    select.addEventListener("change", function () {
      refrescarOpcionesMunicipios();
    });
    tdSelect.appendChild(select);
    tr.appendChild(tdSelect);

    var tdCantidad = document.createElement("td");
    var input = document.createElement("input");
    input.type = "number";
    input.min = "0";
    input.step = "1";
    input.className =
      "form-control form-control-sm muni-fila-cantidad municipios-input";
    input.value = cantidadVal !== undefined ? cantidadVal : "";
    input.placeholder = "0";
    input.addEventListener("input", function () {
      actualizarTotalMunicipio(
        municipioWizardArticulos[municipioWizardIdx].cantidad,
      );
    });
    tdCantidad.appendChild(input);
    tr.appendChild(tdCantidad);

    var tdElim = document.createElement("td");
    tdElim.className = "text-center";
    var btnElim = document.createElement("button");
    btnElim.type = "button";
    btnElim.className = "btn-delete";
    btnElim.innerHTML = '<i class="fa-solid fa-circle-minus"></i>';
    btnElim.addEventListener("click", function () {
      destruirSelectorMunicipio(select);
      tr.remove();
      refrescarOpcionesMunicipios();
      actualizarTotalMunicipio(
        municipioWizardArticulos[municipioWizardIdx].cantidad,
      );
    });
    tdElim.appendChild(btnElim);
    tr.appendChild(tdElim);

    tbody.appendChild(tr);
    inicializarSelectorMunicipio(select);
    refrescarOpcionesMunicipios();
  }

  function inicializarSelectorMunicipio(select) {
    if (!select) return;

    window.SelectRosaBuscable.inicializar(select, {
      placeholder: "Buscar municipio...",
      defaultText: "Puebla",
    });
  }

  function destruirSelectorMunicipio(select) {
    if (!select) return;

    window.SelectRosaBuscable.destruir(select);
  }

  function actualizarTotalMunicipio(cantidadRequerida) {
    var total = 0;
    document
      .querySelectorAll("#wizardMuniTbody .muni-fila-cantidad")
      .forEach(function (inp) {
        total += parseFloat(inp.value) || 0;
      });
    var el = document.getElementById("wizardMuniCantidadAsig");
    el.textContent = total;
    el.style.color =
      Math.abs(total - cantidadRequerida) < 0.01 ? "#22c55e" : "#ef4444";
  }

  function guardarMunicipioActual() {
    var art = municipioWizardArticulos[municipioWizardIdx];
    var filas = [];
    document.querySelectorAll("#wizardMuniTbody tr").forEach(function (tr) {
      var sel = tr.querySelector(".muni-fila-select");
      var inp = tr.querySelector(".muni-fila-cantidad");
      if (sel && sel.value) {
        filas.push({
          idMunicipio: sel.value,
          cantidad: parseFloat(inp.value) || 0,
        });
      }
    });
    municipioWizardDatos[art.idArticulo] = filas;
  }

  function validarMunicipioActual() {
    var art = municipioWizardArticulos[municipioWizardIdx];
    var total = 0;
    var hayFilas = false;
    document.querySelectorAll("#wizardMuniTbody tr").forEach(function (tr) {
      var sel = tr.querySelector(".muni-fila-select");
      var inp = tr.querySelector(".muni-fila-cantidad");
      if (sel && sel.value) {
        hayFilas = true;
        total += parseFloat(inp.value) || 0;
      }
    });
    if (!hayFilas) {
      return {
        valido: false,
        mensaje: "Debe asignar al menos un municipio para esta partida.",
      };
    }
    if (Math.abs(total - art.cantidad) > 0.01) {
      return {
        valido: false,
        mensaje:
          "La suma (" +
          total +
          ") debe ser igual a la cantidad requerida (" +
          art.cantidad +
          ").",
      };
    }
    return { valido: true };
  }

  function serializarMunicipios() {
    document.querySelectorAll(".wizard-hidden-muni").forEach(function (el) {
      el.remove();
    });
    var form = document.querySelector("form");
    $("#tablaArticulos tbody tr").each(function (i) {
      var idArticulo = $(this).find(".select-articulo").val();
      if (!idArticulo) return;
      var filas = municipioWizardDatos[idArticulo];
      if (!filas || !filas.length) return;
      filas.forEach(function (f, j) {
        function addHidden(name, value) {
          var inp = document.createElement("input");
          inp.type = "hidden";
          inp.name = name;
          inp.value = value || "";
          inp.className = "wizard-hidden-muni";
          form.appendChild(inp);
        }
        addHidden(
          "Articulos[" + i + "].Municipios[" + j + "].IdMunicipio",
          f.idMunicipio,
        );
        addHidden(
          "Articulos[" + i + "].Municipios[" + j + "].Cantidad",
          f.cantidad,
        );
      });
    });
  }

  function mostrarExitoYEnviar() {
    Swal.fire({
      title: "Programación completada",
      text: 'Presione "Crear Requisición" para guardar.',
      icon: "success",
      iconColor: "var(--rosa-400)",
      confirmButtonText: "Entendido",
      confirmButtonColor: "var(--rosa-400)",
    });
  }

  function completarFlujoContinuar() {
    programacionFinalizada = true;
    bloquearSeccionArticulos();

    var btnC = document.getElementById("btnContinuar");
    var btnG = document.getElementById("btnGuardarFinal");
    if (btnC) btnC.style.display = "none";
    if (btnG) btnG.style.display = "";

    mostrarExitoYEnviar();
  }

  window.eliminarFotoExistente = function (idFoto) {
    Swal.fire({
      title: "\u00bfEliminar esta foto?",
      text: "Esta acci\u00f3n no se puede deshacer.",
      icon: "warning",
      showCancelButton: true,
      confirmButtonColor: "#e53e3e",
      cancelButtonColor: "var(--slate-500)",
      confirmButtonText: "S\u00ed, eliminar",
      cancelButtonText: "Cancelar",
    }).then(function (result) {
      if (result.isConfirmed) {
        $.post("/Servicios/EliminarFoto", { idFoto: idFoto }, function (res) {
          if (res.success) {
            // Quitar el wrapper de la foto de la vista
            var wrapper = document.getElementById("foto-wrapper-" + idFoto);
            if (wrapper) wrapper.remove();

            // Si ya no quedan fotos existentes, ocultar el contenedor
            var fotosRestantes = document.querySelectorAll(
              ".foto-existente-wrapper",
            );
            if (fotosRestantes.length === 0) {
              var contenedor = document.getElementById("fotosExistentes");
              if (contenedor) contenedor.closest("div").remove();
            }

            Swal.fire({
              icon: "success",
              title: "Foto eliminada",
            });
          } else {
            Swal.fire({ icon: "error", title: "No se pudo eliminar la foto" });
          }
        });
      }
    });
  };

  // --- Lightbox: mismo comportamiento que modales de tablas (modal-adjuntos.js) ---
  window.abrirVisorImagen = function (src) {
    if (typeof window.abrirVisorImagenTabla === "function") {
      window.abrirVisorImagenTabla(src);
    }
  };

  window.cerrarVisorImagen = function () {
    if (typeof window.cerrarVisorImagenTabla === "function") {
      window.cerrarVisorImagenTabla();
    }
  };
})();
