/**
 * Convierte un <select> en un combo con barra de búsqueda: un input de texto que filtra
 * las opciones a medida que se escribe. El <select> original sigue existiendo (oculto) y
 * sigue siendo la fuente de verdad — el resto del código (cascadas, validaciones, lectura
 * de .value/.selectedOptions) no necesita cambiar.
 */
(function () {
  function escapeHtml(str) {
    return String(str ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  }

  function dgaComboBuscable(select) {
    if (select.dataset.comboBuscable === 'listo') return;
    select.dataset.comboBuscable = 'listo';

    const wrapper = document.createElement('div');
    wrapper.className = 'combo-buscable';
    select.parentNode.insertBefore(wrapper, select);
    wrapper.appendChild(select);
    select.classList.add('combo-buscable__select-oculto');

    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'form-control combo-buscable__input';
    input.setAttribute('autocomplete', 'off');
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-expanded', 'false');
    wrapper.appendChild(input);

    // El select queda oculto pero sigue siendo el elemento real del formulario; el resto
    // de la app (marcarInvalido/limpiarInvalido en solicitud-form.js) necesita saber cuál
    // es el input visible para aplicarle el estilo ".is-invalid" y el foco.
    select._comboInput = input;

    const lista = document.createElement('div');
    lista.className = 'combo-buscable__lista';
    lista.hidden = true;
    wrapper.appendChild(lista);

    let indiceActivo = -1;

    function opcionesReales() {
      return Array.from(select.options).filter((o) => o.value !== '');
    }

    function sincronizar() {
      const opt = select.selectedIndex >= 0 ? select.options[select.selectedIndex] : null;
      input.value = opt && opt.value !== '' ? opt.text : '';
      input.placeholder = opt ? opt.text : '';
      input.disabled = select.disabled;

      const cargando = select.dataset.cargando === 'true';
      wrapper.classList.toggle('is-cargando', cargando);
      const spinnerActual = wrapper.querySelector('.dga-spinner');
      if (cargando && !spinnerActual) {
        wrapper.appendChild(window.dgaCrearSpinner());
      } else if (!cargando && spinnerActual) {
        spinnerActual.remove();
      }
    }

    function resaltar(opciones) {
      opciones.forEach((o, i) => o.classList.toggle('is-activa', i === indiceActivo));
      opciones[indiceActivo]?.scrollIntoView({ block: 'nearest' });
    }

    function renderLista(filtro) {
      const texto = filtro.trim().toLocaleLowerCase();
      const filtradas = texto
        ? opcionesReales().filter((o) => o.text.toLocaleLowerCase().includes(texto))
        : opcionesReales();

      lista.innerHTML = filtradas.length
        ? filtradas.map((o) => `<div class="combo-buscable__opcion" data-value="${escapeHtml(o.value)}">${escapeHtml(o.text)}</div>`).join('')
        : '<div class="combo-buscable__vacio">Sin resultados</div>';

      indiceActivo = -1;
      lista.hidden = false;
      input.setAttribute('aria-expanded', 'true');
    }

    function cerrarLista() {
      lista.hidden = true;
      indiceActivo = -1;
      input.setAttribute('aria-expanded', 'false');
    }

    function seleccionar(value) {
      select.value = value;
      select.dispatchEvent(new Event('change', { bubbles: true }));
      cerrarLista();
    }

    input.addEventListener('focus', () => {
      renderLista('');
      input.select();
    });
    input.addEventListener('input', () => renderLista(input.value));
    input.addEventListener('blur', () => {
      sincronizar();
      cerrarLista();
    });
    input.addEventListener('keydown', (e) => {
      if (lista.hidden) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
          e.preventDefault();
          renderLista(input.value);
        }
        return;
      }
      const opciones = Array.from(lista.querySelectorAll('.combo-buscable__opcion'));
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        indiceActivo = Math.min(indiceActivo + 1, opciones.length - 1);
        resaltar(opciones);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        indiceActivo = Math.max(indiceActivo - 1, 0);
        resaltar(opciones);
      } else if (e.key === 'Enter') {
        e.preventDefault();
        const activa = opciones[indiceActivo] || opciones[0];
        if (activa) seleccionar(activa.dataset.value);
      } else if (e.key === 'Escape') {
        sincronizar();
        cerrarLista();
      }
    });
    lista.addEventListener('mousedown', (e) => {
      // preventDefault evita que el input pierda el foco (blur) antes de procesar el click.
      e.preventDefault();
      const opt = e.target.closest('.combo-buscable__opcion');
      if (opt) seleccionar(opt.dataset.value);
    });

    // El resto de la app manipula el <select> directamente (innerHTML con nuevas <option>,
    // .disabled, .value) para armar las cascadas de Componente → Subcomponente → Elemento →
    // Detalle. En vez de tocar ese código, se detectan esos cambios acá para mantener el
    // input sincronizado: MutationObserver capta los reemplazos de <option> y el atributo
    // "disabled" (que sí se refleja); .value no se refleja como atributo, así que se
    // intercepta el setter de la propiedad.
    new MutationObserver(sincronizar).observe(select, { childList: true, attributes: true, attributeFilter: ['disabled', 'data-cargando'] });

    const valorOriginal = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'value');
    Object.defineProperty(select, 'value', {
      get() { return valorOriginal.get.call(select); },
      set(v) { valorOriginal.set.call(select, v); sincronizar(); },
      configurable: true,
    });

    sincronizar();
  }

  window.dgaComboBuscable = dgaComboBuscable;
})();
