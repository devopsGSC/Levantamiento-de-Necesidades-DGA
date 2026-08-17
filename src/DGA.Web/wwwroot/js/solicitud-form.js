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
    limpiarInvalidoAlEditar(document.querySelector('[name="' + campo.nombre + '"]'));
  });

  function validarInformacionGeneral() {
    for (const campo of CAMPOS_OBLIGATORIOS_GENERAL) {
      const el = document.querySelector('[name="' + campo.nombre + '"]');
      if (el && !el.value) {
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
      elementoSel.disabled = true;
      return;
    }

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
      detalleSel.innerHTML = '<option value="">Seleccione el detalle exacto requerido</option>';
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
  }

  componenteSel.addEventListener('change', () => cargarSubcomponentes(componenteSel.value, null));
  subcomponenteSel.addEventListener('change', () => cargarElementos(subcomponenteSel.value, null));
  elementoSel.addEventListener('change', () => cargarDetalles(elementoSel.value, null));

  function resetElementoYDetalle() {
    wrapElementoSelect.hidden = false;
    wrapElementoChecklist.hidden = true;
    wrapElementoLibre.hidden = true;
    wrapDetalle.hidden = true;
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
      numeroItem,
      componenteId: Number(componenteSel.value),
      componenteNombre: componenteSel.selectedOptions[0].textContent,
      subcomponenteId: Number(subcomponenteSel.value),
      subcomponenteNombre: subcomponenteSel.selectedOptions[0].textContent,
      ...camposElemento,
      cantidadSolicitada: Number(document.getElementById('CantidadSolicitada').value) || 1,
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
    document.getElementById('UbicacionEspecifica').value = '';
    document.getElementById('JustificacionItem').value = '';
    fotosActuales = [];
    renderPhotoChips();
  }

  function renderTablaItems() {
    contadorItems.textContent = String(items.length);
    btnFinalizar.disabled = items.length === 0;

    if (items.length === 0) {
      tablaBody.innerHTML = '';
      tablaBody.appendChild(filaVacia);
      return;
    }

    tablaBody.innerHTML = items.map((it, idx) => {
      const elemento = it.elementoNombre || it.elementoLibre || '-';
      const detalle = it.detalleNombre ? ` — ${escapeHtml(it.detalleNombre)}` : '';
      const totalFotos = it.fotografiasNuevas.length + it.fotografiasExistentes.length;
      return `<tr>
        <td>${idx + 1}</td>
        <td>${escapeHtml(it.prioridadNombre)}</td>
        <td>${escapeHtml(it.componenteNombre)}<br /><span class="muted">${escapeHtml(it.subcomponenteNombre)}</span></td>
        <td>${escapeHtml(elemento)}${detalle}</td>
        <td>${escapeHtml(it.ubicacionEspecifica || '-')}</td>
        <td>${it.cantidadSolicitada}</td>
        <td>${totalFotos || '-'}</td>
        <td>
          <button type="button" class="btn btn-outline btn-sm" data-accion="editar" data-idx="${idx}">Editar</button>
          <button type="button" class="btn btn-outline btn-sm" data-accion="eliminar" data-idx="${idx}" style="color:var(--dga-danger);">Eliminar</button>
        </td>
      </tr>`;
    }).join('');

    tablaBody.querySelectorAll('[data-accion="eliminar"]').forEach((btn) => {
      btn.addEventListener('click', () => {
        items.splice(Number(btn.dataset.idx), 1);
        items.forEach((it, i) => (it.numeroItem = i + 1));
        renderTablaItems();
      });
    });
    tablaBody.querySelectorAll('[data-accion="editar"]').forEach((btn) => {
      btn.addEventListener('click', () => cargarItemParaEditar(Number(btn.dataset.idx)));
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
