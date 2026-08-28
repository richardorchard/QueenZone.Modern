(function () {
  "use strict";

  document.querySelectorAll("[data-gallery-picker]").forEach(bindPicker);

  function bindPicker(root) {
    var host = root.closest("[data-gallery-picker-host]") || root;
    var openButton = root.querySelector("[data-gallery-picker-open]");
    var dialog = host.querySelector("[data-gallery-picker-dialog]");
    var panel = host.querySelector("[data-gallery-picker-panel]");
    var closeButton = host.querySelector("[data-gallery-picker-close]");
    var blobKeyInput = host.querySelector("[data-article-image-blob-key]");
    var galleryIdInput = host.querySelector("[data-article-image-gallery-id]");
    var fileInput = root.querySelector("[data-article-image-input]");
    var preview = root.querySelector("[data-article-image-preview]");
    if (!openButton || !dialog || !panel) {
      return;
    }

    var pickerUrl = openButton.getAttribute("data-gallery-picker-url") || "/admin/news/gallery-picker";

    openButton.addEventListener("click", function () {
      if (typeof dialog.showModal === "function") {
        dialog.showModal();
      }

      if (!panel.getAttribute("data-loaded")) {
        loadPanel(panel, pickerUrl);
      }
    });

    closeButton && closeButton.addEventListener("click", function () {
      dialog.close();
    });

    dialog.addEventListener("cancel", function (event) {
      event.preventDefault();
      dialog.close();
    });

    panel.addEventListener("submit", function (event) {
      var form = event.target.closest("[data-gallery-picker-filter]");
      if (!form) {
        return;
      }

      event.preventDefault();
      loadPanel(panel, form.action + "?" + new URLSearchParams(new FormData(form)).toString());
    });

    panel.addEventListener("click", function (event) {
      var pageLink = event.target.closest("[data-gallery-picker-page]");
      if (pageLink) {
        event.preventDefault();
        loadPanel(panel, pageLink.getAttribute("href"));
        return;
      }

      var pick = event.target.closest("[data-gallery-pick]");
      if (!pick) {
        return;
      }

      var picId = pick.getAttribute("data-pic-id");
      var imageUrl = pick.getAttribute("data-image-url");
      var title = pick.getAttribute("data-title") || "Article image";
      if (!picId) {
        return;
      }

      if (galleryIdInput) {
        galleryIdInput.value = picId;
      }

      if (blobKeyInput) {
        blobKeyInput.value = "gallery:" + picId;
      }

      if (fileInput) {
        fileInput.value = "";
      }

      resetCropFields(root);

      if (preview && imageUrl) {
        preview.src = imageUrl;
        preview.alt = title;
        preview.style.objectPosition = "";
      }

      var caption = root.querySelector(".admin-article-image figcaption");
      if (caption) {
        caption.remove();
      }

      dialog.close();
    });
  }

  function loadPanel(panel, url) {
    panel.setAttribute("data-loaded", "1");
    panel.setAttribute("aria-busy", "true");
    fetch(url, { headers: { "X-Requested-With": "fetch" } })
      .then(function (response) {
        if (!response.ok) {
          throw new Error("Could not load gallery photos.");
        }

        return response.text();
      })
      .then(function (html) {
        var parsed = document.createElement("div");
        parsed.innerHTML = html;
        var next = parsed.querySelector("[data-gallery-picker-panel]");
        panel.innerHTML = next ? next.innerHTML : html;
        panel.setAttribute("aria-busy", "false");
      })
      .catch(function () {
        panel.innerHTML = "<p class=\"admin-help\">Could not load gallery photos.</p>";
        panel.setAttribute("aria-busy", "false");
      });
  }

  function resetCropFields(root) {
    ["data-crop-x", "data-crop-y", "data-crop-width", "data-crop-height"].forEach(function (name) {
      var field = root.querySelector("[" + name + "]");
      if (field) {
        field.value = "";
      }
    });
  }
})();
