/**
 * Initializes Quill rich-text editors bound to hidden textareas.
 * Expected markup: .qz-rte[data-textarea][data-upload-url][data-container][data-af-token]
 */
(function () {
  "use strict";

  function getToken(root) {
    return root.getAttribute("data-af-token") || "";
  }

  function showError(root, message) {
    var el = root.querySelector(".qz-rte-error");
    if (!el) {
      return;
    }
    el.textContent = message || "";
    el.hidden = !message;
  }

  function getInflight(root) {
    var n = parseInt(root.getAttribute("data-uploads-inflight") || "0", 10);
    return isNaN(n) ? 0 : n;
  }

  function setInflight(root, count) {
    root.setAttribute("data-uploads-inflight", String(Math.max(0, count)));
    var busy = count > 0;
    root.classList.toggle("qz-rte--uploading", busy);

    var progress = root.querySelector(".qz-rte-progress");
    if (progress) {
      progress.hidden = !busy;
      progress.setAttribute("aria-busy", busy ? "true" : "false");
      var label = progress.querySelector(".qz-rte-progress__label");
      if (label) {
        label.textContent =
          count === 1 ? "Uploading image…" : "Uploading " + count + " images…";
      }
    }

    var form = root.closest("form");
    if (form) {
      var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
      for (var i = 0; i < buttons.length; i++) {
        buttons[i].disabled = busy;
      }
    }
  }

  function beginUpload(root) {
    setInflight(root, getInflight(root) + 1);
  }

  function endUpload(root) {
    setInflight(root, getInflight(root) - 1);
  }

  function uploadImage(root, file) {
    var uploadUrl = root.getAttribute("data-upload-url");
    var container = root.getAttribute("data-container") || "ugc-forum";
    var token = getToken(root);

    var form = new FormData();
    form.append("file", file, file.name || "paste.png");
    form.append("container", container);
    form.append("__RequestVerificationToken", token);

    beginUpload(root);

    return fetch(uploadUrl, {
      method: "POST",
      body: form,
      credentials: "same-origin",
      headers: {
        RequestVerificationToken: token,
      },
    })
      .then(function (response) {
        return response.json().then(function (body) {
          if (!response.ok) {
            var msg = (body && body.error) || "Image upload failed.";
            throw new Error(msg);
          }
          if (!body || !body.url) {
            throw new Error("Image upload returned no URL.");
          }
          // Prefer full proxy URL in the document; display layer rewrites to thumb + link.
          return body.url;
        });
      })
      .finally(function () {
        endUpload(root);
      });
  }

  function insertImage(quill, url) {
    var range = quill.getSelection(true) || { index: quill.getLength(), length: 0 };
    quill.insertEmbed(range.index, "image", url, "user");
    quill.setSelection(range.index + 1, 0, "silent");
  }

  function handleImageFile(quill, root, file) {
    if (!file) {
      return;
    }
    showError(root, "");
    uploadImage(root, file)
      .then(function (url) {
        insertImage(quill, url);
      })
      .catch(function (err) {
        showError(root, err.message || "Image upload failed.");
      });
  }

  function bindImageHandler(quill, root) {
    var toolbar = quill.getModule("toolbar");
    if (toolbar) {
      toolbar.addHandler("image", function () {
        var input = document.createElement("input");
        input.setAttribute("type", "file");
        input.setAttribute("accept", "image/*");
        input.click();
        input.onchange = function () {
          var file = input.files && input.files[0];
          handleImageFile(quill, root, file);
        };
      });
    }

    quill.root.addEventListener("paste", function (e) {
      var items = e.clipboardData && e.clipboardData.items;
      if (!items) {
        return;
      }
      for (var i = 0; i < items.length; i++) {
        if (items[i].type && items[i].type.indexOf("image") === 0) {
          e.preventDefault();
          var file = items[i].getAsFile();
          handleImageFile(quill, root, file);
          return;
        }
      }
    });

    quill.root.addEventListener("drop", function (e) {
      var files = e.dataTransfer && e.dataTransfer.files;
      if (!files || !files.length) {
        return;
      }
      var file = files[0];
      if (!file.type || file.type.indexOf("image") !== 0) {
        return;
      }
      e.preventDefault();
      handleImageFile(quill, root, file);
    });
  }

  function initOne(root) {
    if (root.getAttribute("data-qz-rte-ready") === "1") {
      return;
    }
    if (typeof Quill === "undefined") {
      return;
    }

    var textareaId = root.getAttribute("data-textarea");
    var textarea = textareaId ? document.getElementById(textareaId) : null;
    var mount = root.querySelector(".qz-rte-mount");
    if (!textarea || !mount) {
      return;
    }

    var quill = new Quill(mount, {
      theme: "snow",
      modules: {
        toolbar: [
          [{ header: [2, 3, false] }],
          ["bold", "italic", "underline"],
          [{ list: "ordered" }, { list: "bullet" }],
          ["blockquote", "link", "image"],
          ["clean"],
        ],
      },
      formats: [
        "header",
        "bold",
        "italic",
        "underline",
        "list",
        "blockquote",
        "link",
        "image",
      ],
    });

    if (textarea.value) {
      quill.root.innerHTML = textarea.value;
    }

    function sync() {
      var html = quill.root.innerHTML;
      if (html === "<p><br></p>") {
        html = "";
      }
      textarea.value = html;
    }

    quill.on("text-change", sync);
    var form = textarea.closest("form");
    if (form) {
      form.addEventListener("submit", function (e) {
        sync();
        if (getInflight(root) > 0) {
          e.preventDefault();
          showError(root, "Please wait for image uploads to finish.");
        }
      });
    }

    bindImageHandler(quill, root);
    root.setAttribute("data-qz-rte-ready", "1");
    setInflight(root, 0);
  }

  function initAll() {
    document.querySelectorAll(".qz-rte").forEach(initOne);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initAll);
  } else {
    initAll();
  }

  window.QueenZoneRichTextEditor = { initAll: initAll };
})();
