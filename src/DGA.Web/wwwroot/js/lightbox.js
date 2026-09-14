(function () {
  const overlay = document.getElementById('dga-lightbox');
  if (!overlay) return;

  const img = document.getElementById('dga-lightbox-img');
  const btnClose = document.getElementById('dga-lightbox-close');
  const btnPrev = document.getElementById('dga-lightbox-prev');
  const btnNext = document.getElementById('dga-lightbox-next');
  const contador = document.getElementById('dga-lightbox-count');

  let items = [];
  let indice = 0;

  function mostrar() {
    const actual = items[indice];
    img.src = actual.src;
    img.alt = actual.alt || '';
    overlay.hidden = false;

    const hayVarias = items.length > 1;
    btnPrev.hidden = !hayVarias;
    btnNext.hidden = !hayVarias;
    if (hayVarias) {
      contador.hidden = false;
      contador.textContent = (indice + 1) + ' / ' + items.length;
    } else {
      contador.hidden = true;
    }
  }

  function anterior() {
    if (items.length < 2) return;
    indice = (indice - 1 + items.length) % items.length;
    mostrar();
  }

  function siguiente() {
    if (items.length < 2) return;
    indice = (indice + 1) % items.length;
    mostrar();
  }

  window.dgaLightbox = function (srcOAItems, altOIndice) {
    if (Array.isArray(srcOAItems)) {
      items = srcOAItems;
      indice = altOIndice || 0;
    } else {
      items = [{ src: srcOAItems, alt: altOIndice || '' }];
      indice = 0;
    }
    mostrar();
  };

  function cerrar() {
    overlay.hidden = true;
    img.src = '';
    items = [];
    indice = 0;
  }

  btnClose.addEventListener('click', cerrar);
  btnPrev.addEventListener('click', anterior);
  btnNext.addEventListener('click', siguiente);
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) cerrar();
  });
  document.addEventListener('keydown', (e) => {
    if (overlay.hidden) return;
    if (e.key === 'Escape') cerrar();
    else if (e.key === 'ArrowLeft') anterior();
    else if (e.key === 'ArrowRight') siguiente();
  });
})();
