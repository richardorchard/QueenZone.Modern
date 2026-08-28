(function () {
  "use strict";

  var allowedExtensions = { ".jpg": true, ".jpeg": true, ".png": true, ".webp": true };

  document.querySelectorAll("[data-article-image-crop]").forEach(bindCropper);

  function bindCropper(root) {
    var form = root.closest("form");
    var input = root.querySelector("[data-article-image-input]");
    var preview = root.querySelector("[data-article-image-preview]");
    var error = root.querySelector("[data-article-image-error]");
    var dialog = root.querySelector("[data-article-image-dialog]");
    var stage = root.querySelector("[data-article-image-stage]");
    var stageImg = root.querySelector("[data-article-image-stage-img]");
    var zoomInput = root.querySelector("[data-article-image-zoom]");
    var applyButton = root.querySelector("[data-article-image-apply]");
    var cancelButton = root.querySelector("[data-article-image-cancel]");
    var cropX = root.querySelector("[data-crop-x]");
    var cropY = root.querySelector("[data-crop-y]");
    var cropWidth = root.querySelector("[data-crop-width]");
    var cropHeight = root.querySelector("[data-crop-height]");
    if (!form || !input || !dialog || !stage || !stageImg || !zoomInput) {
      return;
    }

    var aspectWidth = Number(root.getAttribute("data-aspect-width")) || 3;
    var aspectHeight = Number(root.getAttribute("data-aspect-height")) || 2;
    var maxBytes = Number(root.getAttribute("data-max-bytes")) || 10 * 1024 * 1024;
    var objectUrl = "";
    var naturalWidth = 0;
    var naturalHeight = 0;
    var coverScale = 1;
    var zoom = 1;
    var translateX = 0;
    var translateY = 0;
    var drag = null;
    var cropApplied = false;

    input.addEventListener("change", function () {
      clearError();
      resetCropFields();
      cropApplied = false;
      var file = input.files && input.files[0];
      if (!file) {
        return;
      }

      var rejection = validateFile(file, maxBytes);
      if (rejection) {
        showError(rejection);
        input.value = "";
        return;
      }

      revokeObjectUrl();
      objectUrl = URL.createObjectURL(file);
      stageImg.onload = function () {
        naturalWidth = stageImg.naturalWidth;
        naturalHeight = stageImg.naturalHeight;
        resetView();
        if (typeof dialog.showModal === "function") {
          dialog.showModal();
        }
      };
      stageImg.src = objectUrl;
    });

    zoomInput.addEventListener("input", function () {
      zoom = Number(zoomInput.value) || 1;
      clampPan();
      render();
    });

    stage.addEventListener("pointerdown", function (event) {
      drag = { id: event.pointerId, x: event.clientX, y: event.clientY, tx: translateX, ty: translateY };
      stage.classList.add("is-dragging");
      stage.setPointerCapture(event.pointerId);
    });

    stage.addEventListener("pointermove", function (event) {
      if (!drag || drag.id !== event.pointerId) {
        return;
      }

      translateX = drag.tx + (event.clientX - drag.x);
      translateY = drag.ty + (event.clientY - drag.y);
      clampPan();
      render();
    });

    stage.addEventListener("pointerup", endDrag);
    stage.addEventListener("pointercancel", endDrag);

    applyButton && applyButton.addEventListener("click", function () {
      var crop = currentCrop();
      if (!crop) {
        showError("Could not read the crop. Try another image.");
        return;
      }

      cropX.value = String(crop.x);
      cropY.value = String(crop.y);
      cropWidth.value = String(crop.width);
      cropHeight.value = String(crop.height);
      cropApplied = true;
      if (preview) {
        preview.src = objectUrl;
        preview.alt = "Article image";
        preview.style.objectPosition =
          ((crop.x + crop.width / 2) / naturalWidth) * 100 + "% " +
          ((crop.y + crop.height / 2) / naturalHeight) * 100 + "%";
      }

      var caption = root.querySelector(".admin-article-image figcaption");
      if (caption) {
        caption.remove();
      }

      dialog.close();
    });

    cancelButton && cancelButton.addEventListener("click", cancelSelection);
    dialog.addEventListener("cancel", function (event) {
      event.preventDefault();
      cancelSelection();
    });

    form.addEventListener("submit", function (event) {
      if (!input.files || !input.files[0] || cropApplied) {
        return;
      }

      event.preventDefault();
      if (typeof dialog.showModal === "function") {
        dialog.showModal();
      }
    });

    function endDrag(event) {
      if (!drag || drag.id !== event.pointerId) {
        return;
      }

      drag = null;
      stage.classList.remove("is-dragging");
    }

    function resetView() {
      zoom = 1;
      zoomInput.value = "1";
      coverScale = Math.max(
        stage.clientWidth / Math.max(naturalWidth, 1),
        stage.clientHeight / Math.max(naturalHeight, 1));
      translateX = (stage.clientWidth - naturalWidth * coverScale) / 2;
      translateY = (stage.clientHeight - naturalHeight * coverScale) / 2;
      render();
    }

    function displayScale() {
      return coverScale * zoom;
    }

    function clampPan() {
      var scale = displayScale();
      var drawnWidth = naturalWidth * scale;
      var drawnHeight = naturalHeight * scale;
      var minX = Math.min(0, stage.clientWidth - drawnWidth);
      var minY = Math.min(0, stage.clientHeight - drawnHeight);
      translateX = Math.min(0, Math.max(minX, translateX));
      translateY = Math.min(0, Math.max(minY, translateY));
    }

    function render() {
      var scale = displayScale();
      stageImg.style.transform =
        "translate(" + translateX + "px, " + translateY + "px) scale(" + scale + ")";
    }

    function currentCrop() {
      var scale = displayScale();
      if (scale <= 0 || naturalWidth < 1 || naturalHeight < 1) {
        return null;
      }

      var x = Math.round(-translateX / scale);
      var y = Math.round(-translateY / scale);
      var width = Math.round(stage.clientWidth / scale);
      var height = Math.round(stage.clientHeight / scale);
      x = clampNumber(x, 0, Math.max(0, naturalWidth - 1));
      y = clampNumber(y, 0, Math.max(0, naturalHeight - 1));
      width = clampNumber(width, 1, naturalWidth - x);
      height = clampNumber(height, 1, naturalHeight - y);

      var expected = aspectWidth / aspectHeight;
      var actual = width / height;
      if (Math.abs(actual - expected) / expected > 0.08) {
        if (actual > expected) {
          width = Math.max(1, Math.round(height * expected));
        } else {
          height = Math.max(1, Math.round(width / expected));
        }

        width = clampNumber(width, 1, naturalWidth - x);
        height = clampNumber(height, 1, naturalHeight - y);
      }

      return { x: x, y: y, width: width, height: height };
    }

    function cancelSelection() {
      input.value = "";
      resetCropFields();
      cropApplied = false;
      revokeObjectUrl();
      dialog.close();
    }

    function resetCropFields() {
      cropX.value = "";
      cropY.value = "";
      cropWidth.value = "";
      cropHeight.value = "";
    }

    function showError(message) {
      if (!error) {
        return;
      }

      error.textContent = message;
      error.hidden = false;
    }

    function clearError() {
      if (!error) {
        return;
      }

      error.textContent = "";
      error.hidden = true;
    }

    function revokeObjectUrl() {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
        objectUrl = "";
      }
    }
  }

  function validateFile(file, maxBytes) {
    var name = (file.name || "").toLowerCase();
    var dot = name.lastIndexOf(".");
    var ext = dot >= 0 ? name.slice(dot) : "";
    var type = (file.type || "").toLowerCase();
    var typeOk = type === "image/jpeg" || type === "image/png" || type === "image/webp"
      || type === "image/jpg";
    if (!typeOk && !allowedExtensions[ext]) {
      return "Article image must be a JPEG, PNG, or WebP file.";
    }

    if (file.size > maxBytes) {
      return "Article image must be " + maxBytes + " bytes or smaller.";
    }

    return "";
  }

  function clampNumber(value, min, max) {
    return Math.min(max, Math.max(min, value));
  }
})();
