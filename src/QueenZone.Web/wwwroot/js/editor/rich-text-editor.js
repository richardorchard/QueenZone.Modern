/**
 * Initializes Quill rich-text editors bound to hidden textareas.
 * Expected markup: .qz-rte[data-textarea][data-upload-url][data-container][data-af-token]
 */
(function () {
  "use strict";

  /**
   * Prefer the form's antiforgery field (same token used for POST) over the data attribute.
   * Generating a second token in the partial can desync from the form token on some hosts.
   */
  function getToken(root) {
    var form = root.closest("form");
    if (form) {
      var input = form.querySelector('input[name="__RequestVerificationToken"]');
      if (input && input.value) {
        return input.value;
      }
    }
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
          count <= 1 ? "Uploading…" : "Uploading " + count + " files…";
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

  function getMaxBytes(root) {
    var n = parseInt(root.getAttribute("data-max-bytes") || "0", 10);
    return n > 0 ? n : 10 * 1024 * 1024;
  }

  function isImageFile(file) {
    if (!file) {
      return false;
    }
    if (file.type && file.type.indexOf("image/") === 0) {
      return true;
    }
    var name = (file.name || "").toLowerCase();
    return (
      name.endsWith(".png") ||
      name.endsWith(".jpg") ||
      name.endsWith(".jpeg") ||
      name.endsWith(".gif") ||
      name.endsWith(".webp")
    );
  }

  function uploadFile(root, file) {
    var uploadUrl = root.getAttribute("data-upload-url");
    var container = root.getAttribute("data-container") || "ugc-forum";
    var token = getToken(root);
    var maxBytes = getMaxBytes(root);

    if (!token) {
      return Promise.reject(
        new Error("Missing antiforgery token. Refresh the page and try again.")
      );
    }

    if (file.size > maxBytes) {
      return Promise.reject(
        new Error("File must be " + Math.ceil(maxBytes / (1024 * 1024)) + " MB or smaller.")
      );
    }

    var form = new FormData();
    form.append("file", file, file.name || (isImageFile(file) ? "paste.png" : "attachment.bin"));
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
            var msg = (body && body.error) || "Upload failed.";
            throw new Error(msg);
          }
          if (!body || !body.url) {
            throw new Error("Upload returned no URL.");
          }
          return body;
        });
      })
      .finally(function () {
        endUpload(root);
      });
  }

  /**
   * Resolve a Quill document index for a mouse/touch point (for drag-and-drop).
   */
  function indexFromPoint(quill, x, y) {
    try {
      var native = null;
      if (document.caretRangeFromPoint) {
        native = document.caretRangeFromPoint(x, y);
      } else if (document.caretPositionFromPoint) {
        var pos = document.caretPositionFromPoint(x, y);
        if (pos) {
          native = document.createRange();
          native.setStart(pos.offsetNode, pos.offset);
          native.collapse(true);
        }
      }
      if (native) {
        var blot = Quill.find(native.startContainer, true);
        if (blot) {
          return blot.offset(quill.scroll) + (native.startOffset || 0);
        }
      }
    } catch (e) {
      // fall through
    }
    var sel = quill.getSelection(true);
    if (sel) {
      return sel.index;
    }
    // Insert before the trailing newline Quill keeps at end of document.
    return Math.max(0, quill.getLength() - 1);
  }

  function insertAt(quill, index, body) {
    var kind = (body && body.kind) || "image";
    var url = body.url;
    var safeIndex = Math.max(0, Math.min(index, Math.max(0, quill.getLength() - 1)));

    if (kind === "file") {
      var name = (body.fileName || "attachment").toString();
      quill.insertText(safeIndex, name, "link", url, "user");
      quill.insertText(safeIndex + name.length, " ", "user");
      quill.setSelection(safeIndex + name.length + 1, 0, "silent");
      return;
    }

    // Image: embed then ensure a paragraph break so further typing works.
    quill.insertEmbed(safeIndex, "image", url, "user");
    quill.insertText(safeIndex + 1, "\n", "user");
    quill.setSelection(safeIndex + 2, 0, "silent");
  }

  function handleFile(quill, root, file, index) {
    if (!file) {
      return;
    }
    showError(root, "");
    var insertIndex =
      typeof index === "number"
        ? index
        : (function () {
            var sel = quill.getSelection(true);
            return sel ? sel.index : Math.max(0, quill.getLength() - 1);
          })();

    uploadFile(root, file)
      .then(function (body) {
        insertAt(quill, insertIndex, body);
      })
      .catch(function (err) {
        showError(root, err.message || "Upload failed.");
      });
  }

  function pickFiles(accept, multiple, onFiles) {
    var input = document.createElement("input");
    input.setAttribute("type", "file");
    input.setAttribute("accept", accept);
    if (multiple) {
      input.setAttribute("multiple", "multiple");
    }
    input.click();
    input.onchange = function () {
      var files = input.files;
      if (!files || !files.length) {
        return;
      }
      for (var i = 0; i < files.length; i++) {
        onFiles(files[i]);
      }
    };
  }

  function bindHandlers(quill, root) {
    var toolbar = quill.getModule("toolbar");
    if (toolbar) {
      toolbar.addHandler("image", function () {
        pickFiles("image/*", false, function (file) {
          handleFile(quill, root, file);
        });
      });

      // Attach non-image (and image) files via a paperclip control if present.
      var attachBtn = root.parentElement && root.parentElement.querySelector("[data-qz-rte-attach]");
      if (attachBtn) {
        attachBtn.addEventListener("click", function (e) {
          e.preventDefault();
          pickFiles(
            "image/*,.pdf,.txt,.zip,.doc,.docx,.xls,.xlsx,application/pdf,text/plain,application/zip",
            true,
            function (file) {
              handleFile(quill, root, file);
            }
          );
        });
      }
    }

    quill.root.addEventListener("paste", function (e) {
      var items = e.clipboardData && e.clipboardData.items;
      if (!items) {
        return;
      }
      for (var i = 0; i < items.length; i++) {
        if (items[i].kind === "file") {
          var file = items[i].getAsFile();
          if (!file) {
            continue;
          }
          // Prefer images from clipboard; skip non-files.
          if (isImageFile(file) || (file.type && file.type.length > 0)) {
            e.preventDefault();
            handleFile(quill, root, file);
            return;
          }
        }
      }
    });

    // Required so the browser allows drop into the editor (empty paragraphs included).
    quill.root.addEventListener("dragover", function (e) {
      if (e.dataTransfer && e.dataTransfer.types) {
        var types = e.dataTransfer.types;
        var hasFiles = false;
        for (var i = 0; i < types.length; i++) {
          if (types[i] === "Files") {
            hasFiles = true;
            break;
          }
        }
        if (hasFiles) {
          e.preventDefault();
          e.dataTransfer.dropEffect = "copy";
        }
      }
    });

    quill.root.addEventListener("drop", function (e) {
      var files = e.dataTransfer && e.dataTransfer.files;
      if (!files || !files.length) {
        return;
      }
      e.preventDefault();
      e.stopPropagation();

      var index = indexFromPoint(quill, e.clientX, e.clientY);
      // Process first file only to keep placement predictable; extra files append after.
      for (var i = 0; i < files.length; i++) {
        handleFile(quill, root, files[i], index + i);
      }
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

    // Sync data-af-token from form if the form token is the canonical one.
    var formToken = getToken(root);
    if (formToken) {
      root.setAttribute("data-af-token", formToken);
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
          showError(root, "Please wait for uploads to finish.");
        }
      });
    }

    // Attach button lives next to the editor field.
    var field = root.closest(".qz-rte-field");
    if (field && !field.querySelector("[data-qz-rte-attach]")) {
      var actions = document.createElement("div");
      actions.className = "qz-rte-actions";
      var attach = document.createElement("button");
      attach.type = "button";
      attach.className = "qz-button qz-button--outline qz-rte-attach";
      attach.setAttribute("data-qz-rte-attach", "1");
      attach.textContent = "Attach file";
      actions.appendChild(attach);
      field.appendChild(actions);
    }

    bindHandlers(quill, root);
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
