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

  if (menu && menu.parentElement !== document.body) {
    document.body.appendChild(menu);
  }

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

// Progressive submit feedback for forms that can take a noticeable time
// (e.g. avatar upload with server-side image processing).
(() => {
  const forms = document.querySelectorAll("form[data-busy-submit]");
  if (forms.length === 0) {
    return;
  }

  forms.forEach((form) => {
    form.addEventListener("submit", (event) => {
      if (event.defaultPrevented) {
        return;
      }

      if (form.classList.contains("is-submitting")) {
        // Already submitted once; block double-posts.
        event.preventDefault();
        return;
      }

      // Honour HTML constraint validation even when the form has novalidate
      // (novalidate only disables the browser's automatic pre-submit check).
      if (typeof form.reportValidity === "function" && !form.reportValidity()) {
        event.preventDefault();
        return;
      }

      form.classList.add("is-submitting");

      const busyLabel = form.getAttribute("data-busy-label") || "Working…";
      const status = form.querySelector("[data-busy-status]");
      let timerId = null;
      if (status) {
        status.hidden = false;
        status.textContent = busyLabel;
        if (form.getAttribute("data-busy-timer") === "true") {
          const startedAt = Date.now();
          const updateTimer = () => {
            const elapsed = Math.max(0, Math.floor((Date.now() - startedAt) / 1000));
            status.textContent = `${busyLabel} (${elapsed}s)`;
          };
          updateTimer();
          timerId = window.setInterval(updateTimer, 1000);
        }
      }

      const submitters = form.querySelectorAll('button[type="submit"], input[type="submit"]');
      submitters.forEach((control) => {
        if (control instanceof HTMLButtonElement) {
          control.classList.add("is-busy");
          control.setAttribute("aria-busy", "true");
          control.textContent = busyLabel;
          // Disable after the current event so the browser still completes this submit.
          window.setTimeout(() => {
            control.disabled = true;
          }, 0);
        } else if (control instanceof HTMLInputElement) {
          control.classList.add("is-busy");
          control.setAttribute("aria-busy", "true");
          control.value = busyLabel;
          window.setTimeout(() => {
            control.disabled = true;
          }, 0);
        }
      });

      window.addEventListener("pageshow", () => {
        if (timerId !== null) {
          window.clearInterval(timerId);
        }
      }, { once: true });
    });
  });
})();

// Apply per-element inline styles that come from server-rendered data (percentages,
// computed bar heights, avatar colours) via the CSSOM instead of a style="" attribute,
// so these stay CSP-compliant without needing style-src 'unsafe-inline'.
(() => {
  document.querySelectorAll("[data-bar-height]").forEach((el) => {
    el.style.setProperty("height", el.dataset.barHeight);
  });

  document.querySelectorAll("[data-bar-width]").forEach((el) => {
    el.style.setProperty("width", el.dataset.barWidth);
  });

  document.querySelectorAll("[data-avatar-bg]").forEach((el) => {
    el.style.setProperty("--qz-avatar-bg", el.dataset.avatarBg);
  });
})();
