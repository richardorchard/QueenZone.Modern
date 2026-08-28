(function () {
  "use strict";

  if (typeof Cropper !== "function") {
    return;
  }

  var allowedExtensions = { ".jpg": true, ".jpeg": true, ".png": true, ".webp": true };

  document.querySelectorAll("[data-article-image-crop]").forEach(bindCropper);

  function bindCropper(root) {
    var form = root.closest("form");
    var input = root.querySelector("[data-article-image-input]");
    var preview = root.querySelector("[data-article-image-preview]");
    var error = root.querySelector("[data-article-image-error]");
    var dialog = root.querySelector("[data-article-image-dialog]");
    var stageImg = root.querySelector("[data-article-image-stage-img]");
    var zoomInput = root.querySelector("[data-article-image-zoom]");
    var applyButton = root.querySelector("[data-article-image-apply]");
    var cancelButton = root.querySelector("[data-article-image-cancel]");
    var cropX = root.querySelector("[data-crop-x]");
    var cropY = root.querySelector("[data-crop-y]");
    var cropWidth = root.querySelector("[data-crop-width]");
    var cropHeight = root.querySelector("[data-crop-height]");
    if (!form || !input || !dialog || !stageImg || !zoomInput) {
      return;
    }

    var aspectWidth = Number(root.getAttribute("data-aspect-width")) || 3;
    var aspectHeight = Number(root.getAttribute("data-aspect-height")) || 2;
    var minCropWidth = Number(root.getAttribute("data-min-crop-width")) || 400;
    var minCropHeight = Number(root.getAttribute("data-min-crop-height")) || 267;
    var maxBytes = Number(root.getAttribute("data-max-bytes")) || 10 * 1024 * 1024;
    var objectUrl = "";
    var cropper = null;
    var cropApplied = false;
    var baseRatio = 1;
    var syncingZoom = false;

    input.addEventListener("change", function () {
      clearError();
      resetCropFields();
      cropApplied = false;
      destroyCropper();
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
      objectUrl = asBlobObjectUrl(URL.createObjectURL(file));
      if (!objectUrl) {
        return;
      }

      stageImg.onload = function () {
        if (typeof dialog.showModal === "function") {
          dialog.showModal();
        }

        startCropper();
      };
      assignBlobImageSrc(stageImg, objectUrl);
    });

    zoomInput.addEventListener("input", function () {
      if (!cropper || syncingZoom) {
        return;
      }

      cropper.zoomTo(Number(zoomInput.value) || baseRatio);
    });

    applyButton && applyButton.addEventListener("click", function () {
      var crop = currentCrop();
      if (!crop) {
        showError("Could not read the crop. Try another image.");
        return;
      }

      if (crop.width < minCropWidth || crop.height < minCropHeight) {
        showError(
          "The selected crop is too small. Use at least " +
            minCropWidth +
            "\u00d7" +
            minCropHeight +
            " pixels.");
        return;
      }

      cropX.value = String(crop.x);
      cropY.value = String(crop.y);
      cropWidth.value = String(crop.width);
      cropHeight.value = String(crop.height);
      cropApplied = true;
      if (preview) {
        assignBlobImageSrc(preview, objectUrl);
        preview.alt = "Article image";
        preview.style.objectPosition =
          ((crop.x + crop.width / 2) / Math.max(stageImg.naturalWidth, 1)) * 100 + "% " +
          ((crop.y + crop.height / 2) / Math.max(stageImg.naturalHeight, 1)) * 100 + "%";
      }

      var caption = root.querySelector(".admin-article-image figcaption");
      if (caption) {
        caption.remove();
      }

      destroyCropper();
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

      if (!cropper) {
        startCropper();
      }
    });

    function startCropper() {
      destroyCropper();
      cropper = new Cropper(stageImg, {
        aspectRatio: aspectWidth / aspectHeight,
        viewMode: 2,
        autoCropArea: 1,
        dragMode: "move",
        cropBoxMovable: false,
        cropBoxResizable: false,
        guides: false,
        center: false,
        highlight: false,
        movable: true,
        rotatable: false,
        scalable: false,
        zoomable: true,
        zoomOnWheel: true,
        ready: function () {
          var imageData = cropper.getImageData();
          baseRatio = imageData.width / Math.max(imageData.naturalWidth, 1);
          zoomInput.min = String(baseRatio);
          zoomInput.max = String(maxAllowedRatio(baseRatio));
          zoomInput.step = "0.01";
          zoomInput.value = String(baseRatio);
        },
        zoom: function (event) {
          if (cropWouldBeTooSmall(event.detail.ratio, event.detail.oldRatio)) {
            event.preventDefault();
            return;
          }

          syncingZoom = true;
          zoomInput.value = String(event.detail.ratio);
          syncingZoom = false;
        }
      });
    }

    function maxAllowedRatio(startRatio) {
      var data = cropper.getData();
      if (data.width < 1 || data.height < 1) {
        return startRatio;
      }

      var byWidth = startRatio * (data.width / minCropWidth);
      var byHeight = startRatio * (data.height / minCropHeight);
      return Math.max(startRatio, Math.min(byWidth, byHeight));
    }

    function cropWouldBeTooSmall(newRatio, oldRatio) {
      if (!cropper || !oldRatio || newRatio <= oldRatio) {
        return false;
      }

      var data = cropper.getData();
      var scale = oldRatio / newRatio;
      return data.width * scale < minCropWidth || data.height * scale < minCropHeight;
    }

    function currentCrop() {
      if (!cropper) {
        return null;
      }

      var data = cropper.getData(true);
      if (!data || data.width < 1 || data.height < 1) {
        return null;
      }

      var naturalWidth = Math.max(stageImg.naturalWidth, 1);
      var naturalHeight = Math.max(stageImg.naturalHeight, 1);
      var x = clampNumber(data.x, 0, Math.max(0, naturalWidth - 1));
      var y = clampNumber(data.y, 0, Math.max(0, naturalHeight - 1));
      var width = clampNumber(data.width, 1, naturalWidth - x);
      var height = clampNumber(data.height, 1, naturalHeight - y);
      return { x: x, y: y, width: width, height: height };
    }

    function destroyCropper() {
      if (cropper) {
        cropper.destroy();
        cropper = null;
      }
    }

    function cancelSelection() {
      destroyCropper();
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

  // createObjectURL is a modeled taint step from input.files (js/xss-through-dom).
  // Only browser blob: URLs may reach img.src; encodeURI is the XSS sanitizer CodeQL recognizes.
  function asBlobObjectUrl(url) {
    return typeof url === "string" && url.indexOf("blob:") === 0 ? url : "";
  }

  function assignBlobImageSrc(image, url) {
    var safeUrl = asBlobObjectUrl(url);
    if (!image || !safeUrl) {
      return false;
    }

    image.src = encodeURI(safeUrl);
    return true;
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
