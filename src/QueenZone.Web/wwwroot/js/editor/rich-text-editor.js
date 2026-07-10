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

  function uploadImage(root, file) {
    var uploadUrl = root.getAttribute("data-upload-url");
    var container = root.getAttribute("data-container") || "ugc-forum";
    var token = getToken(root);

    var form = new FormData();
    form.append("file", file, file.name || "paste.png");
    form.append("container", container);
    form.append("__RequestVerificationToken", token);

    return fetch(uploadUrl, {
      method: "POST",
      body: form,
      credentials: "same-origin",
      headers: {
        RequestVerificationToken: token,
      },
    }).then(function (response) {
      return response.json().then(function (body) {
        if (!response.ok) {
          var msg = (body && body.error) || "Image upload failed.";
          throw new Error(msg);
        }
        if (!body || !body.url) {
          throw new Error("Image upload returned no URL.");
        }
        return body.url;
      });
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
          if (!file) {
            return;
          }
          showError(root, "");
          uploadImage(root, file)
            .then(function (url) {
              var range = quill.getSelection(true) || { index: quill.getLength(), length: 0 };
              quill.insertEmbed(range.index, "image", url, "user");
              quill.setSelection(range.index + 1, 0, "silent");
            })
            .catch(function (err) {
              showError(root, err.message || "Image upload failed.");
            });
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
          if (!file) {
            return;
          }
          showError(root, "");
          uploadImage(root, file)
            .then(function (url) {
              var range = quill.getSelection(true) || { index: quill.getLength(), length: 0 };
              quill.insertEmbed(range.index, "image", url, "user");
              quill.setSelection(range.index + 1, 0, "silent");
            })
            .catch(function (err) {
              showError(root, err.message || "Image upload failed.");
            });
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
      showError(root, "");
      uploadImage(root, file)
        .then(function (url) {
          var range = quill.getSelection(true) || { index: quill.getLength(), length: 0 };
          quill.insertEmbed(range.index, "image", url, "user");
          quill.setSelection(range.index + 1, 0, "silent");
        })
        .catch(function (err) {
          showError(root, err.message || "Image upload failed.");
        });
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
      form.addEventListener("submit", sync);
    }

    bindImageHandler(quill, root);
    root.setAttribute("data-qz-rte-ready", "1");
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
