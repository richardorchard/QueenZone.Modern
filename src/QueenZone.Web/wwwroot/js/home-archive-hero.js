// Homepage archive hero — era montage (loaded only on /).
(() => {
  const configEl = document.getElementById("qz-era-config");
  const ERAS = configEl ? JSON.parse(configEl.textContent) : [];

  const hero = document.getElementById("qz-hero-archive");
  if (!hero || ERAS.length === 0) {
    return;
  }

  const glowEl = document.getElementById("qz-era-glow");
  const labelEl = document.getElementById("qz-era-label");
  const yearEl = document.getElementById("qz-era-year");
  const imgEl = document.getElementById("qz-era-img");
  const btns = Array.from(hero.querySelectorAll(".qz-era-btn"));

  let current = 0;
  let timer = 0;

  const prefersReducedMotion = () =>
    window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function applyEra(index) {
    current = index;
    const era = ERAS[index];

    glowEl.style.background = `radial-gradient(closest-side, ${era.glow}55, transparent 72%)`;
    labelEl.textContent = era.label;
    yearEl.textContent = era.year;
    imgEl.src = era.img;
    imgEl.alt = `Queenzone.com in ${era.year}`;

    btns.forEach((btn, n) => {
      const active = n === index;
      btn.classList.toggle("qz-era-btn--active", active);
      btn.setAttribute("aria-pressed", String(active));
    });
  }

  function stopTimer() {
    if (timer) {
      clearInterval(timer);
      timer = 0;
    }
  }

  function startTimer() {
    stopTimer();
    if (prefersReducedMotion() || document.hidden) {
      return;
    }

    timer = setInterval(() => applyEra((current + 1) % ERAS.length), 3600);
  }

  btns.forEach((btn, n) => {
    btn.addEventListener("click", () => {
      applyEra(n);
      startTimer();
    });
  });

  document.addEventListener("visibilitychange", () => {
    if (document.hidden) {
      stopTimer();
    } else {
      startTimer();
    }
  });

  if (window.matchMedia) {
    const motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    const onMotionChange = () => {
      if (prefersReducedMotion()) {
        stopTimer();
      } else {
        startTimer();
      }
    };

    if (typeof motionQuery.addEventListener === "function") {
      motionQuery.addEventListener("change", onMotionChange);
    } else if (typeof motionQuery.addListener === "function") {
      motionQuery.addListener(onMotionChange);
    }
  }

  applyEra(0);
  startTimer();
})();
