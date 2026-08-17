(function () {
  const overlay = document.getElementById('dga-lightbox');
  if (!overlay) return;

  const img = document.getElementById('dga-lightbox-img');
  const btnClose = document.getElementById('dga-lightbox-close');

  window.dgaLightbox = function (src, alt) {
    img.src = src;
    img.alt = alt || '';
    overlay.hidden = false;
  };

  function cerrar() {
    overlay.hidden = true;
    img.src = '';
  }

  btnClose.addEventListener('click', cerrar);
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) cerrar();
  });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !overlay.hidden) cerrar();
  });
})();
