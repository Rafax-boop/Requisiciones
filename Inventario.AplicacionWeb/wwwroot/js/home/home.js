/**
 * Home - Fecha y hora en tiempo real
 */
(function () {
    var opciones = {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    };

    function actualizarFechaHora() {
        var ahora = new Date();
        var texto = ahora.toLocaleDateString('es-MX', opciones);
        texto = texto.charAt(0).toUpperCase() + texto.slice(1);
        var el = document.getElementById('fecha-hora-pc');
        if (el) el.textContent = texto;
    }

    actualizarFechaHora();
    setInterval(actualizarFechaHora, 1000);
})();
