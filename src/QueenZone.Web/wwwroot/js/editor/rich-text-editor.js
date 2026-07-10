/**
 * Initializes Quill rich-text editors bound to hidden textareas.
 * Expected markup: .qz-rte[data-textarea][data-upload-url][data-container][data-af-token]
 *
 * File drops are handled on the whole editor chrome (not only the caret line) and the
 * document-level default is suppressed on compose pages so Chrome does not open the
 * image in a new tab when the drop misses the contenteditable line.
 */
(function () {
  "use strict";

  /** @type {WeakMap<Element, {quill: object, root: Element}>} */
  var editorsByRoot = new WeakMap();

  /**
   * Prefer the form's antiforgery field (same token used for POST) over the data attribute.
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
          count <= 1 ? "Uploading file…" : "Uploading " + count + " files…";
      }
    }

    var form = root.closest("form");
    if (form) {
      form.classList.toggle("qz-form--uploading", busy);
      var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
      for (var i = 0; i < buttons.length; i++) {
        if (busy || !form.classList.contains("is-submitting")) {
          buttons[i].disabled = busy;
        }
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

  function transferHasFiles(dataTransfer) {
    if (!dataTransfer || !dataTransfer.types) {
      return false;
    }
    var types = dataTransfer.types;
    // types can be a DOMStringList or array-like
    for (var i = 0; i < types.length; i++) {
      if (types[i] === "Files" || types[i] === "application/x-moz-file") {
        return true;
      }
    }
    return false;
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

  function acceptDroppedFiles(quill, root, files, clientX, clientY) {
    if (!files || !files.length) {
      return false;
    }
    var index = indexFromPoint(quill, clientX, clientY);
    for (var i = 0; i < files.length; i++) {
      handleFile(quill, root, files[i], index + i);
    }
    return true;
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

  /**
   * Find the editor under a point, else the nearest compose editor on the page.
   */
  function resolveEditorAtPoint(x, y) {
    var el = document.elementFromPoint(x, y);
    while (el && el !== document.documentElement) {
      if (el.classList && el.classList.contains("qz-rte") && editorsByRoot.has(el)) {
        return editorsByRoot.get(el);
      }
      // Walk up from field/form chrome into the editor root.
      if (el.classList && el.classList.contains("qz-rte-field")) {
        var nested = el.querySelector(".qz-rte");
        if (nested && editorsByRoot.has(nested)) {
          return editorsByRoot.get(nested);
        }
      }
      el = el.parentElement;
    }

    // Fallback: first initialized editor (compose pages usually have one).
    var all = document.querySelectorAll(".qz-rte[data-qz-rte-ready='1']");
    for (var i = 0; i < all.length; i++) {
      if (editorsByRoot.has(all[i])) {
        return editorsByRoot.get(all[i]);
      }
    }
    return null;
  }

  function setDragActive(root, active) {
    root.classList.toggle("qz-rte--dragover", active);
    var field = root.closest(".qz-rte-field");
    if (field) {
      field.classList.toggle("qz-rte-field--dragover", active);
    }
    var form = root.closest("form");
    if (form) {
      form.classList.toggle("qz-form--dragover", active);
    }
  }

  function bindDropZone(zone, quill, root) {
    // Nested dragenter/leave: only clear highlight when the pointer fully leaves the zone.
    var depth = 0;

    zone.addEventListener("dragenter", function (e) {
      if (!transferHasFiles(e.dataTransfer)) {
        return;
      }
      e.preventDefault();
      depth += 1;
      setDragActive(root, true);
    });

    zone.addEventListener("dragover", function (e) {
      if (!transferHasFiles(e.dataTransfer)) {
        return;
      }
      e.preventDefault();
      e.stopPropagation();
      if (e.dataTransfer) {
        e.dataTransfer.dropEffect = "copy";
      }
      setDragActive(root, true);
    });

    zone.addEventListener("dragleave", function (e) {
      if (!transferHasFiles(e.dataTransfer)) {
        return;
      }
      depth = Math.max(0, depth - 1);
      // relatedTarget outside the zone → clear
      var related = e.relatedTarget;
      if (depth === 0 || (related && !zone.contains(related))) {
        depth = 0;
        setDragActive(root, false);
      }
    });

    zone.addEventListener("drop", function (e) {
      var files = e.dataTransfer && e.dataTransfer.files;
      if (!files || !files.length) {
        return;
      }
      e.preventDefault();
      e.stopPropagation();
      depth = 0;
      setDragActive(root, false);
      acceptDroppedFiles(quill, root, files, e.clientX, e.clientY);
    });
  }

  function bindHandlers(quill, root) {
    var toolbar = quill.getModule("toolbar");
    if (toolbar) {
      toolbar.addHandler("image", function () {
        pickFiles("image/*", false, function (file) {
          handleFile(quill, root, file);
        });
      });

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
          if (isImageFile(file) || (file.type && file.type.length > 0)) {
            e.preventDefault();
            handleFile(quill, root, file);
            return;
          }
        }
      }
    });

    // Whole editor chrome (toolbar + content + progress), not just the caret line.
    bindDropZone(root, quill, root);

    var field = root.closest(".qz-rte-field");
    if (field) {
      bindDropZone(field, quill, root);
    }

    // Compose form: catch drops on help text, buttons, empty padding.
    var form = root.closest("form");
    if (form && form.getAttribute("data-qz-rte-drop-bound") !== "1") {
      form.setAttribute("data-qz-rte-drop-bound", "1");
      bindDropZone(form, quill, root);
    }
  }

  /**
   * Page-level guard: stop the browser navigating to a dropped file (Chrome opens a tab).
   * Drops that land outside the editor still cancel navigation; if we can resolve an
   * editor, the file is accepted into it.
   */
  function installDocumentDropGuard() {
    if (document.documentElement.getAttribute("data-qz-rte-doc-drop") === "1") {
      return;
    }
    document.documentElement.setAttribute("data-qz-rte-doc-drop", "1");

    document.addEventListener(
      "dragover",
      function (e) {
        if (!document.querySelector(".qz-rte[data-qz-rte-ready='1']")) {
          return;
        }
        if (!transferHasFiles(e.dataTransfer)) {
          return;
        }
        // Required so drop fires and the browser does not use its navigate default.
        e.preventDefault();
        if (e.dataTransfer) {
          e.dataTransfer.dropEffect = "copy";
        }
      },
      false
    );

    document.addEventListener(
      "drop",
      function (e) {
        if (!document.querySelector(".qz-rte[data-qz-rte-ready='1']")) {
          return;
        }
        if (!transferHasFiles(e.dataTransfer)) {
          return;
        }
        e.preventDefault();

        var files = e.dataTransfer && e.dataTransfer.files;
        if (!files || !files.length) {
          return;
        }

        var target = resolveEditorAtPoint(e.clientX, e.clientY);
        if (!target) {
          return;
        }
        setDragActive(target.root, false);
        acceptDroppedFiles(target.quill, target.root, files, e.clientX, e.clientY);
      },
      false
    );
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

    // Drop hint overlay (shown while dragging files over the zone).
    if (!root.querySelector(".qz-rte-drop-hint")) {
      var hint = document.createElement("div");
      hint.className = "qz-rte-drop-hint";
      hint.setAttribute("aria-hidden", "true");
      hint.innerHTML =
        '<span class="qz-rte-drop-hint__label">Drop image or file to attach</span>';
      root.appendChild(hint);
    }

    editorsByRoot.set(root, { quill: quill, root: root });
    bindHandlers(quill, root);
    installDocumentDropGuard();
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
