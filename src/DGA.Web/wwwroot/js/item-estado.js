/**
 * Checkboxes de "Completado" y "Denegado" de cada ítem (AdminSolicitudes/Details y
 * MisRequerimientos/Details): al marcar/desmarcar cualquiera de los dos se pide un
 * comentario con dgaPrompt (queda en la bitácora) antes de enviar el formulario —
 * obligatorio para Denegado (el motivo lo ve el dueño de la solicitud), opcional para
 * Completado. Si se cancela el prompt, el checkbox vuelve a su estado anterior.
 */
(function () {
  function wireGrupo(selector, mensajes, obligatorio) {
    document.querySelectorAll(selector).forEach(function (checkbox) {
      checkbox.addEventListener('change', function () {
        var marcando = checkbox.checked;
        window.dgaPrompt(marcando ? mensajes.marcar : mensajes.desmarcar, { obligatorio: obligatorio }).then(function (comentario) {
          if (comentario === null) {
            checkbox.checked = !marcando;
            return;
          }
          var form = checkbox.closest('form');
          form.querySelector('input[name="comentario"]').value = comentario;
          form.querySelector('input[name="completado"], input[name="denegado"]').value = marcando ? 'true' : 'false';
          // Ojo: dgaFormEnviando deshabilita los campos del form (no tiene botón visible)
          // — llamarlo ANTES de submit() haría que el navegador los excluya del envío.
          form.submit();
          window.dgaFormEnviando?.(form);
        });
      });
    });
  }

  wireGrupo('.js-item-completado', {
    marcar: 'Comentario para marcar este ítem como completado (opcional, queda en la bitácora):',
    desmarcar: 'Comentario para desmarcar este ítem como completado (opcional, queda en la bitácora):',
  }, false);

  wireGrupo('.js-item-denegado', {
    marcar: 'Motivo por el que este ítem no aplica (obligatorio, lo ve el dueño de la solicitud):',
    desmarcar: 'Comentario para volver a habilitar este ítem (obligatorio, queda en la bitácora):',
  }, true);
})();
