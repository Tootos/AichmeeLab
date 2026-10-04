window.writerEngine = {
    // Tracks saved selection range across editor focus shifts
    savedRange: null,
    _pasteHandlerInitialized: false,
    _enterHandlerInitialized: false,
    _trackerInitialized: false,

    // 1. Live selection tracker: continuously saves cursor position inside any contenteditable element
    initSelectionTracker: () => {
        if (window.writerEngine._trackerInitialized) return;
        window.writerEngine._trackerInitialized = true;

        document.addEventListener('selectionchange', () => {
            const sel = window.getSelection();
            if (sel && sel.rangeCount > 0) {
                let node = sel.anchorNode;
                if (node) {
                    if (node.nodeType === Node.TEXT_NODE) node = node.parentElement;
                    
                    // Always save range if selection is inside an active contenteditable block
                    if (node && node.closest && node.closest('[contenteditable="true"]')) {
                        window.writerEngine.savedRange = sel.getRangeAt(0);
                    }
                }
            }
        });
    },

    // 2. Helper: Explicitly save selection right before focus leaves the canvas
    saveSelection: () => {
        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0) {
            let node = sel.anchorNode;
            if (node) {
                if (node.nodeType === Node.TEXT_NODE) node = node.parentElement;
                
                if (node && node.closest && node.closest('[contenteditable="true"]')) {
                    window.writerEngine.savedRange = sel.getRangeAt(0);
                }
            }
        }
    },

    // 3. Helper: Restore selection range back to target element
    restoreSelection: () => {
        const range = window.writerEngine.savedRange;
        if (range) {
            const sel = window.getSelection();
            sel.removeAllRanges();
            sel.addRange(range);
        }
    },

    // Load data from C# to Html
    loadContent: (domId, htmlContent) => {
        const el = document.getElementById(domId);
        if (!el) return;

        if (!htmlContent || htmlContent.trim() === "") {
            el.innerHTML = "<p><br></p>";
        } else {
            el.innerHTML = htmlContent;
        }

        document.execCommand('defaultParagraphSeparator', false, 'p');
    },

    // Apply Format Style
    applyBlockStyle: (id, type) => {
        const el = document.getElementById(id);
        if (el) {
            el.focus();
            document.execCommand('formatBlock', false, type);
            el.dispatchEvent(new Event('blur', { bubbles: true }));
        }
    },

    // Apply List Format
    applyList: (id, type) => {
        const el = document.getElementById(id);
        if (el) {
            el.focus();
            const command = type === 'ol' ? 'insertOrderedList' : 'insertUnorderedList';
            document.execCommand(command, false, null);
            el.dispatchEvent(new Event('blur', { bubbles: true }));
        }
    },

    // Move side menu position relative to cursor position
    updateMenuPosition: (id, menuWrapperId) => {
        const el = document.getElementById(id);
        const menu = document.getElementById(menuWrapperId);

        if (!el || !menu) return;

        const selection = window.getSelection();
        if (!selection.rangeCount) return;

        const range = selection.getRangeAt(0);
        const rect = range.getBoundingClientRect();
        const parentRect = el.getBoundingClientRect();

        let offsetTop = 0;

        if (rect.height > 0) {
            offsetTop = rect.top - parentRect.top;
        } else {
            // FALLBACK: On empty line, create temp element to calculate cursor Y position
            const dummy = document.createElement("span");
            dummy.textContent = "\u200b"; // zero-width space
            range.insertNode(dummy);
            offsetTop = dummy.getBoundingClientRect().top - parentRect.top;
            dummy.remove();
        }

        menu.style.display = 'flex';
        menu.style.top = (offsetTop + 2) + "px";
        menu.classList.add('is-active');
    },

    // Clean Pasted Handler
    cleanPastedHandler: () => {
        if (window.writerEngine._pasteHandlerInitialized) return;
        window.writerEngine._pasteHandlerInitialized = true;

        document.addEventListener('paste', (e) => {
            const target = e.target;
            if (!target || !target.isContentEditable) return;

            e.preventDefault();

            const clipboard = e.clipboardData || window.clipboardData;
            const html = clipboard.getData('text/html');
            const text = clipboard.getData('text/plain');

            const cleanContent = (html && html.trim()) 
                ? window.writerEngine.cleanPastedHtml(html) 
                : text;

            document.execCommand('insertHTML', false, cleanContent);
            target.dispatchEvent(new Event('input', { bubbles: true }));
        });
    },

    cleanPastedHtml: (html) => {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');

        doc.body.removeAttribute('style');
        doc.body.removeAttribute('color');
        doc.body.removeAttribute('class');
        doc.body.removeAttribute('bgcolor');

        const allElements = doc.body.querySelectorAll('*');
        allElements.forEach(el => {
            el.removeAttribute('style');
            el.removeAttribute('color');
            el.removeAttribute('face');
            el.removeAttribute('class');
            el.removeAttribute('bgcolor');

            if (el.tagName.toLowerCase() === 'a') {
                el.setAttribute('target', '_blank');
                el.setAttribute('rel', 'noopener noreferrer');
            }
        });

        return doc.body.innerHTML;
    },

    // Reset formatting when pressing Enter
    initEnterKeyReset: () => {
        if (window.writerEngine._enterHandlerInitialized) return;
        window.writerEngine._enterHandlerInitialized = true;

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                const target = e.target;
                if (!target || !target.isContentEditable) return;

                setTimeout(() => {
                    document.execCommand('removeFormat', false, null);
                    
                    // Keep scroll position local to the canvas
                    const canvas = target.closest('.writer-canvas');
                    if (canvas) {
                        const selection = window.getSelection();
                        if (selection.rangeCount > 0) {
                            const range = selection.getRangeAt(0);
                            const rect = range.getBoundingClientRect();
                            const canvasRect = canvas.getBoundingClientRect();

                            if (rect.bottom > canvasRect.bottom - 40) {
                                canvas.scrollTop += (rect.bottom - canvasRect.bottom) + 60;
                            }
                        }
                    }

                    target.dispatchEvent(new Event('input', { bubbles: true }));
                }, 0);
            }
        });
    },

    // Change Inline Style
    applyInlineStyle: (id, command) => {
        const el = document.getElementById(id);
        if (el) {
            el.focus();
            document.execCommand(command, false, null);
            el.dispatchEvent(new Event('input', { bubbles: true }));
        }
    },

    // Apply Hyperlink
    applyLink: (id) => {
        const el = document.getElementById(id);
        if (!el) return;

        el.focus();
        const url = prompt("Enter the URL:");
        if (url) {
            document.execCommand('createLink', false, url);

            const selection = window.getSelection();
            if (selection.rangeCount > 0) {
                let node = selection.anchorNode;
                if (node.nodeType === Node.TEXT_NODE) {
                    node = node.parentElement;
                }
                const anchor = node.closest('a');
                if (anchor) {
                    anchor.setAttribute('target', '_blank');
                    anchor.setAttribute('rel', 'noopener noreferrer');
                }
            }

            el.dispatchEvent(new Event('input', { bubbles: true }));
        }
    },

    // Applies alignment (left, center, right, justify)
    applyAlignment: (id, alignType) => {
        const el = document.getElementById(id);
        if (el) {
            el.focus();

            switch (alignType) {
                case 'left':
                    document.execCommand('justifyLeft', false, null);
                    break;
                case 'center':
                    document.execCommand('justifyCenter', false, null);
                    break;
                case 'right':
                    document.execCommand('justifyRight', false, null);
                    break;
                case 'justify':
                    document.execCommand('justifyFull', false, null);
                    break;
            }
        }
    },

    // Fixed Picmo Picker with selection restoration sequence
    openPicmoPicker: (domId, btnId) => {
        const triggerBtn = document.getElementById(btnId);
        if (!triggerBtn) return;

        window.writerEngine.saveSelection();

        const existingPicker = document.getElementById('picmo-popover');
        if (existingPicker) {
            existingPicker.remove();
            if (existingPicker.dataset.btnId === btnId) return;
        }

        const rect = triggerBtn.getBoundingClientRect();

        const pickerContainer = document.createElement('div');
        pickerContainer.id = 'picmo-popover';
        pickerContainer.dataset.btnId = btnId;
        pickerContainer.style.position = 'fixed';
        pickerContainer.style.zIndex = '99999';
        pickerContainer.style.top = `${rect.bottom + 6}px`;
        pickerContainer.style.left = `${rect.left}px`;

        const picker = picmo.createPicker({
            rootElement: pickerContainer,
            showPreview: false,
            autoFocus: 'search'
        });

        picker.addEventListener('emoji:select', selection => {
            const el = document.getElementById(domId);
            if (el) {
                window.writerEngine.restoreSelection();
                el.focus();
                window.writerEngine.restoreSelection();

                document.execCommand('insertText', false, selection.emoji);
                window.writerEngine.saveSelection();

                el.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });

        const closeOnClickOutside = (e) => {
            if (!pickerContainer.contains(e.target) && e.target !== triggerBtn) {
                pickerContainer.remove();
                document.removeEventListener('click', closeOnClickOutside);
            }
        };

        setTimeout(() => {
            document.addEventListener('click', closeOnClickOutside);
        }, 0);

        document.body.appendChild(pickerContainer);
    },

    // Split document content
splitContent: (domId) => {
const el = document.getElementById(domId);
    if (!el) return "";

    // Restore saved selection before splitting
    window.writerEngine.restoreSelection();

    const selection = window.getSelection();

    if (!selection || !selection.rangeCount || !el.contains(selection.anchorNode)) {
        return "";
    }

    const range = selection.getRangeAt(0);

    const trailingRange = document.createRange();
    trailingRange.setStart(range.endContainer, range.endOffset);
    trailingRange.setEndAfter(el.lastChild || el);

    const fragment = trailingRange.extractContents();

    const tempDiv = document.createElement("div");
    tempDiv.appendChild(fragment);

    const trailingHtml = tempDiv.innerHTML;

    if (el.innerHTML.trim() === "" || el.innerHTML === "<br>") {
        el.innerHTML = "<p><br></p>";
    }

    // Trigger input event so Blazor SyncContent captures updated leading content
    el.dispatchEvent(new Event('input', { bubbles: true }));

    return trailingHtml;
},

    // Get content of <div>
    getHtml: (id) => {
        const el = document.getElementById(id);
        if (!el) return "";

        let html = el.innerHTML;

        html = html.replace(/<p><br><\/p>/g, "");
        html = html.replace(/(<p>&nbsp;<\/p>)+/g, "");

        return html.trim();
    }
};