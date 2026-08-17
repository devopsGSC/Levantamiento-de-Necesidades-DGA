/**
 * Feedback visual de "esto está procesando" para toda la app: spinner + deshabilitar
 * en botones/links, sin tener que instrumentar cada formulario a mano.
 *
 * Cobertura automática (sin cambios en las vistas): cualquier <form> con un
 * <button type="submit">/<input type="submit"> que se envía de la forma normal
 * (click del usuario, sin JS de por medio) — dialogs.js y solicitud-form.js llaman
 * a dgaFormEnviando/dgaBotonCargando a mano para los flujos que envían el form vía
 * JS (form.submit() no dispara el evento 'submit', así que ahí no hay forma de
 * enganchar esto automáticamente).
 */
(function () {
  var SPINNER_SVG =
    '<svg class="dga-spinner" viewBox="0 0 24 24" fill="none" aria-hidden="true">' +
    '<circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-dasharray="42 100"></circle>' +
    '</svg>';

  function crearSpinner() {
    var envoltorio = document.createElement('span');
    envoltorio.innerHTML = SPINNER_SVG;
    return envoltorio.firstElementChild;
  }

  function botonCargando(el, cargando) {
    if (!el) return;
    var yaCargando = el.classList.contains('is-cargando');
    if (cargando === yaCargando) return;

    el.classList.toggle('is-cargando', cargando);
    if ('disabled' in el) {
      el.disabled = cargando;
    } else if (cargando) {
      el.setAttribute('aria-disabled', 'true');
    } else {
      el.removeAttribute('aria-disabled');
    }
    if (cargando) {
      el.prepend(crearSpinner());
    } else {
      el.querySelector('.dga-spinner')?.remove();
    }
  }

  // Formulario a punto de enviarse (real o vía JS): si tiene un botón de submit
  // visible, se le pone el spinner; si no (ej. un <select> que se auto-envía al
  // cambiar), se deshabilita todo el formulario como señal genérica de "procesando".
  function formEnviando(form) {
    if (!form || form.classList.contains('is-submitting')) return;
    var boton = form.querySelector('button[type="submit"]:not([disabled]), input[type="submit"]:not([disabled])');
    if (boton) {
      botonCargando(boton, true);
      return;
    }
    form.classList.add('is-submitting');
    form.querySelectorAll('select, input, textarea, button').forEach(function (el) { el.disabled = true; });
  }

  document.addEventListener('submit', function (e) {
    var form = e.target;
    if (e.defaultPrevented || !(form instanceof HTMLFormElement) || form.dataset.loadingManual === 'true') return;
    formEnviando(form);
  });

  // Los links de descarga (PDF/Excel/plantillas) no navegan a otra página — el
  // navegador solo dispara la descarga y se queda en la misma vista — así que no
  // hay ningún evento que avise cuándo termina. Se revierte solo, a los pocos
  // segundos, en vez de quedar "cargando" para siempre.
  document.addEventListener('click', function (e) {
    var link = e.target.closest('a[href*="Descargar"]');
    if (!link || link.classList.contains('is-cargando')) return;
    botonCargando(link, true);
    setTimeout(function () { botonCargando(link, false); }, 3500);
  });

  window.dgaCrearSpinner = crearSpinner;
  window.dgaBotonCargando = botonCargando;
  window.dgaFormEnviando = formEnviando;
})();
