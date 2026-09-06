document.querySelectorAll('[data-password-visibility]').forEach((toggle) => {
  toggle.addEventListener('change', () => {
    const inputType = toggle.checked ? 'text' : 'password';
    toggle.dataset.passwordVisibility.split(',').forEach((id) => {
      const input = document.getElementById(id);
      if (input) {
        input.type = inputType;
      }
    });
  });
});
