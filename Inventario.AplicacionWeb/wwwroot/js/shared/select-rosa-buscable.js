(function () {
  function obtenerOpciones(select) {
    return Array.from(select.options || []).map(function (option) {
      return {
        value: option.value,
        label: option.textContent || "",
      };
    });
  }

  function encontrarValorPorTexto(select, texto) {
    var textoNorm = String(texto || "").trim().toLowerCase();
    var match = obtenerOpciones(select).find(function (op) {
      return op.label.trim().toLowerCase() === textoNorm;
    });
    return match ? match.value : "";
  }

  function cerrarInstancia(instance) {
    if (!instance) return;
    instance.wrapper.classList.remove("abierto");
    instance.input.value = instance.labelSeleccionada || "";
    if (instance.dropdownHost) {
      instance.dropdownHost.style.display = "none";
    }
  }

  function sincronizarEstado(instance) {
    if (!instance) return;
    var deshabilitado = !!instance.select.disabled;
    instance.input.disabled = deshabilitado;
    instance.wrapper.classList.toggle("deshabilitado", deshabilitado);
    if (deshabilitado) {
      cerrarInstancia(instance);
    }
  }

  function renderizarOpciones(instance, filtro) {
    var textoFiltro = String(filtro || "").trim().toLowerCase();
    var opciones = obtenerOpciones(instance.select).filter(function (op) {
      if (!op.value) return false;
      if (!textoFiltro) return true;
      return op.label.toLowerCase().indexOf(textoFiltro) !== -1;
    });

    instance.dropdownHost.innerHTML = "";

    if (!opciones.length) {
      var vacio = document.createElement("div");
      vacio.className = "rosa-select__vacio";
      vacio.textContent = instance.ajax && textoFiltro ? "Buscando..." : "Sin resultados";
      instance.dropdownHost.appendChild(vacio);
      return;
    }

    opciones.forEach(function (op) {
      var boton = document.createElement("button");
      boton.type = "button";
      boton.className = "rosa-select__option";
      boton.setAttribute("data-value", op.value);
      if (instance.select.value === op.value) {
        boton.classList.add("seleccionada");
      }
      boton.textContent = op.label;
      boton.addEventListener("mousedown", function (e) {
        e.preventDefault();
        instance.select.value = op.value;
        instance.select.dispatchEvent(new Event("change", { bubbles: true }));
        sincronizarDesdeSelect(instance);
        cerrarInstancia(instance);
      });
      instance.dropdownHost.appendChild(boton);
    });

    actualizarOpcionActiva(instance);
  }

  function obtenerOpcionesVisibles(instance) {
    return Array.from(
      instance.dropdownHost.querySelectorAll(".rosa-select__option"),
    );
  }

  function actualizarOpcionActiva(instance) {
    var opciones = obtenerOpcionesVisibles(instance);
    if (!opciones.length) {
      instance.activeIndex = -1;
      return;
    }

    if (typeof instance.activeIndex !== "number" || instance.activeIndex < 0) {
      instance.activeIndex = 0;
    }

    if (instance.activeIndex >= opciones.length) {
      instance.activeIndex = opciones.length - 1;
    }

    opciones.forEach(function (opcion, index) {
      opcion.classList.toggle("activa", index === instance.activeIndex);
    });

    var activa = opciones[instance.activeIndex];
    if (activa) {
      activa.scrollIntoView({ block: "nearest" });
    }
  }

  function moverOpcionActiva(instance, delta) {
    var opciones = obtenerOpcionesVisibles(instance);
    if (!opciones.length) return;

    if (typeof instance.activeIndex !== "number" || instance.activeIndex < 0) {
      instance.activeIndex = 0;
    } else {
      instance.activeIndex += delta;
    }

    if (instance.activeIndex < 0) {
      instance.activeIndex = opciones.length - 1;
    }

    if (instance.activeIndex >= opciones.length) {
      instance.activeIndex = 0;
    }

    actualizarOpcionActiva(instance);
  }

  function seleccionarOpcionActiva(instance) {
    var opciones = obtenerOpcionesVisibles(instance);
    if (!opciones.length) return;

    if (typeof instance.activeIndex !== "number" || instance.activeIndex < 0) {
      instance.activeIndex = 0;
    }

    var activa = opciones[instance.activeIndex];
    if (activa) {
      activa.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));
    }
  }

  function posicionarDropdown(instance) {
    if (!instance || !instance.dropdownHost) return;
    var rect = instance.control.getBoundingClientRect();
    instance.dropdownHost.style.position = "fixed";
    instance.dropdownHost.style.top = rect.bottom + 8 + "px";
    instance.dropdownHost.style.left = rect.left + "px";
    instance.dropdownHost.style.width = rect.width + "px";
    instance.dropdownHost.style.display = "block";
  }

  function sincronizarDesdeSelect(instance) {
    var selected =
      instance.select.options[instance.select.selectedIndex] || null;
    instance.labelSeleccionada = selected && selected.value ? selected.text : "";
    instance.input.value = instance.labelSeleccionada || "";
    renderizarOpciones(instance, "");
    sincronizarEstado(instance);
  }

  function cargarOpcionesAjax(instance, termino) {
    var ajax = instance.ajax;
    if (!ajax || !ajax.url) return;

    if (ajax.before) {
      ajax.before(instance);
    }

    var params = ajax.data ? ajax.data({ term: termino }) : { term: termino };
    var separador = ajax.url.indexOf("?") === -1 ? "?" : "&";
    var queryString = Object.keys(params)
      .map(function (k) { return encodeURIComponent(k) + "=" + encodeURIComponent(params[k]); })
      .join("&");

    fetch(ajax.url + separador + queryString, {
      credentials: "same-origin",
      headers: { Accept: "application/json" },
    })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        var results = ajax.processResults ? ajax.processResults(data).results : data;
        var select = instance.select;
        select.innerHTML = '<option value=""></option>';
        (results || []).forEach(function (item) {
          var opt = document.createElement("option");
          opt.value = item.id;
          opt.textContent = item.text;
          select.appendChild(opt);
        });
        renderizarOpciones(instance, "");
        posicionarDropdown(instance);
      })
      .catch(function () {
        instance.dropdownHost.innerHTML = '<div class="rosa-select__vacio">Error al cargar</div>';
      });
  }

  function inicializar(select, opts) {
    if (!select) return null;
    destruir(select);

    var opciones = opts || {};
    if (!select.value && opciones.defaultText !== false) {
      var valorDefault = encontrarValorPorTexto(
        select,
        opciones.defaultText || "Puebla",
      );
      if (valorDefault) {
        select.value = valorDefault;
      }
    }

    select.classList.add("rosa-select--native");

    var wrapper = document.createElement("div");
    wrapper.className = "rosa-select";

    var control = document.createElement("div");
    control.className = "rosa-select__control";

    var input = document.createElement("input");
    input.type = "text";
    input.className = "rosa-select__input";
    input.placeholder = opciones.placeholder || "Buscar municipio...";
    input.autocomplete = "off";

    var icono = document.createElement("i");
    icono.className = "fa-solid fa-chevron-down rosa-select__icono";

    var dropdown = document.createElement("div");
    dropdown.className = "rosa-select__dropdown rosa-select__dropdown--local";

    var dropdownHost = document.createElement("div");
    dropdownHost.className = "rosa-select__dropdown rosa-select__dropdown--portal";
    dropdownHost.style.display = "none";
    document.body.appendChild(dropdownHost);

    control.appendChild(input);
    control.appendChild(icono);
    wrapper.appendChild(control);
    wrapper.appendChild(dropdown);

    select.parentNode.insertBefore(wrapper, select.nextSibling);

    var ajax = opciones.ajax || null;

    var instance = {
      select: select,
      wrapper: wrapper,
      control: control,
      input: input,
      dropdown: dropdown,
      dropdownHost: dropdownHost,
      labelSeleccionada: "",
      activeIndex: -1,
      ajax: ajax,
      _ajaxTimer: null,
    };

    function manejarInput() {
      if (input.disabled) return;
      wrapper.classList.add("abierto");
      instance.activeIndex = 0;

      if (ajax) {
        clearTimeout(instance._ajaxTimer);
        var termino = input.value;
        if (termino.length < (ajax.minimumInputLength || 1)) {
          instance.dropdownHost.innerHTML = '<div class="rosa-select__vacio">' +
            (ajax.minimumInputLength ? "Escribe al menos " + ajax.minimumInputLength + " caracteres..." : "Sin resultados") +
            '</div>';
          posicionarDropdown(instance);
          return;
        }
        instance._ajaxTimer = setTimeout(function () {
          cargarOpcionesAjax(instance, termino);
        }, ajax.delay || 250);
      } else {
        renderizarOpciones(instance, input.value);
        posicionarDropdown(instance);
      }
    }

    input.addEventListener("focus", function () {
      if (input.disabled) return;
      wrapper.classList.add("abierto");
      input.value = "";
      if (ajax) {
        cargarOpcionesAjax(instance, "");
      } else {
        renderizarOpciones(instance, "");
      }
      posicionarDropdown(instance);
    });

    input.addEventListener("input", manejarInput);

    input.addEventListener("click", function () {
      if (input.disabled) return;
      wrapper.classList.add("abierto");
      if (!ajax) {
        renderizarOpciones(instance, input.value);
      }
      posicionarDropdown(instance);
    });

    input.addEventListener("keydown", function (e) {
      if (e.key === "ArrowDown") {
        e.preventDefault();
        wrapper.classList.add("abierto");
        renderizarOpciones(instance, input.value);
        moverOpcionActiva(instance, 1);
        posicionarDropdown(instance);
        return;
      }

      if (e.key === "ArrowUp") {
        e.preventDefault();
        wrapper.classList.add("abierto");
        renderizarOpciones(instance, input.value);
        moverOpcionActiva(instance, -1);
        posicionarDropdown(instance);
        return;
      }

      if (e.key === "Enter") {
        e.preventDefault();
        if (!wrapper.classList.contains("abierto")) {
          wrapper.classList.add("abierto");
          renderizarOpciones(instance, input.value);
          posicionarDropdown(instance);
        }
        seleccionarOpcionActiva(instance);
        return;
      }

      if (e.key === "Escape") {
        cerrarInstancia(instance);
        input.blur();
      }
    });

    select.addEventListener("change", instance._changeHandler = function () {
      sincronizarDesdeSelect(instance);
    });

    instance._documentHandler = function (e) {
      if (!wrapper.contains(e.target) && !dropdownHost.contains(e.target)) {
        cerrarInstancia(instance);
      }
    };
    document.addEventListener("mousedown", instance._documentHandler);
    instance._repositionHandler = function () {
      if (instance.wrapper.classList.contains("abierto")) {
        posicionarDropdown(instance);
      }
    };
    window.addEventListener("resize", instance._repositionHandler);
    window.addEventListener("scroll", instance._repositionHandler, true);

    select._rosaSelectBuscable = instance;
    sincronizarDesdeSelect(instance);
    return instance;
  }

  function destruir(select) {
    var instance = select && select._rosaSelectBuscable;
    if (!instance) return;
    clearTimeout(instance._ajaxTimer);
    document.removeEventListener("mousedown", instance._documentHandler);
    window.removeEventListener("resize", instance._repositionHandler);
    window.removeEventListener("scroll", instance._repositionHandler, true);
    instance.select.removeEventListener("change", instance._changeHandler);
    instance.wrapper.remove();
    if (instance.dropdownHost) {
      instance.dropdownHost.remove();
    }
    instance.select.classList.remove("rosa-select--native");
    delete instance.select._rosaSelectBuscable;
  }

  window.SelectRosaBuscable = {
    inicializar: inicializar,
    destruir: destruir,
    actualizar: function (select) {
      var instance = select && select._rosaSelectBuscable;
      if (!instance) return null;
      sincronizarDesdeSelect(instance);
      return instance;
    },
    reinicializar: function (select, opts) {
      destruir(select);
      return inicializar(select, opts);
    },
  };
})();
