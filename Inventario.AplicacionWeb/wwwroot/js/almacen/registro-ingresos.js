(function () {
  var container = document.querySelector(".tabla-requi-page");
  if (!container) return;

  /* ── URLs ── */
  var urlRegistrarIngreso   = container.getAttribute("data-url-registrar-ingreso")    || "";
  var urlIngresoInventarioPdf = container.getAttribute("data-url-ingreso-inventario-pdf") || "";
  var urlDetalle            = container.getAttribute("data-url-obtener-detalle-ingreso") || "";
  var urlSubirPdf           = container.getAttribute("data-url-subir-pdf-ingreso")    || "";

  /* ── Helpers ── */
  function postJson(url, body) {
    return fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body || {})
    }).then(function (r) {
      if (!r.ok) throw "Error " + r.status;
      return r.json();
    });
  }

  function swalError(msg) {
    Swal.fire({ icon: "error", title: "Error", text: msg, confirmButtonColor: "#e11d48" });
  }
  function swalWarning(msg) {
    Swal.fire({ icon: "warning", title: "Atención", text: msg, confirmButtonColor: "#e11d48" });
  }
  function swalExito(msg) {
    return Swal.fire({ icon: "success", title: "¡Listo!", text: msg, confirmButtonColor: "#e11d48" });
  }
  function swalConfirmar(titulo, texto, btnTexto) {
    return Swal.fire({
      icon: "question", title: titulo, text: texto,
      showCancelButton: true,
      confirmButtonText: btnTexto || "Confirmar",
      cancelButtonText: "Cancelar",
      confirmButtonColor: "#e11d48",
      cancelButtonColor: "#64748b"
    });
  }

  /* ════════════════════════════════════════════════════════════
     MODAL: REGISTRAR INGRESO — lógica completa
  ════════════════════════════════════════════════════════════ */
  (function () {
    var itemsIngreso = [];

    /* Referencias del modal */
    var modal          = document.getElementById("modalRegistrarIngreso");
    var btnAgregar     = document.getElementById("btnAgregarItem");
    var btnIngreso     = document.getElementById("btnRegistrarIngreso");
    var radNo          = document.getElementById("prodNuevoNo");
    var radSi          = document.getElementById("prodNuevoSi");
    var seccionNuevo   = document.getElementById("seccionProductoNuevo");
    var seccionExist   = document.getElementById("seccionProductoExistente");
    var selExistente   = document.getElementById("selectProductoExistente");
    var selUnidad      = document.getElementById("ingresoUnidad");
    var resumen        = document.getElementById("resumenProductoExistente");
    var resumenTexto   = document.getElementById("resumenProductoTexto");
    var detalle        = document.getElementById("detalleProductoExistente");
    var detalleClave   = document.getElementById("detalleProductoClave");
    var detalleUnidad  = document.getElementById("detalleProductoUnidad");
    var detalleDesc    = document.getElementById("detalleProductoDescripcion");
    var tablaBody      = document.getElementById("tablaIngresoItemsBody");
    var itemsWrap      = document.getElementById("ingresoItemsWrap");

    if (!modal) return;

    /* ── Toggle modo producto ── */
    function toggleModo() {
      var esNuevo = radSi && radSi.checked;
      if (seccionNuevo) seccionNuevo.style.display = esNuevo ? "block" : "none";
      if (seccionExist) seccionExist.hidden = esNuevo;
    }

    if (radSi) radSi.addEventListener("change", toggleModo);
    if (radNo)  radNo.addEventListener("change", toggleModo);

    /* ── Detalle producto existente ── */
    function actualizarDetalle(opt) {
      if (!detalle) return;
      if (opt && opt.value) {
        detalle.hidden = false;
        if (detalleClave)  detalleClave.textContent  = opt.getAttribute("data-clave") || "-";
        if (detalleUnidad) detalleUnidad.textContent = opt.getAttribute("data-unidad") || "-";
        if (detalleDesc)   detalleDesc.textContent   = opt.getAttribute("data-descripcion") || "-";
      } else {
        detalle.hidden = true;
        if (detalleClave)  detalleClave.textContent  = "-";
        if (detalleUnidad) detalleUnidad.textContent = "-";
        if (detalleDesc)   detalleDesc.textContent   = "-";
      }
    }

    /* ── Change en select existente ── */
    function onSelExistenteChange() {
      if (!selExistente) return;
      var opt = selExistente.options[selExistente.selectedIndex];
      if (opt && opt.value) {
        var claveEl = document.getElementById("ingresoClave");
        var descEl  = document.getElementById("ingresoDescripcion");
        var unidEl  = document.getElementById("ingresoUnidad");
        if (claveEl) claveEl.value = opt.getAttribute("data-clave") || "";
        if (descEl)  descEl.value  = opt.getAttribute("data-descripcion") || "";
        if (unidEl)  unidEl.value  = opt.getAttribute("data-unidad") || "";
        if (selUnidad && window.SelectRosaBuscable) window.SelectRosaBuscable.actualizar(selUnidad);
        if (resumen) resumen.hidden = false;
        if (resumenTexto) resumenTexto.textContent = opt.text;
        actualizarDetalle(opt);
      } else {
        if (resumen) resumen.hidden = true;
        if (resumenTexto) resumenTexto.textContent = "";
        actualizarDetalle(null);
      }
    }

    if (selExistente) selExistente.addEventListener("change", onSelExistenteChange);

    /* ── Renderizar tabla interna ── */
    function renderTabla() {
      if (!tablaBody) return;
      if (itemsIngreso.length === 0) {
        if (itemsWrap) itemsWrap.style.display = "none";
        if (btnIngreso) btnIngreso.disabled = true;
        tablaBody.innerHTML = "";
        return;
      }
      if (itemsWrap) itemsWrap.style.display = "block";
      if (btnIngreso) btnIngreso.disabled = false;

      tablaBody.innerHTML = itemsIngreso.map(function (item, idx) {
        return "<tr>" +
          "<td>" + (item.clave || "—") + "</td>" +
          "<td>" + item.descripcion + "</td>" +
          "<td>" + item.unidadMedida + "</td>" +
          '<td style="text-align:center;font-weight:600;">' + item.cantidad + "</td>" +
          '<td style="text-align:center;">' +
            '<button type="button" class="btn-accion" style="background:#fef2f2;border:1px solid #fecaca;color:#dc2626;border-radius:6px;width:30px;height:30px;display:inline-flex;align-items:center;justify-content:center;cursor:pointer;" data-del="' + idx + '" title="Quitar">' +
              '<i class="fa-solid fa-trash-can" style="font-size:12px;pointer-events:none;"></i>' +
            "</button>" +
          "</td>" +
        "</tr>";
      }).join("");

      tablaBody.querySelectorAll("[data-del]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          itemsIngreso.splice(parseInt(this.getAttribute("data-del")), 1);
          renderTabla();
        });
      });
    }

    /* ── Limpiar selector de artículo ── */
    function limpiarSelector() {
      ["ingresoClave", "ingresoDescripcion", "ingresoUnidad"].forEach(function (id) {
        var el = document.getElementById(id); if (el) el.value = "";
      });
      var cant = document.getElementById("ingresoCantidad");
      if (cant) cant.value = "1";
      if (selExistente) {
        selExistente.value = "";
        if (window.SelectRosaBuscable) window.SelectRosaBuscable.actualizar(selExistente);
      }
      if (selUnidad) {
        selUnidad.value = "";
        if (window.SelectRosaBuscable) window.SelectRosaBuscable.actualizar(selUnidad);
      }
      if (resumen) resumen.hidden = true;
      if (resumenTexto) resumenTexto.textContent = "";
      actualizarDetalle(null);
      if (radNo) radNo.checked = true;
      toggleModo();
    }

    /* ── Limpiar modal completo ── */
    function limpiarModal() {
      limpiarSelector();
      var motivo = document.getElementById("ingresoMotivo");
      if (motivo) motivo.value = "";
      itemsIngreso = [];
      renderTabla();
    }

    /* ── Leer artículo del selector ── */
    function leerArticulo() {
      var esNuevo = radSi && radSi.checked;
      var clave, descripcion, unidadMedida;
      if (!esNuevo && selExistente && selExistente.value) {
        var opt = selExistente.options[selExistente.selectedIndex];
        clave        = (opt.getAttribute("data-clave") || "").trim();
        descripcion  = (opt.getAttribute("data-descripcion") || "").trim();
        unidadMedida = (opt.getAttribute("data-unidad") || "").trim();
      } else {
        clave        = (document.getElementById("ingresoClave")       ? document.getElementById("ingresoClave").value       : "").trim();
        descripcion  = (document.getElementById("ingresoDescripcion") ? document.getElementById("ingresoDescripcion").value : "").trim();
        unidadMedida = (document.getElementById("ingresoUnidad")      ? document.getElementById("ingresoUnidad").value      : "").trim();
      }
      var cantidad = parseInt(document.getElementById("ingresoCantidad") ? document.getElementById("ingresoCantidad").value : "0", 10);
      return { esNuevo: esNuevo, clave: clave, descripcion: descripcion, unidadMedida: unidadMedida, cantidad: cantidad };
    }

    /* ── Botón Agregar ── */
    if (btnAgregar) {
      btnAgregar.addEventListener("click", function () {
        var d = leerArticulo();
        if (!d.esNuevo && (!selExistente || !selExistente.value)) {
          swalWarning("Selecciona un artículo existente."); return;
        }
        if (!d.descripcion)  { swalWarning("La descripción es obligatoria."); return; }
        if (!d.unidadMedida) { swalWarning("La unidad de medida es obligatoria."); return; }
        if (!d.cantidad || d.cantidad <= 0) { swalWarning("La cantidad debe ser mayor a 0."); return; }
        itemsIngreso.push({ clave: d.clave, descripcion: d.descripcion, unidadMedida: d.unidadMedida, cantidad: d.cantidad });
        renderTabla();
        limpiarSelector();
      });
    }

    /* ── Botón Registrar Ingreso ── */
    if (btnIngreso) {
      btnIngreso.addEventListener("click", function () {
        if (!urlRegistrarIngreso) { swalError("URL de ingreso no configurada."); return; }
        if (itemsIngreso.length === 0) { swalWarning("Agrega al menos un artículo."); return; }
        var motivo = (document.getElementById("ingresoMotivo") ? document.getElementById("ingresoMotivo").value : "").trim();
        if (!motivo) { swalWarning("El motivo es obligatorio."); return; }

        swalConfirmar(
          "Registrar ingreso",
          "Se registrarán " + itemsIngreso.length + " artículo(s). ¿Continuar?",
          "Sí, registrar"
        ).then(function (result) {
          if (!result.isConfirmed) return;
          Swal.fire({ title: "Procesando...", allowOutsideClick: false, didOpen: function () { Swal.showLoading(); } });

          var snapshot = itemsIngreso.slice();

          postJson(urlRegistrarIngreso, { items: snapshot, motivo: motivo })
            .then(function (r) {
              if (r.ok) {
                Swal.fire({
                  icon: "success",
                  title: "Ingreso registrado",
                  text: r.mensaje || "El ingreso se guardó correctamente.",
                  showCancelButton: true,
                  confirmButtonText: '<i class="fa-solid fa-file-pdf"></i> Ver PDF',
                  cancelButtonText: "Cerrar",
                  confirmButtonColor: "#e11d48",
                  cancelButtonColor: "#64748b",
                }).then(function (res) {
                  if (res.isConfirmed && urlIngresoInventarioPdf) {
                    var form = document.createElement("form");
                    form.method = "POST";
                    form.action = urlIngresoInventarioPdf;
                    form.target = "_blank";
                    var inp = document.createElement("input");
                    inp.type = "hidden"; inp.name = "payload";
                    inp.value = JSON.stringify({ folio: r.folio || 0, motivo: motivo, items: snapshot });
                    form.appendChild(inp);
                    document.body.appendChild(form);
                    form.submit();
                    document.body.removeChild(form);
                  }
                  location.reload();
                });
              } else {
                swalError(r.error || "No se pudo registrar el ingreso.");
              }
            })
            .catch(function (err) {
              swalError(typeof err === "string" ? err : "No se pudo registrar el ingreso.");
            });
        });
      });
    }

    /* ── Lifecycle: SelectRosaBuscable ── */
    modal.addEventListener("show.bs.modal", function () {
      limpiarModal();
      if (!window.SelectRosaBuscable) return;
      if (selExistente) {
        window.SelectRosaBuscable.destruir(selExistente);
        window.SelectRosaBuscable.inicializar(selExistente, { placeholder: "-- Buscar artículo existente --", defaultText: false });
      }
      if (selUnidad) {
        window.SelectRosaBuscable.destruir(selUnidad);
        window.SelectRosaBuscable.inicializar(selUnidad, { placeholder: "-- Seleccionar unidad --", defaultText: false });
      }
    });

    modal.addEventListener("hidden.bs.modal", function () {
      if (!window.SelectRosaBuscable) return;
      if (selExistente) window.SelectRosaBuscable.destruir(selExistente);
      if (selUnidad)    window.SelectRosaBuscable.destruir(selUnidad);
    });
  })();

  /* ════════════════════════════════════════════════════════════
     TABLA DE INGRESOS: Filtro y Paginación
  ════════════════════════════════════════════════════════════ */
  var PAGINA_SIZE = 10;
  var paginaActual = 1;
  var filasTodas = [];

  function inicializarFiltros() {
    filasTodas = Array.from(document.querySelectorAll("#tablaIngresos tbody tr.fila-requi"));
    actualizarTabla();
  }

  function obtenerFiltros() {
    var buscar = (document.getElementById("filtroBuscarIngresos") || {}).value || "";
    var estado = (document.getElementById("filtroEstadoPdf") || {}).value || "";
    return { buscar: buscar.toLowerCase().trim(), estado: estado };
  }

  function filaCoincide(fila, f) {
    var folio  = (fila.querySelector(".folio-badge") || {}).textContent || "";
    var motivo = fila.getAttribute("data-motivo") || "";
    var pdf    = fila.getAttribute("data-tiene-pdf") || "no";
    if (f.buscar && !folio.toLowerCase().includes(f.buscar) && !motivo.toLowerCase().includes(f.buscar)) return false;
    if (f.estado === "sin" && pdf !== "no") return false;
    if (f.estado === "con" && pdf !== "si") return false;
    return true;
  }

  function actualizarTabla() {
    var f = obtenerFiltros();
    var filtradas = filasTodas.filter(function (fila) { return filaCoincide(fila, f); });
    var totalPags = Math.max(1, Math.ceil(filtradas.length / PAGINA_SIZE));
    if (paginaActual > totalPags) paginaActual = 1;
    filasTodas.forEach(function (fila) { fila.style.display = "none"; });
    var inicio = (paginaActual - 1) * PAGINA_SIZE;
    filtradas.slice(inicio, inicio + PAGINA_SIZE).forEach(function (fila) { fila.style.display = ""; });
    renderPaginacion(filtradas.length, totalPags);
    document.querySelectorAll("#tablaIngresos tbody tr.fila-vacia").forEach(function (v) {
      v.style.display = filtradas.length === 0 ? "" : "none";
    });
  }

  function renderPaginacion(total, totalPags) {
    var cont = document.getElementById("paginacionIngresos");
    if (!cont) return;
    if (totalPags <= 1 && total <= PAGINA_SIZE) { cont.innerHTML = ""; return; }
    var info = '<span class="almacen-paginacion-info">' + total + ' registro(s)</span>';
    var btns = '<div class="almacen-paginacion-btns">';
    btns += '<button class="almacen-paginacion-btn" data-pag="prev" ' + (paginaActual === 1 ? "disabled" : "") + '>&laquo;</button>';
    for (var p = 1; p <= totalPags; p++) {
      btns += '<button class="almacen-paginacion-btn almacen-paginacion-num ' + (p === paginaActual ? "activo" : "") + '" data-pag="' + p + '">' + p + '</button>';
    }
    btns += '<button class="almacen-paginacion-btn" data-pag="next" ' + (paginaActual === totalPags ? "disabled" : "") + '>&raquo;</button></div>';
    cont.innerHTML = info + btns;
    cont.querySelectorAll("[data-pag]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var val = this.getAttribute("data-pag");
        if (val === "prev" && paginaActual > 1) paginaActual--;
        else if (val === "next" && paginaActual < totalPags) paginaActual++;
        else if (val !== "prev" && val !== "next") paginaActual = parseInt(val);
        actualizarTabla();
      });
    });
  }

  var buscarInput  = document.getElementById("filtroBuscarIngresos");
  var estadoSelect = document.getElementById("filtroEstadoPdf");
  if (buscarInput)  buscarInput.addEventListener("input",  function () { paginaActual = 1; actualizarTabla(); });
  if (estadoSelect) estadoSelect.addEventListener("change", function () { paginaActual = 1; actualizarTabla(); });
  inicializarFiltros();

  /* ════════════════════════════════════════════════════════════
     VER DETALLE
  ════════════════════════════════════════════════════════════ */
  document.addEventListener("click", function (e) {
    var btn = e.target.closest("[data-ver-detalle-ingreso]");
    if (!btn) return;
    var numIngreso = btn.getAttribute("data-num-ingreso");
    var modalEl = document.getElementById("modalDetalleIngreso");
    var titulo  = document.getElementById("modalDetalleIngresoTitulo");
    var sub     = document.getElementById("modalDetalleIngresoSubtitulo");
    var tbody   = document.getElementById("tablaDetalleIngresoBody");
    if (titulo) titulo.textContent = "Detalle de Ingreso #" + String(numIngreso).padStart(4, "0");
    if (sub)    sub.textContent    = "Artículos registrados en este lote";
    if (tbody)  tbody.innerHTML    = '<tr><td colspan="4" class="text-center text-muted">Cargando...</td></tr>';
    if (modalEl) new bootstrap.Modal(modalEl).show();
    if (!urlDetalle) return;
    fetch(urlDetalle + "?primerIdIngreso=" + numIngreso)
      .then(function (r) { return r.json(); })
      .then(function (data) {
        if (!tbody) return;
        if (!data || !data.length) { tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Sin artículos</td></tr>'; return; }
        tbody.innerHTML = data.map(function (i) {
          return "<tr><td>" + (i.clave || "—") + "</td><td>" + i.descripcion + "</td><td>" + i.unidadMedida + "</td>" +
            '<td style="text-align:center;font-weight:600;">' + i.cantidad + "</td></tr>";
        }).join("");
      })
      .catch(function () { if (tbody) tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Error al cargar</td></tr>'; });
  });

  /* ════════════════════════════════════════════════════════════
     SUBIR PDF FIRMADO
  ════════════════════════════════════════════════════════════ */
  var numIngresoParaPdf = null;
  var filaParaPdf = null;

  document.addEventListener("click", function (e) {
    var btn = e.target.closest("[data-subir-pdf-ingreso]");
    if (!btn) return;
    numIngresoParaPdf = btn.getAttribute("data-num-ingreso");
    filaParaPdf = btn.closest("tr");
    var sub = document.getElementById("modalSubirPdfSubtitulo");
    if (sub) sub.textContent = "Ingreso #" + String(numIngresoParaPdf).padStart(4, "0");
    var input = document.getElementById("inputPdfFirmadoIngreso");
    if (input) input.value = "";
    var modalEl = document.getElementById("modalSubirPdfIngreso");
    if (modalEl) new bootstrap.Modal(modalEl).show();
  });

  var btnConfirmar = document.getElementById("btnConfirmarSubirPdf");
  if (btnConfirmar) {
    btnConfirmar.addEventListener("click", function () {
      var input = document.getElementById("inputPdfFirmadoIngreso");
      if (!input || !input.files || !input.files[0]) { swalError("Selecciona un archivo PDF."); return; }
      var form = new FormData();
      form.append("NumIngreso", numIngresoParaPdf);
      form.append("Pdf", input.files[0]);
      btnConfirmar.disabled = true;
      btnConfirmar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Subiendo...';
      fetch(urlSubirPdf, { method: "POST", body: form })
        .then(function (r) { return r.json(); })
        .then(function (r) {
          btnConfirmar.disabled = false;
          btnConfirmar.innerHTML = '<i class="fa-solid fa-upload"></i> Subir PDF';
          if (r.ok) {
            var modalEl = document.getElementById("modalSubirPdfIngreso");
            if (modalEl) { var inst = bootstrap.Modal.getInstance(modalEl); if (inst) inst.hide(); }
            swalExito("PDF firmado guardado correctamente.").then(function () {
              if (filaParaPdf) {
                var btnSubir = filaParaPdf.querySelector("[data-subir-pdf-ingreso]");
                var celdaEstado = filaParaPdf.cells[5];
                if (btnSubir) {
                  var enlace = document.createElement("a");
                  enlace.href = r.rutaFirmado; enlace.target = "_blank";
                  enlace.className = "btn-accion btn-pdf"; enlace.title = "Ver PDF firmado";
                  enlace.innerHTML = '<i class="fa-solid fa-file-pdf"></i>';
                  btnSubir.replaceWith(enlace);
                }
                if (celdaEstado) {
                  celdaEstado.innerHTML = '<span class="badge-estado-premium badge-estado-success"><i class="fa-solid fa-circle-check"></i> Firmado</span>';
                }
                filaParaPdf.setAttribute("data-tiene-pdf", "si");
              }
            });
          } else {
            swalError(r.error || "No se pudo subir el PDF.");
          }
        })
        .catch(function () {
          btnConfirmar.disabled = false;
          btnConfirmar.innerHTML = '<i class="fa-solid fa-upload"></i> Subir PDF';
          swalError("Error al subir el archivo.");
        });
    });
  }

})();
