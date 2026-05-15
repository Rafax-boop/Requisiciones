# Plan: Homologar estilos de consolidadas con requisiciones regulares

## Cambios a realizar

### 1. Función JS compartida: `renderBadgeEstatus()`

**Archivo:** `wwwroot/js/requisicion/tabla-requisiciones.js`

Insertar antes de `window.cargarConsolidadas` (line ~3370):

```javascript
function renderBadgeEstatus(idEstatus, nombreEstatus) {
    var colorClass, iconClass;
    switch (idEstatus) {
        case 7: case 12: case 17:
            colorClass = "badge-estado-success";
            iconClass = "fa-solid fa-check-double";
            break;
        case 4: case 10: case 15: case 16:
            colorClass = "badge-estado-purple";
            iconClass = "fa-solid fa-user-check";
            break;
        case 5: case 6:
            colorClass = "badge-estado-danger";
            iconClass = "fa-solid fa-ban";
            break;
        case 3: case 18:
            colorClass = "badge-estado-warning";
            iconClass = "fa-solid fa-triangle-exclamation";
            break;
        default:
            colorClass = "badge-estado-info";
            iconClass = idEstatus === 1
                ? "fa-solid fa-file-signature"
                : "fa-solid fa-spinner fa-spin-pulse";
            break;
    }
    return '<div class="badge-estado-premium ' + colorClass + '">'
        + '<i class="' + iconClass + '"></i>'
        + '<span class="badge-text">' + (nombreEstatus || "—") + '</span></div>';
}
```

### 2. Aplicar badge en tabla de consolidadas (Requisiciones)

**Archivo:** `wwwroot/js/requisicion/tabla-requisiciones.js`

**Función `cargarConsolidadas()`** — línea 3402:
- Cambiar: `"<td>" + (c.estatus || "—") + "</td>"`
- Por: `"<td>" + renderBadgeEstatus(c.idEstatus, c.estatus) + "</td>"`

**Función `cargarVerificadasConsolidadas()`** — línea 3455:
- Cambiar: `"<td>" + (c.estatus || "—") + "</td>"`
- Por: `"<td>" + renderBadgeEstatus(c.idEstatus, c.estatus) + "</td>"`

### 3. Aplicar badge en tabla de consolidadas (Financieros)

**Archivo:** `wwwroot/js/financieros/tabla-financieros.js`

**Función `cargarConsolidadas()`** — línea 1478:
- Cambiar: `var badgeHtml = item.estatus || "";`
- Por: Copiar la función `renderBadgeEstatus` (al inicio del scope o global) y usar:
  ```javascript
  var badgeHtml = renderBadgeEstatus(item.idEstatus, item.estatus);
  ```

### 4. Estandarizar tamaños de modales

**Archivo:** `Views/Requisicion/TablaRequisiciones.cshtml`

| Modal | Línea | Cambio |
|-------|-------|--------|
| `modalDetalleConsolidada` | 1402 | Agregar `.modal-detalle-requisicion` al `modal-dialog` |
| `modalAtenderConsolidada` | 1466 | Cambiar `modal-lg` → `modal-xl` y agregar `.modal-detalle-requisicion` |
| `modalCrearConsolidada` | 1315 | Agregar `.modal-gestionar-requisicion` al `modal-dialog` |

**Archivo:** `wwwroot/css/componentes/modal.css`
- Línea 230-232: Agregar `!important` a `.modal-premium-compacta`:
  ```css
  .modal-premium-compacta {
      max-width: 700px !important;
      margin: 0 auto;
  }
  ```

### 5. Lugar de entrega en pedido consolidado

**Archivo:** `Inventarios.BLL/Implementacion/FinancierosService.cs`

Línea 2636:
- Cambiar: `LugarEntrega = "RECURSOS MATERIALES"`
- Por: `LugarEntrega = "ALMACEN GENERAL"`

## Archivos afectados (resumen)

| Archivo | Cambio |
|---------|--------|
| `wwwroot/js/requisicion/tabla-requisiciones.js` | +renderBadgeEstatus(), usar en cargarConsolidadas y cargarVerificadasConsolidadas |
| `wwwroot/js/financieros/tabla-financieros.js` | +renderBadgeEstatus(), usar en cargarConsolidadas |
| `Views/Requisicion/TablaRequisiciones.cshtml` | Ajustar clases CSS en 3 modales |
| `wwwroot/css/componentes/modal.css` | Agregar !important a .modal-premium-compacta |
| `Inventarios.BLL/Implementacion/FinancierosService.cs` | Cambiar LugarEntrega a "ALMACEN GENERAL" |
