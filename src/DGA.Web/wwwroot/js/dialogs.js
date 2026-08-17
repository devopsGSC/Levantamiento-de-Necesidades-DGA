(function () {
  var ICONOS = {
    peligro: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z" /><line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>',
    pregunta: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" /><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>',
    success: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" /><path d="m22 4-10 10-3-3" /></svg>',
    warning: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.46 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z" /><line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>',
    danger: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" /></svg>',
  };

  // ---------------------------------------------------------------
  // Confirmación (reemplaza window.confirm)
  // ---------------------------------------------------------------

  var overlay = document.getElementById('dga-confirm');
  if (overlay) {
    var titulo = document.getElementById('dga-confirm-title');
    var cuerpo = document.getElementById('dga-confirm-body');
    var icono = document.getElementById('dga-confirm-icon');
    var btnOk = document.getElementById('dga-confirm-ok');
    var btnCancelar = document.getElementById('dga-confirm-cancel');
    var resolver = null;

    function cerrarConfirm(resultado) {
      overlay.hidden = true;
      if (resolver) {
        var r = resolver;
        resolver = null;
        r(resultado);
      }
    }

    window.dgaConfirm = function (mensaje, opciones) {
      opciones = opciones || {};
      titulo.textContent = opciones.titulo || (opciones.peligroso ? 'Confirmar acción' : 'Confirmar');
      cuerpo.textContent = mensaje;
      btnOk.textContent = opciones.textoConfirmar || 'Confirmar';
      btnOk.className = 'btn ' + (opciones.peligroso ? 'btn-danger' : 'btn-primary');
      icono.className = 'modal-icon ' + (opciones.peligroso ? 'is-peligroso' : 'is-normal');
      icono.innerHTML = opciones.peligroso ? ICONOS.peligro : ICONOS.pregunta;
      overlay.hidden = false;
      btnOk.focus();
      return new Promise(function (resolve) {
        resolver = resolve;
      });
    };

    btnOk.addEventListener('click', function () { cerrarConfirm(true); });
    btnCancelar.addEventListener('click', function () { cerrarConfirm(false); });
    overlay.addEventListener('click', function (e) {
      if (e.target === overlay) cerrarConfirm(false);
    });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && !overlay.hidden) cerrarConfirm(false);
    });

    // Cualquier <form data-confirm="mensaje"> se intercepta automáticamente:
    // muestra el modal y recién ahí envía el form (form.submit() no vuelve a
    // disparar 'submit', así que no hay riesgo de loop).
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
      form.addEventListener('submit', function (e) {
        if (form.dataset.confirmado === 'true') {
          return;
        }
        e.preventDefault();
        window.dgaConfirm(form.dataset.confirm, { peligroso: form.dataset.confirmPeligroso === 'true' })
          .then(function (ok) {
            if (ok) {
              form.dataset.confirmado = 'true';
              window.dgaFormEnviando?.(form);
              form.submit();
            }
          });
      });
    });
  }

  // ---------------------------------------------------------------
  // Toast (reemplaza window.alert para avisos no bloqueantes)
  // ---------------------------------------------------------------

  var stack = document.getElementById('dga-toast-stack');
  if (stack) {
    window.dgaToast = function (mensaje, tipo) {
      tipo = tipo || 'warning';
      var el = document.createElement('div');
      el.className = 'toast is-' + tipo;
      el.innerHTML =
        '<span class="toast__icon">' + (ICONOS[tipo] || ICONOS.warning) + '</span>' +
        '<span class="toast__body"></span>' +
        '<button type="button" class="toast__close" aria-label="Cerrar">&times;</button>';
      el.querySelector('.toast__body').textContent = mensaje;
      stack.appendChild(el);

      var quitado = false;
      function quitar() {
        if (quitado) return;
        quitado = true;
        el.classList.add('is-leaving');
        setTimeout(function () { el.remove(); }, 180);
      }

      el.querySelector('.toast__close').addEventListener('click', quitar);
      setTimeout(quitar, 4200);
    };
  }
})();
