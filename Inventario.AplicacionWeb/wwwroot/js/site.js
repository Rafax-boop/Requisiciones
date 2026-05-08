// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.checkAnimadoBulletHtml = [
  '<span class="check-animado__bullet">',
  '<span class="check-animado__line zero"></span>',
  '<span class="check-animado__line one"></span>',
  '<span class="check-animado__line two"></span>',
  '<span class="check-animado__line three"></span>',
  '<span class="check-animado__line four"></span>',
  '<span class="check-animado__line five"></span>',
  '<span class="check-animado__line six"></span>',
  '<span class="check-animado__line seven"></span>',
  '</span>'
].join("");

window.crearCheckAnimado = function (config) {
  config = config || {};

  var labelClass = "check-animado";
  if (config.text) labelClass += " check-animado--choice";
  if (config.className) labelClass += " " + config.className;

  var inputClass = "check-animado__input";
  if (config.inputClass) inputClass += " " + config.inputClass;

  var inputHtml =
    '<input type="' + (config.type || "checkbox") + '"' +
    ' class="' + inputClass + '"' +
    (config.name ? ' name="' + config.name + '"' : "") +
    (config.value !== undefined ? ' value="' + config.value + '"' : "") +
    (config.id ? ' id="' + config.id + '"' : "") +
    (config.checked ? " checked" : "") +
    (config.required ? " required" : "") +
    (config.attrs ? " " + config.attrs : "") +
    " />";

  var textHtml = config.text
    ? '<span class="check-animado__text">' + config.text + "</span>"
    : "";

  return (
    '<label class="' + labelClass + '"' +
    (config.title ? ' title="' + config.title + '"' : "") +
    ">" +
    inputHtml +
    window.checkAnimadoBulletHtml +
    textHtml +
    "</label>"
  );
};
