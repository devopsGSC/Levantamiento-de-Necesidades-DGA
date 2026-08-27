(function () {
  const boot = window.__solicitudBootstrap || { esEdicion: false, itemsExistentes: [] };
  const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

  /** @type {Array<any>} */
  let items = Array.isArray(boot.itemsExistentes) ? boot.itemsExistentes.map(normalizarItemExistente) : [];
  /** Fotos adjuntadas al ítem que se está armando ahora mismo (todavía no agregado a la lista). */
  let fotosActuales = [];
  /** Si no es null, estamos editando ese índice de `items` en vez de agregar uno nuevo. */
  let indiceEnEdicion = null;

  function normalizarItemExistente(i) {
    return {
      id: i.id ?? 0,
      numeroItem: i.numeroItem,
      componenteId: i.componenteId,
      componenteNombre: i.componenteNombre,
      subcomponenteId: i.subcomponenteId,
      subcomponenteNombre: i.subcomponenteNombre,
      elementoId: i.elementoId ?? null,
      elementoNombre: i.elementoNombre ?? null,
      elementoLibre: i.elementoLibre ?? null,
      detalleId: i.detalleId ?? null,
      detalleNombre: i.detalleNombre ?? null,
      cantidadSolicitada: i.cantidadSolicitada,
      tienePresupuesto: i.tienePresupuesto ?? false,
      costoEstimado: i.costoEstimado ?? 0,
      tipoCosto: i.tipoCosto || 'Unitario',
      cotizacionRutaExistente: i.cotizacionRutaExistente ?? null,
      cotizacionNombreExistente: i.cotizacionNombreExistente ?? null,
      tipoSuscripcion: i.tipoSuscripcion ?? null,
      cantidadPeriodos: i.cantidadPeriodos ?? null,
      prioridadId: i.prioridadId,
      prioridadNombre: i.prioridadNombre ?? prioridadNombrePorId(i.prioridadId),
      ubicacionEspecifica: i.ubicacionEspecifica ?? null,
      justificacionItem: i.justificacionItem ?? null,
      fotografiasNuevas: [],
      fotografiasExistentes: i.fotografiasExistentes ?? [],
    };
  }

  function prioridadNombrePorId(id) {
    const opt = document.querySelector('#PrioridadId option[value="' + id + '"]');
    return opt ? opt.textContent : '';
  }

  function formatoMoneda(valor) {
    return '$' + (Number(valor) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ---------------------------------------------------------------
  // Validación de campos obligatorios — helpers compartidos
  // ---------------------------------------------------------------
  // Los campos de este formulario (asp-for con [Required]) solo llevan atributos
  // data-val-required de jQuery Unobtrusive Validation, no el atributo HTML5
  // `required` — así que `el.reportValidity()` no muestra nada (no hay ninguna
  // restricción nativa que violar). Por eso la validación de "¿está lleno?" se
  // hace a mano acá, con mensaje puntual + resaltado visual del campo.

  // Los <select> de Componente/Subcomponente/Elemento/Detalle quedan ocultos detrás de un
  // combo con búsqueda (ver combo-buscable.js) — el estilo de error y el foco tienen que
  // aplicarse sobre el input visible (`el._comboInput`), no sobre el select oculto.

  function marcarInvalido(el, mensaje) {
    const visible = el._comboInput || el;
    visible.classList.add('is-invalid');
    visible.focus();
    dgaToast(mensaje, 'warning');
  }

  function limpiarInvalido(el) {
    (el._comboInput || el).classList.remove('is-invalid');
  }

  function limpiarInvalidoAlEditar(el) {
    if (!el) return;
    el.addEventListener('input', () => limpiarInvalido(el));
    el.addEventListener('change', () => limpiarInvalido(el));
  }

  // ---------------------------------------------------------------
  // Sección 1: Información General — Confirmar Datos / Editar Info
  // ---------------------------------------------------------------

  const seccionGeneral = document.getElementById('seccion-general');
  const seccionItem = document.getElementById('seccion-item');
  const btnToggleGeneral = document.getElementById('btn-toggle-general');
  const camposGeneral = document.getElementById('campos-general');

  const ICONO_CANDADO_CERRADO = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>';
  const ICONO_CANDADO_ABIERTO = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 9.9-1"></path></svg>';

  function bloquearGeneral(bloqueado) {
    // OJO: nunca usar `disabled` acá — un campo disabled no se envía en el POST del
    // formulario. El bloqueo visual/interactivo ya lo da el CSS (.is-locked .form-grid
    // { pointer-events: none }), así que solo togglear la clase.
    seccionGeneral.classList.toggle('is-locked', bloqueado);
    seccionItem.classList.toggle('is-locked', !bloqueado);
    const etiqueta = bloqueado ? 'Editar Info' : 'Confirmar Datos';
    const icono = bloqueado ? ICONO_CANDADO_CERRADO : ICONO_CANDADO_ABIERTO;
    btnToggleGeneral.innerHTML = icono + '<span class="icon-toggle-btn__label">' + etiqueta + '</span>';
    btnToggleGeneral.classList.toggle('is-locked', bloqueado);
    btnToggleGeneral.setAttribute('aria-label', etiqueta);
    btnToggleGeneral.setAttribute('title', etiqueta);
  }

  const CAMPOS_OBLIGATORIOS_GENERAL = [
    { nombre: 'NombreResponsable', etiqueta: 'Nombre Solicitante' },
    { nombre: 'CargoId', etiqueta: 'Cargo' },
    { nombre: 'TipoAduanaId', etiqueta: 'Tipo de Aduana' },
    { nombre: 'AduanaId', etiqueta: 'Nombre de Aduana' },
    { nombre: 'JustificacionGeneral', etiqueta: 'Justificación General' },
  ];
  CAMPOS_OBLIGATORIOS_GENERAL.forEach((campo) => {
    // Un campo con más de un elemento del mismo name es un grupo de radios (ej. Unidad
    // Ejecutora, estilo checklist): el resaltado de error se aplica sobre el contenedor
    // `.elemento-checklist`, no sobre cada radio suelto (ver `_comboInput`, usado también
    // por combo-buscable.js para redirigir el estilo de un input real a su reemplazo visible).
    const elementos = Array.from(document.querySelectorAll('[name="' + campo.nombre + '"]'));
    const contenedor = elementos.length > 1 ? elementos[0].closest('.elemento-checklist') : null;
    elementos.forEach((el) => {
      if (contenedor) el._comboInput = contenedor;
      limpiarInvalidoAlEditar(el);
    });
  });

  function validarInformacionGeneral() {
    for (const campo of CAMPOS_OBLIGATORIOS_GENERAL) {
      const el = document.querySelector('[name="' + campo.nombre + '"]');
      if (!el) continue;
      const vacio = el.type === 'radio'
        ? !document.querySelector('input[name="' + campo.nombre + '"]:checked')
        : !el.value;
      if (vacio) {
        marcarInvalido(el, 'Completá "' + campo.etiqueta + '" antes de confirmar los datos.');
        return false;
      }
    }
    return true;
  }

  btnToggleGeneral.addEventListener('click', () => {
    const bloqueando = !btnToggleGeneral.classList.contains('is-locked');
    if (bloqueando && !validarInformacionGeneral()) {
      return;
    }
    bloquearGeneral(bloqueando);
  });

  // Empieza bloqueada (confirmada) si estamos editando una solicitud existente.
  bloquearGeneral(boot.esEdicion);

  // ---------------------------------------------------------------
  // Cascada Tipo de Aduana -> Aduana
  // ---------------------------------------------------------------

  const tipoAduanaSel = document.getElementById('TipoAduanaId');
  const aduanaSel = document.getElementById('AduanaId');

  async function cargarAduanas(tipoAduanaId, seleccionar) {
    aduanaSel.innerHTML = '<option value="">Cargando...</option>';
    aduanaSel.disabled = true;
    if (!tipoAduanaId) {
      aduanaSel.innerHTML = '<option value="">Seleccionar Aduana</option>';
      return;
    }
    const resp = await fetch('/Catalogos/Aduanas?tipoAduanaId=' + tipoAduanaId + (seleccionar ? '&incluir=' + seleccionar : ''));
    const datos = await resp.json();
    aduanaSel.innerHTML = '<option value="">Seleccionar Aduana</option>' +
      datos.map((a) => `<option value="${a.id}">${escapeHtml(a.nombre)}</option>`).join('');
    aduanaSel.disabled = false;
    aduanaSel.dataset.tieneOpciones = 'true';
    if (seleccionar) aduanaSel.value = String(seleccionar);
  }

  tipoAduanaSel.addEventListener('change', () => cargarAduanas(tipoAduanaSel.value, null));

  if (boot.tipoAduanaId) {
    tipoAduanaSel.value = String(boot.tipoAduanaId);
    cargarAduanas(boot.tipoAduanaId, boot.aduanaId);
  }

  // ---------------------------------------------------------------
  // Cascada Componente -> Subcomponente -> Elemento -> Detalle
  // ---------------------------------------------------------------

  const componenteSel = document.getElementById('ComponenteId');
  const subcomponenteSel = document.getElementById('SubcomponenteId');
  const elementoSel = document.getElementById('ElementoId');
  const elementoLibre = document.getElementById('ElementoLibre');
  const detalleSel = document.getElementById('DetalleId');
  const wrapElementoSelect = document.getElementById('wrap-elemento-select');
  const wrapElementoChecklist = document.getElementById('wrap-elemento-checklist');
  const elementoChecklist = document.getElementById('elemento-checklist');
  const elementoChecklistBuscar = document.getElementById('elemento-checklist-buscar');
  const elementoChecklistVacio = document.getElementById('elemento-checklist-vacio');
  const wrapElementoLibre = document.getElementById('wrap-elemento-libre');
  const wrapDetalle = document.getElementById('wrap-detalle');

  // ---------------------------------------------------------------
  // Ítems de suscripción (Internet, Telefonía) — algunos puntos fijos del catálogo
  // (ver DGA.Web.Data.CatalogoSuscripciones, IDs recibidos en boot) no son una compra
  // única: piden Tipo de Suscripción (Mensual/Anual) + Cantidad de Períodos, y el
  // subtotal del ítem multiplica por esa cantidad además de Costo × Cantidad.
  // ---------------------------------------------------------------

  const elementoIdsSuscripcion = boot.elementoIdsSuscripcion || [];
  const detalleIdsSuscripcion = boot.detalleIdsSuscripcion || [];
  const wrapSuscripcion = document.getElementById('wrap-suscripcion');
  const tipoSuscripcionSel = document.getElementById('TipoSuscripcion');
  const cantidadPeriodosInput = document.getElementById('CantidadPeriodos');
  const labelCantidadPeriodos = document.getElementById('label-cantidad-periodos');
  const labelCostoEstimado = document.getElementById('label-costo-estimado');
  const tipoCostoSel = document.getElementById('TipoCosto');
  limpiarInvalidoAlEditar(tipoCostoSel);
  limpiarInvalidoAlEditar(document.getElementById('CostoEstimado'));

  // ---------------------------------------------------------------
  // ¿Tiene monto presupuestado? — el Costo/Tipo de Costo/Cotización son opcionales;
  // por defecto el ítem no tiene presupuesto y toda esa sección queda oculta.
  // ---------------------------------------------------------------

  const wrapCosto = document.getElementById('wrap-costo');
  const tienePresupuestoRadios = Array.from(document.querySelectorAll('input[name="tiene-presupuesto"]'));

  function tienePresupuesto() {
    return document.querySelector('input[name="tiene-presupuesto"]:checked')?.value === 'si';
  }

  tienePresupuestoRadios.forEach((r) => r.addEventListener('change', () => {
    wrapCosto.hidden = !tienePresupuesto();
  }));

  function actualizarEtiquetaPeriodos() {
    labelCantidadPeriodos.textContent = tipoSuscripcionSel.value === 'Anual' ? 'Cantidad de Años' : 'Cantidad de Meses';
  }
  tipoSuscripcionSel.addEventListener('change', actualizarEtiquetaPeriodos);

  /** Determina si el Elemento/Detalle elegido en este momento es una suscripción, y
   * muestra u oculta los campos de Tipo de Suscripción / Cantidad de Períodos. */
  function actualizarCamposSuscripcion() {
    let esSuscripcion;
    if (!wrapDetalle.hidden) {
      esSuscripcion = detalleSel.value !== '' && detalleIdsSuscripcion.includes(Number(detalleSel.value));
    } else if (!wrapElementoChecklist.hidden) {
      const marcado = elementoChecklist.querySelector('input:checked');
      esSuscripcion = !!marcado && elementoIdsSuscripcion.includes(Number(marcado.value));
    } else if (!wrapElementoSelect.hidden) {
      esSuscripcion = elementoSel.value !== '' && elementoIdsSuscripcion.includes(Number(elementoSel.value));
    } else {
      esSuscripcion = false; // "elemento libre" (texto) no tiene id de catálogo
    }

    wrapSuscripcion.hidden = !esSuscripcion;
    labelCostoEstimado.textContent = 'Costo Estimado *' + (esSuscripcion ? ' (por período)' : '');
    if (!esSuscripcion) {
      tipoSuscripcionSel.value = 'Mensual';
      cantidadPeriodosInput.value = 1;
    }
    actualizarEtiquetaPeriodos();
  }
  elementoChecklist.addEventListener('change', actualizarCamposSuscripcion);
  detalleSel.addEventListener('change', actualizarCamposSuscripcion);

  // ---------------------------------------------------------------
  // "Agregar nuevo" de Elemento / Detalle — cualquier usuario puede sumar al
  // catálogo lo que no encuentra en la lista mientras completa un ítem.
  // ---------------------------------------------------------------

  const agregarElemento = document.getElementById('agregar-elemento');
  const btnMostrarAgregarElemento = document.getElementById('btn-mostrar-agregar-elemento');
  const formAgregarElemento = document.getElementById('form-agregar-elemento');
  const nuevoElementoNombre = document.getElementById('nuevo-elemento-nombre');
  const btnGuardarElemento = document.getElementById('btn-guardar-elemento');
  const btnCancelarElemento = document.getElementById('btn-cancelar-elemento');

  const btnMostrarAgregarDetalle = document.getElementById('btn-mostrar-agregar-detalle');
  const formAgregarDetalle = document.getElementById('form-agregar-detalle');
  const nuevoDetalleNombre = document.getElementById('nuevo-detalle-nombre');
  const btnGuardarDetalle = document.getElementById('btn-guardar-detalle');
  const btnCancelarDetalle = document.getElementById('btn-cancelar-detalle');

  function ocultarFormAgregarElemento() {
    formAgregarElemento.hidden = true;
    btnMostrarAgregarElemento.hidden = false;
    nuevoElementoNombre.value = '';
  }
  function ocultarFormAgregarDetalle() {
    formAgregarDetalle.hidden = true;
    btnMostrarAgregarDetalle.hidden = false;
    nuevoDetalleNombre.value = '';
  }

  btnMostrarAgregarElemento.addEventListener('click', () => {
    formAgregarElemento.hidden = false;
    btnMostrarAgregarElemento.hidden = true;
    nuevoElementoNombre.focus();
  });
  btnCancelarElemento.addEventListener('click', ocultarFormAgregarElemento);

  btnMostrarAgregarDetalle.addEventListener('click', () => {
    formAgregarDetalle.hidden = false;
    btnMostrarAgregarDetalle.hidden = true;
    nuevoDetalleNombre.focus();
  });
  btnCancelarDetalle.addEventListener('click', ocultarFormAgregarDetalle);

  async function agregarElementoAlCatalogo() {
    const nombre = nuevoElementoNombre.value.trim();
    if (!nombre) {
      dgaToast('Ingresá el nombre del nuevo elemento.', 'warning');
      return;
    }
    const fd = new FormData();
    fd.append('subcomponenteId', subcomponenteSel.value);
    fd.append('nombre', nombre);
    fd.append('__RequestVerificationToken', token);
    dgaBotonCargando(btnGuardarElemento, true);
    try {
      const resp = await fetch('/Catalogos/CrearElemento', { method: 'POST', body: fd });
      const data = await resp.json();
      if (!resp.ok) {
        dgaToast(data.error || 'No se pudo agregar el elemento.', 'danger');
        return;
      }
      ocultarFormAgregarElemento();
      await cargarElementos(subcomponenteSel.value, data.id);
      dgaToast('"' + data.nombre + '" agregado. Ya está seleccionado.', 'success');
    } finally {
      dgaBotonCargando(btnGuardarElemento, false);
    }
  }
  btnGuardarElemento.addEventListener('click', agregarElementoAlCatalogo);
  nuevoElementoNombre.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') { e.preventDefault(); agregarElementoAlCatalogo(); }
  });

  async function agregarDetalleAlCatalogo() {
    const nombre = nuevoDetalleNombre.value.trim();
    if (!nombre) {
      dgaToast('Ingresá el nombre del nuevo detalle.', 'warning');
      return;
    }
    const fd = new FormData();
    fd.append('elementoId', elementoSel.value);
    fd.append('nombre', nombre);
    fd.append('__RequestVerificationToken', token);
    dgaBotonCargando(btnGuardarDetalle, true);
    try {
      const resp = await fetch('/Catalogos/CrearDetalle', { method: 'POST', body: fd });
      const data = await resp.json();
      if (!resp.ok) {
        dgaToast(data.error || 'No se pudo agregar el detalle.', 'danger');
        return;
      }
      ocultarFormAgregarDetalle();
      await cargarDetalles(elementoSel.value, data.id);
      dgaToast('"' + data.nombre + '" agregado. Ya está seleccionado.', 'success');
    } finally {
      dgaBotonCargando(btnGuardarDetalle, false);
    }
  }
  btnGuardarDetalle.addEventListener('click', agregarDetalleAlCatalogo);
  nuevoDetalleNombre.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') { e.preventDefault(); agregarDetalleAlCatalogo(); }
  });

  // Filtra la lista de radios de "Elemento / Necesidad" a medida que se escribe —
  // igual que combo-buscable.js pero sobre una lista siempre visible (no un <select>
  // oculto detrás de un popup), porque acá puede haber 30+ opciones para escanear.
  function filtrarElementoChecklist() {
    const texto = elementoChecklistBuscar.value.trim().toLocaleLowerCase();
    const opciones = Array.from(elementoChecklist.querySelectorAll('.elemento-checklist__item'));
    let visibles = 0;
    opciones.forEach((opcion) => {
      const coincide = !texto || opcion.textContent.toLocaleLowerCase().includes(texto);
      opcion.hidden = !coincide;
      if (coincide) visibles++;
    });
    elementoChecklistVacio.hidden = opciones.length === 0 || visibles > 0;
  }
  elementoChecklistBuscar.addEventListener('input', filtrarElementoChecklist);

  [componenteSel, subcomponenteSel, elementoSel, detalleSel].forEach((sel) => window.dgaComboBuscable(sel));

  // Estas tres se definen como funciones con nombre (en vez de handlers anónimos)
  // para poder llamarlas directamente — y esperarlas de verdad con await — desde
  // cargarItemParaEditar(). `await elemento.dispatchEvent(...)` NO espera a que
  // termine un listener async (dispatchEvent devuelve un boolean, no una Promise),
  // así que simular el evento ahí dejaba el combo de Subcomponente a mitad de
  // cargar cuando el usuario ya estaba interactuando con el formulario.

  async function cargarSubcomponentes(componenteId, seleccionar) {
    resetElementoYDetalle();
    if (!componenteId) {
      subcomponenteSel.innerHTML = '<option value="">No aplica / Sin opciones</option>';
      subcomponenteSel.disabled = true;
      subcomponenteSel.dataset.cargando = 'false';
      return;
    }
    subcomponenteSel.innerHTML = '<option value="">Cargando...</option>';
    subcomponenteSel.disabled = true;
    subcomponenteSel.dataset.cargando = 'true';
    const resp = await fetch('/Catalogos/Subcomponentes?componenteId=' + componenteId + (seleccionar ? '&incluir=' + seleccionar : ''));
    const datos = await resp.json();
    subcomponenteSel.dataset.cargando = 'false';
    subcomponenteSel.innerHTML = '<option value="">Seleccione subcomponente</option>' +
      datos.map((s) => `<option value="${s.id}">${escapeHtml(s.nombre)}</option>`).join('');
    subcomponenteSel.disabled = false;
    if (seleccionar) subcomponenteSel.value = String(seleccionar);
  }

  async function cargarElementos(subcomponenteId, seleccionarElemento) {
    resetElementoYDetalle();
    if (!subcomponenteId) return;
    elementoSel.innerHTML = '<option value="">Cargando...</option>';
    elementoSel.disabled = true;
    elementoSel.dataset.cargando = 'true';
    const resp = await fetch('/Catalogos/Elementos?subcomponenteId=' + subcomponenteId + (seleccionarElemento ? '&incluir=' + seleccionarElemento : ''));
    const datos = await resp.json();
    elementoSel.dataset.cargando = 'false';

    if (datos.length === 0) {
      wrapElementoSelect.hidden = true;
      wrapElementoChecklist.hidden = true;
      wrapElementoLibre.hidden = false;
      agregarElemento.hidden = true;
      elementoSel.disabled = true;
      return;
    }

    agregarElemento.hidden = false;

    // Checklist (lista con selección única, más fácil de escanear que un combo
    // largo) solo cuando hay más de una opción Y ninguna necesita el 4º nivel
    // "Detalle" — un Detalle es un catálogo propio de CADA elemento, y esta
    // lista no reemplaza esa cascada. En los datos reales esto nunca se mezcla:
    // un subcomponente o tiene puros elementos con Detalle, o ninguno (ver
    // Elementos en la BD), así que la regla nunca deja afuera casos.
    const algunoConDetalle = datos.some((e) => e.tieneDetalle);
    if (datos.length > 1 && !algunoConDetalle) {
      wrapElementoSelect.hidden = true;
      wrapElementoChecklist.hidden = false;
      wrapElementoLibre.hidden = true;
      elementoChecklistBuscar.value = '';
      elementoChecklistVacio.hidden = true;
      elementoChecklist.innerHTML = datos.map((e) =>
        `<label class="elemento-checklist__item">
          <input type="radio" name="elemento-checklist-radio" value="${e.id}" data-nombre="${escapeHtml(e.nombre)}" />
          <span>${escapeHtml(e.nombre)}</span>
        </label>`
      ).join('');
      if (seleccionarElemento) {
        const cb = elementoChecklist.querySelector('input[value="' + seleccionarElemento + '"]');
        if (cb) cb.checked = true;
      }
      actualizarCamposSuscripcion();
      return;
    }

    wrapElementoSelect.hidden = false;
    wrapElementoChecklist.hidden = true;
    wrapElementoLibre.hidden = true;
    elementoSel.disabled = false;
    elementoSel.innerHTML = '<option value="">Seleccione elemento específico</option>' +
      datos.map((e) => `<option value="${e.id}" data-tiene-detalle="${e.tieneDetalle}">${escapeHtml(e.nombre)}</option>`).join('');
    if (seleccionarElemento) elementoSel.value = String(seleccionarElemento);
  }

  async function cargarDetalles(elementoId, seleccionarDetalle) {
    const opt = elementoId && elementoSel.querySelector(`option[value="${elementoId}"]`);
    const tieneDetalle = opt && (opt.dataset.tieneDetalle === 'True' || opt.dataset.tieneDetalle === 'true');
    if (!tieneDetalle) {
      wrapDetalle.hidden = true;
      ocultarFormAgregarDetalle();
      detalleSel.innerHTML = '<option value="">Seleccione el detalle exacto requerido</option>';
      actualizarCamposSuscripcion();
      return;
    }
    wrapDetalle.hidden = false;
    detalleSel.innerHTML = '<option value="">Cargando...</option>';
    detalleSel.disabled = true;
    detalleSel.dataset.cargando = 'true';
    const resp = await fetch('/Catalogos/Detalles?elementoId=' + elementoId + (seleccionarDetalle ? '&incluir=' + seleccionarDetalle : ''));
    const datos = await resp.json();
    detalleSel.dataset.cargando = 'false';
    detalleSel.innerHTML = '<option value="">Seleccione el detalle exacto requerido</option>' +
      datos.map((d) => `<option value="${d.id}">${escapeHtml(d.nombre)}</option>`).join('');
    detalleSel.disabled = false;
    if (seleccionarDetalle) detalleSel.value = String(seleccionarDetalle);
    actualizarCamposSuscripcion();
  }

  componenteSel.addEventListener('change', () => cargarSubcomponentes(componenteSel.value, null));
  subcomponenteSel.addEventListener('change', () => cargarElementos(subcomponenteSel.value, null));
  elementoSel.addEventListener('change', () => cargarDetalles(elementoSel.value, null));

  function resetElementoYDetalle() {
    wrapElementoSelect.hidden = false;
    wrapElementoChecklist.hidden = true;
    wrapElementoLibre.hidden = true;
    wrapDetalle.hidden = true;
    agregarElemento.hidden = true;
    ocultarFormAgregarElemento();
    ocultarFormAgregarDetalle();
    wrapSuscripcion.hidden = true;
    labelCostoEstimado.textContent = 'Costo Estimado *';
    tipoSuscripcionSel.value = 'Mensual';
    cantidadPeriodosInput.value = 1;
    elementoSel.innerHTML = '<option value="">Seleccione elemento específico</option>';
    elementoSel.disabled = true;
    elementoChecklist.innerHTML = '';
    elementoChecklistBuscar.value = '';
    elementoChecklistVacio.hidden = true;
    elementoLibre.value = '';
    detalleSel.innerHTML = '<option value="">Seleccione el detalle exacto requerido</option>';
  }

  // ---------------------------------------------------------------
  // Fotografías del ítem en construcción
  // ---------------------------------------------------------------

  const dropzone = document.getElementById('dropzone');
  const inputFotos = document.getElementById('input-fotos');
  const photoChips = document.getElementById('photo-chips');

  dropzone.addEventListener('click', () => inputFotos.click());
  dropzone.addEventListener('dragover', (e) => e.preventDefault());
  dropzone.addEventListener('drop', (e) => {
    e.preventDefault();
    subirArchivos(Array.from(e.dataTransfer.files));
  });
  inputFotos.addEventListener('change', () => {
    // OJO: `inputFotos.files` es una FileList VIVA — si se le pasa tal cual a
    // subirArchivos() (async) y después se limpia `inputFotos.value` acá abajo,
    // esa misma lista queda vacía a mitad de la subida (apenas se resuelve el
    // primer `await fetch`) y el resto de los archivos se pierde en silencio.
    // Por eso hay que copiarla a un array común ANTES de limpiar el input.
    const archivos = Array.from(inputFotos.files);
    inputFotos.value = '';
    subirArchivos(archivos);
  });

  async function subirArchivos(fileList) {
    if (fotosActuales.length + fileList.length > 10) {
      dgaToast('Máximo 10 fotos por ítem.', 'warning');
      return;
    }
    dropzone.classList.add('is-cargando');
    const spinnerDropzone = window.dgaCrearSpinner();
    dropzone.prepend(spinnerDropzone);
    try {
      for (const file of fileList) {
        const formData = new FormData();
        formData.append('archivo', file);
        formData.append('__RequestVerificationToken', token);
        const resp = await fetch('/Solicitudes/SubirFotoTemp', { method: 'POST', body: formData });
        const data = await resp.json();
        if (!resp.ok || !data.ok) {
          dgaToast(data.error || 'No se pudo subir la foto.', 'danger');
          continue;
        }
        fotosActuales.push({ tipo: 'nueva', token: data.token, nombre: data.nombre });
      }
    } finally {
      spinnerDropzone.remove();
      dropzone.classList.remove('is-cargando');
    }
    renderPhotoChips();
  }

  function urlPreviewFoto(f) {
    return f.tipo === 'nueva'
      ? '/Solicitudes/FotoTemp?token=' + encodeURIComponent(f.token)
      : '/Solicitudes/Foto?solicitudItemFotografiaId=' + f.id;
  }

  function renderPhotoChips() {
    photoChips.innerHTML = fotosActuales.map((f, idx) =>
      `<span class="photo-chip">
        <img class="photo-chip__thumb" src="${urlPreviewFoto(f)}" alt="" data-idx="${idx}" loading="lazy" />
        <span>${escapeHtml(f.nombre)}</span>
        <button type="button" data-idx="${idx}" aria-label="Quitar">×</button>
      </span>`
    ).join('');
    photoChips.querySelectorAll('img.photo-chip__thumb').forEach((img) => {
      img.addEventListener('click', () => {
        const foto = fotosActuales[Number(img.dataset.idx)];
        window.dgaLightbox?.(urlPreviewFoto(foto), foto.nombre);
      });
    });
    photoChips.querySelectorAll('button').forEach((btn) => {
      btn.addEventListener('click', () => {
        const idx = Number(btn.dataset.idx);
        const foto = fotosActuales[idx];
        dgaConfirm('¿Quitar la foto "' + foto.nombre + '"?', { peligroso: true, textoConfirmar: 'Quitar' }).then((ok) => {
          if (!ok) return;
          if (foto.tipo === 'nueva') {
            const fd = new FormData();
            fd.append('token', foto.token);
            fd.append('__RequestVerificationToken', token);
            fetch('/Solicitudes/EliminarFotoTemp', { method: 'POST', body: fd });
          }
          fotosActuales.splice(idx, 1);
          renderPhotoChips();
        });
      });
    });
  }

  // ---------------------------------------------------------------
  // Cotización adjunta al ítem en construcción — un solo archivo (imagen o PDF),
  // opcional, a diferencia de las fotos que aceptan varias y solo imagen.
  // ---------------------------------------------------------------

  const dropzoneCotizacion = document.getElementById('dropzone-cotizacion');
  const inputCotizacion = document.getElementById('input-cotizacion');
  const cotizacionChip = document.getElementById('cotizacion-chip');
  /** @type {{tipo: 'nueva', token: string, nombre: string} | {tipo: 'existente', solicitudItemId: number, ruta: string, nombre: string} | null} */
  let cotizacionActual = null;

  dropzoneCotizacion.addEventListener('click', () => inputCotizacion.click());
  dropzoneCotizacion.addEventListener('dragover', (e) => e.preventDefault());
  dropzoneCotizacion.addEventListener('drop', (e) => {
    e.preventDefault();
    if (e.dataTransfer.files[0]) subirCotizacion(e.dataTransfer.files[0]);
  });
  inputCotizacion.addEventListener('change', () => {
    const file = inputCotizacion.files[0];
    inputCotizacion.value = '';
    if (file) subirCotizacion(file);
  });

  async function subirCotizacion(file) {
    dropzoneCotizacion.classList.add('is-cargando');
    const spinnerDropzone = window.dgaCrearSpinner();
    dropzoneCotizacion.prepend(spinnerDropzone);
    try {
      const formData = new FormData();
      formData.append('archivo', file);
      formData.append('__RequestVerificationToken', token);
      const resp = await fetch('/Solicitudes/SubirCotizacionTemp', { method: 'POST', body: formData });
      const data = await resp.json();
      if (!resp.ok || !data.ok) {
        dgaToast(data.error || 'No se pudo subir la cotización.', 'danger');
        return;
      }
      // Si ya había una subida sin guardar todavía (el usuario adjuntó otra encima), se
      // libera el temporal anterior para no dejar archivos huérfanos.
      if (cotizacionActual && cotizacionActual.tipo === 'nueva') {
        const fd = new FormData();
        fd.append('token', cotizacionActual.token);
        fd.append('__RequestVerificationToken', token);
        fetch('/Solicitudes/EliminarCotizacionTemp', { method: 'POST', body: fd });
      }
      cotizacionActual = { tipo: 'nueva', token: data.token, nombre: data.nombre };
    } finally {
      spinnerDropzone.remove();
      dropzoneCotizacion.classList.remove('is-cargando');
    }
    renderCotizacionChip();
  }

  function urlPreviewCotizacion(c) {
    return c.tipo === 'nueva'
      ? '/Solicitudes/CotizacionTemp?token=' + encodeURIComponent(c.token)
      : '/Solicitudes/Cotizacion?solicitudItemId=' + c.solicitudItemId;
  }

  function esImagenPorNombre(nombre) {
    return /\.(jpe?g|png|gif|webp)$/i.test(nombre || '');
  }

  function renderCotizacionChip() {
    if (!cotizacionActual) {
      cotizacionChip.innerHTML = '';
      return;
    }
    const esImagen = esImagenPorNombre(cotizacionActual.nombre);
    cotizacionChip.innerHTML = `<span class="photo-chip">
      ${esImagen
        ? `<img class="photo-chip__thumb" src="${urlPreviewCotizacion(cotizacionActual)}" alt="" loading="lazy" style="cursor:pointer;" />`
        : `<span class="photo-chip__thumb" style="display:flex;align-items:center;justify-content:center;cursor:pointer;">📄</span>`}
      <span>${escapeHtml(cotizacionActual.nombre)}</span>
      <button type="button" aria-label="Quitar">×</button>
    </span>`;
    const previa = cotizacionChip.querySelector('.photo-chip__thumb');
    previa.addEventListener('click', () => {
      if (esImagen) {
        window.dgaLightbox?.(urlPreviewCotizacion(cotizacionActual), cotizacionActual.nombre);
      } else {
        window.open(urlPreviewCotizacion(cotizacionActual), '_blank');
      }
    });
    cotizacionChip.querySelector('button').addEventListener('click', () => {
      dgaConfirm('¿Quitar la cotización adjunta?', { peligroso: true, textoConfirmar: 'Quitar' }).then((ok) => {
        if (!ok) return;
        if (cotizacionActual.tipo === 'nueva') {
          const fd = new FormData();
          fd.append('token', cotizacionActual.token);
          fd.append('__RequestVerificationToken', token);
          fetch('/Solicitudes/EliminarCotizacionTemp', { method: 'POST', body: fd });
        }
        cotizacionActual = null;
        renderCotizacionChip();
      });
    });
  }

  // ---------------------------------------------------------------
  // Agregar / Editar / Eliminar ítems de la lista
  // ---------------------------------------------------------------

  const btnAgregarItem = document.getElementById('btn-agregar-item');
  const tablaBody = document.getElementById('tabla-items-body');
  const filaVacia = document.getElementById('fila-vacia');
  const contadorItems = document.getElementById('contador-items');
  const btnFinalizar = document.getElementById('btn-finalizar');

  [componenteSel, subcomponenteSel, elementoSel, elementoLibre, detalleSel].forEach(limpiarInvalidoAlEditar);

  /** Arma el objeto ítem a partir de los campos del Elemento (select / checklist / libre) + el resto del formulario. */
  function construirItem(camposElemento, numeroItem) {
    return {
      id: indiceEnEdicion !== null ? (items[indiceEnEdicion].id || 0) : 0,
      numeroItem,
      componenteId: Number(componenteSel.value),
      componenteNombre: componenteSel.selectedOptions[0].textContent,
      subcomponenteId: Number(subcomponenteSel.value),
      subcomponenteNombre: subcomponenteSel.selectedOptions[0].textContent,
      ...camposElemento,
      cantidadSolicitada: Number(document.getElementById('CantidadSolicitada').value) || 1,
      tienePresupuesto: tienePresupuesto(),
      costoEstimado: tienePresupuesto() ? (Number(document.getElementById('CostoEstimado').value) || 0) : 0,
      tipoCosto: tienePresupuesto() ? tipoCostoSel.value : 'Unitario',
      cotizacionTokenNuevo: tienePresupuesto() && cotizacionActual && cotizacionActual.tipo === 'nueva' ? cotizacionActual.token : null,
      cotizacionNombreOriginalNuevo: tienePresupuesto() && cotizacionActual && cotizacionActual.tipo === 'nueva' ? cotizacionActual.nombre : null,
      cotizacionRutaExistente: tienePresupuesto() && cotizacionActual && cotizacionActual.tipo === 'existente' ? cotizacionActual.ruta : null,
      cotizacionNombreExistente: tienePresupuesto() && cotizacionActual && cotizacionActual.tipo === 'existente' ? cotizacionActual.nombre : null,
      tipoSuscripcion: wrapSuscripcion.hidden ? null : tipoSuscripcionSel.value,
      cantidadPeriodos: wrapSuscripcion.hidden ? null : (Number(cantidadPeriodosInput.value) || 1),
      prioridadId: Number(document.getElementById('PrioridadId').value),
      prioridadNombre: document.getElementById('PrioridadId').selectedOptions[0].textContent,
      ubicacionEspecifica: document.getElementById('UbicacionEspecifica').value.trim() || null,
      justificacionItem: document.getElementById('JustificacionItem').value.trim() || null,
      fotografiasNuevas: fotosActuales.filter((f) => f.tipo === 'nueva').map((f) => f.token),
      fotografiasExistentes: fotosActuales.filter((f) => f.tipo === 'existente').map((f) => ({ id: f.id, ruta: f.ruta, nombreOriginal: f.nombre })),
    };
  }

  btnAgregarItem.addEventListener('click', () => {
    if (!componenteSel.value) {
      marcarInvalido(componenteSel, 'Seleccioná el Componente.');
      return;
    }
    if (!subcomponenteSel.value) {
      marcarInvalido(subcomponenteSel, 'Seleccioná el Subcomponente.');
      return;
    }

    const usaChecklist = !wrapElementoChecklist.hidden;
    const usaLibre = !wrapElementoLibre.hidden;

    let camposElemento;
    if (usaChecklist) {
      const marcado = elementoChecklist.querySelector('input:checked');
      if (!marcado) {
        dgaToast('Seleccioná el Elemento / Necesidad.', 'warning');
        return;
      }
      camposElemento = { elementoId: Number(marcado.value), elementoNombre: marcado.dataset.nombre, elementoLibre: null, detalleId: null, detalleNombre: null };
    } else if (usaLibre) {
      if (!elementoLibre.value.trim()) {
        marcarInvalido(elementoLibre, 'Ingresá el Elemento / Necesidad.');
        return;
      }
      camposElemento = { elementoId: null, elementoNombre: null, elementoLibre: elementoLibre.value.trim(), detalleId: null, detalleNombre: null };
    } else {
      if (!elementoSel.value) {
        marcarInvalido(elementoSel, 'Seleccioná el Elemento / Necesidad.');
        return;
      }
      if (!wrapDetalle.hidden && !detalleSel.value) {
        marcarInvalido(detalleSel, 'Seleccioná el Detalle Específico.');
        return;
      }
      camposElemento = {
        elementoId: Number(elementoSel.value),
        elementoNombre: elementoSel.selectedOptions[0].textContent,
        elementoLibre: null,
        detalleId: wrapDetalle.hidden ? null : Number(detalleSel.value),
        detalleNombre: wrapDetalle.hidden ? null : detalleSel.selectedOptions[0].textContent,
      };
    }

    if (tienePresupuesto()) {
      if (!tipoCostoSel.value) {
        marcarInvalido(tipoCostoSel, 'Seleccioná si el Costo Estimado es Unitario o Total.');
        return;
      }
      const costoEstimadoEl = document.getElementById('CostoEstimado');
      if (!(Number(costoEstimadoEl.value) > 0)) {
        marcarInvalido(costoEstimadoEl, 'Ingresá el Costo Estimado.');
        return;
      }
    }

    const eraEdicion = indiceEnEdicion !== null;
    const item = construirItem(camposElemento, eraEdicion ? items[indiceEnEdicion].numeroItem : items.length + 1);

    if (eraEdicion) {
      items[indiceEnEdicion] = item;
      indiceEnEdicion = null;
      btnAgregarItem.textContent = 'Guardar Ítem en la Lista';
    } else {
      items.push(item);
    }

    limpiarFormularioItem();
    renderTablaItems();
    dgaToast(eraEdicion ? 'Ítem actualizado en la lista.' : 'Ítem agregado a la lista.', 'success');
  });

  function limpiarFormularioItem() {
    componenteSel.value = '';
    subcomponenteSel.innerHTML = '<option value="">No aplica / Sin opciones</option>';
    subcomponenteSel.disabled = true;
    resetElementoYDetalle();
    document.getElementById('CantidadSolicitada').value = 1;
    tienePresupuestoRadios.forEach((r) => (r.checked = r.value === 'no'));
    wrapCosto.hidden = true;
    tipoCostoSel.value = '';
    document.getElementById('CostoEstimado').value = '';
    cotizacionActual = null;
    renderCotizacionChip();
    document.getElementById('UbicacionEspecifica').value = '';
    document.getElementById('JustificacionItem').value = '';
    fotosActuales = [];
    renderPhotoChips();
  }

  function subtotalItem(it) {
    const multiplicadorCantidad = it.tipoCosto === 'Total' ? 1 : it.cantidadSolicitada;
    return (Number(it.costoEstimado) || 0) * multiplicadorCantidad * (it.cantidadPeriodos || 1);
  }

  function montoPresupuestadoTotal() {
    return items.reduce((acc, it) => acc + subtotalItem(it), 0);
  }

  function renderTablaItems() {
    contadorItems.textContent = String(items.length);
    btnFinalizar.disabled = items.length === 0;
    document.getElementById('monto-presupuestado-total').textContent = formatoMoneda(montoPresupuestadoTotal());

    if (items.length === 0) {
      tablaBody.innerHTML = '';
      tablaBody.appendChild(filaVacia);
      return;
    }

    tablaBody.innerHTML = items.map((it, idx) => {
      const elemento = it.elementoNombre || it.elementoLibre || '-';
      const detalle = it.detalleNombre ? ` — ${escapeHtml(it.detalleNombre)}` : '';
      const suscripcion = it.tipoSuscripcion ? ` <span class="muted">(${escapeHtml(it.tipoSuscripcion)} × ${it.cantidadPeriodos})</span>` : '';
      const totalFotos = it.fotografiasNuevas.length + it.fotografiasExistentes.length;
      const subtotal = subtotalItem(it);
      const tipoCostoTag = ` <span class="muted">(${it.tipoCosto === 'Total' ? 'Total' : 'Unit.'})</span>`;
      const cotizacionTag = (it.cotizacionTokenNuevo || it.cotizacionRutaExistente) ? ' 📎' : '';
      return `<tr>
        <td>${idx + 1}</td>
        <td>${escapeHtml(it.prioridadNombre)}</td>
        <td>${escapeHtml(it.componenteNombre)}<br /><span class="muted">${escapeHtml(it.subcomponenteNombre)}</span></td>
        <td>${escapeHtml(elemento)}${detalle}${suscripcion}</td>
        <td>${escapeHtml(it.ubicacionEspecifica || '-')}</td>
        <td>${it.cantidadSolicitada}</td>
        <td>${it.tienePresupuesto ? formatoMoneda(it.costoEstimado) + tipoCostoTag : '<span class="muted">Sin presupuesto</span>'}</td>
        <td>${formatoMoneda(subtotal)}</td>
        <td>${totalFotos || '-'}${cotizacionTag}</td>
        <td>
          <button type="button" class="btn btn-outline btn-sm" data-accion="editar" data-idx="${idx}">Editar</button>
          <button type="button" class="btn btn-outline btn-sm" data-accion="eliminar" data-idx="${idx}" style="color:var(--dga-danger);">Eliminar</button>
        </td>
      </tr>`;
    }).join('');

    tablaBody.querySelectorAll('[data-accion="eliminar"]').forEach((btn) => {
      btn.addEventListener('click', () => {
        const idx = Number(btn.dataset.idx);
        dgaConfirm('¿Eliminar el ítem #' + (idx + 1) + ' de la lista?', { peligroso: true, textoConfirmar: 'Eliminar' }).then((ok) => {
          if (!ok) return;
          items.splice(idx, 1);
          items.forEach((it, i) => (it.numeroItem = i + 1));
          renderTablaItems();
        });
      });
    });
    tablaBody.querySelectorAll('[data-accion="editar"]').forEach((btn) => {
      btn.addEventListener('click', () => {
        const idx = Number(btn.dataset.idx);
        // Si ya hay datos sin guardar en el ítem que se está armando (uno nuevo a medio
        // completar, o ya se estaba editando otro), avisar antes de descartarlos.
        const hayDatosSinGuardar = indiceEnEdicion !== idx && (componenteSel.value || indiceEnEdicion !== null);
        if (!hayDatosSinGuardar) {
          cargarItemParaEditar(idx);
          return;
        }
        dgaConfirm('Tenés datos sin guardar en el ítem que estás armando. ¿Descartarlos y editar este otro ítem?', { peligroso: true, textoConfirmar: 'Descartar y editar' })
          .then((ok) => { if (ok) cargarItemParaEditar(idx); });
      });
    });
  }

  async function cargarItemParaEditar(idx) {
    const it = items[idx];
    indiceEnEdicion = idx;
    btnAgregarItem.textContent = 'Actualizar Ítem';

    componenteSel.value = String(it.componenteId);
    await cargarSubcomponentes(it.componenteId, it.subcomponenteId);
    if (it.elementoId) {
      await cargarElementos(it.subcomponenteId, it.elementoId);
      if (it.detalleId) {
        await cargarDetalles(it.elementoId, it.detalleId);
      }
    } else {
      await cargarElementos(it.subcomponenteId, null);
      elementoLibre.value = it.elementoLibre || '';
    }
    document.getElementById('CantidadSolicitada').value = it.cantidadSolicitada;
    tienePresupuestoRadios.forEach((r) => (r.checked = r.value === (it.tienePresupuesto ? 'si' : 'no')));
    wrapCosto.hidden = !it.tienePresupuesto;
    tipoCostoSel.value = it.tipoCosto || 'Unitario';
    document.getElementById('CostoEstimado').value = it.costoEstimado ?? '';
    if (it.cotizacionTokenNuevo) {
      cotizacionActual = { tipo: 'nueva', token: it.cotizacionTokenNuevo, nombre: it.cotizacionNombreOriginalNuevo || it.cotizacionTokenNuevo };
    } else if (it.cotizacionRutaExistente) {
      cotizacionActual = { tipo: 'existente', solicitudItemId: it.id, ruta: it.cotizacionRutaExistente, nombre: it.cotizacionNombreExistente || it.cotizacionRutaExistente.split('/').pop() };
    } else {
      cotizacionActual = null;
    }
    renderCotizacionChip();
    if (!wrapSuscripcion.hidden && it.tipoSuscripcion) {
      tipoSuscripcionSel.value = it.tipoSuscripcion;
      cantidadPeriodosInput.value = it.cantidadPeriodos ?? 1;
      actualizarEtiquetaPeriodos();
    }
    document.getElementById('PrioridadId').value = String(it.prioridadId);
    document.getElementById('UbicacionEspecifica').value = it.ubicacionEspecifica || '';
    document.getElementById('JustificacionItem').value = it.justificacionItem || '';
    fotosActuales = [
      ...it.fotografiasNuevas.map((t) => ({ tipo: 'nueva', token: t, nombre: t })),
      ...it.fotografiasExistentes.map((f) => ({ tipo: 'existente', id: f.id, ruta: f.ruta, nombre: f.nombreOriginal || f.ruta.split('/').pop() })),
    ];
    renderPhotoChips();
    seccionItem.scrollIntoView({ behavior: 'smooth' });
  }

  // ---------------------------------------------------------------
  // Guardar Borrador / Finalizar
  // ---------------------------------------------------------------

  const form = document.getElementById('solicitud-form');
  // form.submit() (más abajo) no dispara el evento 'submit', así que el spinner
  // genérico de loading.js no lo detectaría — se pone a mano en cada botón.
  form.dataset.loadingManual = 'true';
  const itemsJsonInput = document.getElementById('ItemsJson');
  const accionInput = document.getElementById('Accion');

  const btnGuardarBorrador = document.getElementById('btn-guardar-borrador');
  btnGuardarBorrador.addEventListener('click', async () => {
    if (items.length === 0) {
      dgaToast('Agregá al menos un ítem antes de guardar.', 'warning');
      return;
    }
    const ok = await dgaConfirm('¿Guardar esta solicitud como borrador? Vas a poder seguir editándola más adelante.');
    if (!ok) return;
    itemsJsonInput.value = JSON.stringify(items);
    accionInput.value = 'borrador';
    dgaBotonCargando(btnGuardarBorrador, true);
    form.submit();
  });

  const modal = document.getElementById('modal-confirmar');
  const checkConfirmo = document.getElementById('check-confirmo');
  const btnConfirmarEnvio = document.getElementById('btn-confirmar-envio');

  btnFinalizar.addEventListener('click', () => {
    document.getElementById('resumen-responsable').textContent = document.querySelector('[name="NombreResponsable"]').value || '-';
    document.getElementById('resumen-aduana').textContent = aduanaSel.selectedOptions[0]?.textContent || '-';
    document.getElementById('resumen-items').textContent = String(items.length);
    document.getElementById('resumen-presupuesto').textContent = formatoMoneda(montoPresupuestadoTotal());
    checkConfirmo.checked = false;
    btnConfirmarEnvio.disabled = true;
    modal.hidden = false;
  });

  checkConfirmo.addEventListener('change', () => (btnConfirmarEnvio.disabled = !checkConfirmo.checked));
  document.getElementById('btn-cerrar-modal').addEventListener('click', () => (modal.hidden = true));
  btnConfirmarEnvio.addEventListener('click', () => {
    itemsJsonInput.value = JSON.stringify(items);
    accionInput.value = 'finalizar';
    dgaBotonCargando(btnConfirmarEnvio, true);
    form.submit();
  });

  // ---------------------------------------------------------------

  function escapeHtml(str) {
    return String(str ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  }

  renderTablaItems();
})();
