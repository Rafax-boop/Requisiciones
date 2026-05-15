(function () {
  function esc(value) {
    return String(value ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function construirPreview(ruta, nombreArchivo) {
    var esImagen = /\.(jpg|jpeg|png|gif|webp)$/i.test(nombreArchivo || "");
    var esPdf = /\.pdf$/i.test(nombreArchivo || "");

    if (esPdf) {
      return '<iframe src="' + esc(ruta) + '" style="width: 100%; height: 100%; border: none; border-radius: 4px;"></iframe>';
    }

    if (esImagen) {
      return '<img src="' + esc(ruta) + '" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 4px;" />';
    }

    return '<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; border-radius: 4px;">' +
      '<div style="text-align: center; color: #999;">' +
      '<i class="fa-solid fa-file" style="font-size: 3rem; margin-bottom: 10px; display: block;"></i>' +
      '<p>No hay vista previa disponible</p>' +
      '<p style="font-size: 0.9rem;">Descarga el archivo para verlo</p>' +
      '</div>' +
      '</div>';
  }

  function construirListaHtml(archivos) {
    var listaHtml = '<ul style="list-style: none; padding: 0; margin: 0;">';

    archivos.forEach(function (arch, idx) {
      var nombre = arch.nombreArchivo || (arch.ruta || "").split("/").pop() || "Archivo";
      var activo = idx === 0 ? "activo" : "";

      listaHtml +=
        '<li class="archivo-item ' + activo + '" data-archivo="' + esc(arch.ruta) + '" data-nombre="' + esc(nombre) + '" style="padding: 12px; border-bottom: 1px solid #f0f0f0; cursor: pointer; transition: background-color 0.2s;">' +
        '<div style="display: flex; justify-content: space-between; align-items: flex-start;">' +
        '<div style="flex: 1;">' +
        '<div style="font-weight: 500; color: #333;"><i class="fa-solid fa-file"></i> ' + esc(nombre) + "</div>" +
        '<div style="font-size: 0.85rem; color: #888; margin-top: 4px;">' + esc(arch.tipo) + " • " + esc(arch.fechaSubida) + "</div>" +
        "</div>" +
        '<a href="' + esc(arch.ruta) + '" download style="margin-left: 10px; white-space: nowrap; padding: 4px 8px; font-size: 11px; color: #666; border: 1px solid #ddd; border-radius: 4px; text-decoration: none; display: inline-block; transition: all 0.2s; background: #f8f8f8;" onmouseover="this.style.background=\'#efefef\'; this.style.color=\'#333\';" onmouseout="this.style.background=\'#f8f8f8\'; this.style.color=\'#666\'">' +
        '<i class="fa-solid fa-download" style="font-size: 9px; margin-right: 4px;"></i>Descargar' +
        "</a>" +
        "</div>" +
        "</li>";
    });

    listaHtml += "</ul>";
    return listaHtml;
  }

  function enlazarEventosPreview(modal) {
    var archivoItems = modal.querySelectorAll(".archivo-item");
    var previewContainer = modal.querySelector("#previewContainer");

    archivoItems.forEach(function (item) {
      item.addEventListener("click", function () {
        archivoItems.forEach(function (it) { it.classList.remove("activo"); });
        this.classList.add("activo");

        var ruta = this.getAttribute("data-archivo") || "";
        var nombre = this.getAttribute("data-nombre") || "";
        previewContainer.innerHTML = construirPreview(ruta, nombre);
      });

      item.addEventListener("mouseover", function () {
        if (!this.classList.contains("activo")) {
          this.style.backgroundColor = "#f0f0f0";
        }
      });

      item.addEventListener("mouseout", function () {
        if (!this.classList.contains("activo")) {
          this.style.backgroundColor = "transparent";
        }
      });
    });
  }

  window.DocumentosRequisicionModal = {
    open: function (options) {
      var fetchUrl = options?.fetchUrl || "";
      var downloadZipUrl = options?.downloadZipUrl || "";
      var idRequisicion = options?.idRequisicion;
      var idConsolidada = options?.idConsolidada;

      if (!fetchUrl || (!idRequisicion && !idConsolidada)) {
        console.error("Faltan datos para abrir la modal de documentos.");
        return;
      }

      var queryParam = idConsolidada
        ? "idConsolidada=" + encodeURIComponent(idConsolidada)
        : "idRequisicion=" + encodeURIComponent(idRequisicion);

      fetch(fetchUrl + "?" + queryParam, {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "same-origin",
      })
        .then(function (r) { return r.json(); })
        .then(function (archivos) {
          if (!archivos || archivos.length === 0) {
            alert("Esta requisición no tiene archivos vinculados");
            return;
          }

          var archivoActual = archivos[0];
          var listaHtml = construirListaHtml(archivos);
          var previewHtml = construirPreview(archivoActual.ruta, archivoActual.nombreArchivo);
          var zipQueryParam = idConsolidada
            ? "idConsolidada=" + encodeURIComponent(idConsolidada)
            : "idRequisicion=" + encodeURIComponent(idRequisicion);
          var downloadMassiveHtml = downloadZipUrl
            ? '<div class="modal-footer" style="justify-content: flex-end;">' +
              '<a href="' + esc(downloadZipUrl) + "?" + zipQueryParam + '" class="btn boton-rosa">' +
              '<i class="fa-solid fa-file-zipper"></i> Descarga masiva' +
              "</a>" +
              "</div>"
            : "";

          var modal = document.createElement("div");
          modal.className = "modal fade";
          modal.setAttribute("tabindex", "-1");
          modal.setAttribute("aria-hidden", "true");
          modal.innerHTML =
            '<div class="modal-dialog modal-xl modal-dialog-centered">' +
            '<div class="modal-content modal-premium">' +
            '<div class="modal-header modal-header-premium">' +
            "<div>" +
            '<h5 class="modal-title modal-titulo-premium">Archivos de la Requisición</h5>' +
            '<p class="modal-subtitulo-premium">Visualiza y descarga los documentos adjuntos</p>' +
            "</div>" +
            '<button type="button" class="modal-btn-cerrar" data-bs-dismiss="modal">' +
            '<i class="fa-solid fa-xmark"></i>' +
            "</button>" +
            "</div>" +
            '<div class="modal-body modal-body-premium" style="padding: 0;">' +
            '<div style="display: flex; height: 600px;">' +
            '<div style="flex: 1; overflow-y: auto; border-right: 1px solid #e0e0e0; background: #fafafa;">' +
            '<div style="padding: 0;">' + listaHtml + "</div>" +
            "</div>" +
            '<div style="flex: 2; padding: 20px; display: flex; align-items: center; justify-content: center; background: white;" id="previewContainer">' +
            previewHtml +
            "</div>" +
            "</div>" +
            "</div>" +
            downloadMassiveHtml +
            "</div>" +
            "</div>";

          document.body.appendChild(modal);

          var bsModal = new bootstrap.Modal(modal);
          bsModal.show();
          enlazarEventosPreview(modal);

          modal.addEventListener("hidden.bs.modal", function () {
            modal.remove();
          });
        })
        .catch(function (err) {
          console.error("Error cargando archivos:", err);
          alert("Error al cargar los archivos");
        });
    },
  };
})();
