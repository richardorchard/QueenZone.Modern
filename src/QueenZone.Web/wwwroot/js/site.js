(() => {
  const masthead = document.querySelector("[data-masthead]");
  if (!masthead) {
    return;
  }

  const groups = Array.from(masthead.querySelectorAll("[data-nav-group]"));
  const menu = masthead.querySelector("[data-mobile-menu]");
  const menuOpen = masthead.querySelector("[data-menu-open]");
  const menuClose = Array.from(masthead.querySelectorAll("[data-menu-close]"));
  let openGroup = null;
  let closeTimer = 0;
  let lastFocus = null;

  const setScrolled = () => {
    masthead.classList.toggle("is-scrolled", window.scrollY > 12);
  };

  const closeGroup = () => {
    if (!openGroup) {
      return;
    }

    openGroup.trigger.setAttribute("aria-expanded", "false");
    openGroup.panel.hidden = true;
    openGroup = null;
  };

  const openGroupPanel = (group) => {
    window.clearTimeout(closeTimer);
    if (openGroup && openGroup !== group) {
      closeGroup();
    }

    group.trigger.setAttribute("aria-expanded", "true");
    group.panel.hidden = false;
    openGroup = group;
  };

  const scheduleClose = () => {
    window.clearTimeout(closeTimer);
    closeTimer = window.setTimeout(closeGroup, 130);
  };

  const focusPanelItem = (group, index) => {
    const items = Array.from(group.panel.querySelectorAll("a"));
    if (items.length === 0) {
      return;
    }

    items[(index + items.length) % items.length].focus();
  };

  groups.forEach((groupEl) => {
    const group = {
      el: groupEl,
      trigger: groupEl.querySelector("[data-nav-trigger]"),
      panel: groupEl.querySelector("[data-nav-panel]")
    };

    groupEl.addEventListener("pointerenter", (event) => {
      if (event.pointerType === "mouse") {
        openGroupPanel(group);
      }
    });

    groupEl.addEventListener("pointerleave", (event) => {
      if (event.pointerType === "mouse") {
        scheduleClose();
      }
    });

    group.trigger.addEventListener("click", () => {
      if (openGroup === group) {
        closeGroup();
      } else {
        openGroupPanel(group);
      }
    });

    group.trigger.addEventListener("keydown", (event) => {
      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        openGroupPanel(group);
        focusPanelItem(group, event.key === "ArrowDown" ? 0 : -1);
      }
    });

    group.panel.addEventListener("keydown", (event) => {
      const items = Array.from(group.panel.querySelectorAll("a"));
      const currentIndex = items.indexOf(document.activeElement);

      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        focusPanelItem(group, currentIndex + (event.key === "ArrowDown" ? 1 : -1));
      }

      if (event.key === "Escape") {
        event.preventDefault();
        closeGroup();
        group.trigger.focus();
      }

      if (event.key === "Tab") {
        closeGroup();
      }
    });
  });

  document.addEventListener("click", (event) => {
    if (!masthead.contains(event.target)) {
      closeGroup();
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && openGroup) {
      const trigger = openGroup.trigger;
      closeGroup();
      trigger.focus();
    }
  });

  const focusableSelector = "a[href], button:not([disabled])";

  const closeMenu = () => {
    if (!menu || menu.hidden) {
      return;
    }

    menu.hidden = true;
    document.body.classList.remove("qz-menu-lock");
    menuOpen?.setAttribute("aria-expanded", "false");
    lastFocus?.focus();
  };

  const openMenu = () => {
    if (!menu || !menuOpen) {
      return;
    }

    lastFocus = document.activeElement;
    menu.hidden = false;
    document.body.classList.add("qz-menu-lock");
    menuOpen.setAttribute("aria-expanded", "true");
    menu.querySelector(focusableSelector)?.focus();
  };

  menuOpen?.addEventListener("click", openMenu);
  menuClose.forEach((control) => control.addEventListener("click", closeMenu));

  menu?.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      event.preventDefault();
      closeMenu();
      return;
    }

    if (event.key !== "Tab") {
      return;
    }

    const focusable = Array.from(menu.querySelectorAll(focusableSelector));
    if (focusable.length === 0) {
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  });

  window.addEventListener("scroll", setScrolled, { passive: true });
  setScrolled();
})();

// ── Homepage archive hero — era montage ──────────────────────────────────────
(() => {
  const configEl = document.getElementById("qz-era-config");
  const ERAS = configEl ? JSON.parse(configEl.textContent) : [];

  const hero    = document.getElementById("qz-hero-archive");
  if (!hero || ERAS.length === 0) return;

  const glowEl  = document.getElementById("qz-era-glow");
  const labelEl = document.getElementById("qz-era-label");
  const yearEl  = document.getElementById("qz-era-year");
  const imgEl   = document.getElementById("qz-era-img");
  const btns    = Array.from(hero.querySelectorAll(".qz-era-btn"));

  let current = 0;
  let timer;

  function applyEra(index) {
    current = index;
    const era = ERAS[index];

    glowEl.style.background = `radial-gradient(closest-side, ${era.glow}55, transparent 72%)`;
    labelEl.textContent     = era.label;
    yearEl.textContent      = era.year;
    imgEl.src               = era.img;
    imgEl.alt               = `Queenzone.com in ${era.year}`;

    btns.forEach((btn, n) => {
      const active = n === index;
      btn.classList.toggle("qz-era-btn--active", active);
      btn.setAttribute("aria-pressed", String(active));
    });
  }

  function startTimer() {
    clearInterval(timer);
    timer = setInterval(() => applyEra((current + 1) % ERAS.length), 3600);
  }

  btns.forEach((btn, n) => {
    btn.addEventListener("click", () => {
      applyEra(n);
      startTimer(); // restart interval after manual jump
    });
  });

  applyEra(0);
  startTimer();
})();
