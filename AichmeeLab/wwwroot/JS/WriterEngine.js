window.writerEngine = {
    // Tracks saved selection range across editor focus shifts
    savedRange: null,

    // Helper: Save current selection range if focused inside a contenteditable block
    saveSelection: () => {
        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0) {
            const activeEl = document.activeElement;
            if (activeEl && activeEl.isContentEditable) {
                window.writerEngine.savedRange = sel.getRangeAt(0);
            }
        }
    },

    // Helper: Restore selection range back to active target element
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

        // If content is null/empty, seed it with default P tag
        if (!htmlContent || htmlContent.trim() === "") {
            el.innerHTML = "<p><br></p>";
        } else {
            // Inject the HTML from DB
            el.innerHTML = htmlContent;
        }

        // Set paragraph separator rule for future typing
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

    // Safe inline toolbar listener with null checking on route changes
    initInlineToolbar: () => {
        document.addEventListener('selectionchange', () => {
            // Continuously track selection inside editable blocks
            window.writerEngine.saveSelection();

            const toolbar = document.getElementById('inline-toolbar');
            
            // Safe guard against unmounted DOM toolbar
            if (!toolbar) return;

            const selection = window.getSelection();
            if (selection.rangeCount > 0 && !selection.isCollapsed) {
                const range = selection.getRangeAt(0);
                const rect = range.getBoundingClientRect();

                toolbar.style.display = 'flex';
                toolbar.style.top = `${rect.top - 45 + window.scrollY}px`;
                toolbar.style.left = `${rect.left + (rect.width / 2) - (toolbar.offsetWidth / 2) - 100}px`;
            } else {
                toolbar.style.display = 'none';
            }
        });
    },

    // Clean incoming HTML by stripping style/color attributes
    cleanPastedHtml: (html) => {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');

        const allElements = doc.body.querySelectorAll('*');
        allElements.forEach(el => {
            el.removeAttribute('style');
            el.removeAttribute('color');
            el.removeAttribute('face');
            el.removeAttribute('class');
        });

        return doc.body.innerHTML;
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
        if (el) {
            el.focus();
            const url = prompt("Enter the URL:");
            if (url) {
                document.execCommand('createLink', false, url);
                el.dispatchEvent(new Event('input', { bubbles: true }));
            }
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

    // Changes text foreground color
    applyTextColor: (id, colorHex) => {
        const el = document.getElementById(id);
        if (!el) return;

        el.focus();
        document.execCommand('foreColor', false, colorHex);
    },

    // Changes text background highlight color
    applyBackgroundColor: (id, colorHex) => {
        const el = document.getElementById(id);
        if (!el) return;

        el.focus();
        document.execCommand('hiliteColor', false, colorHex);
    },

    // Fixed Picmo Picker with cursor position preservation & viewport alignment
    openPicmoPicker: (domId, btnId) => {
        const triggerBtn = document.getElementById(btnId);
        if (!triggerBtn) return;

        // Save selection position BEFORE focus shifts to Picmo
        window.writerEngine.saveSelection();

        // Toggle off if picker is already open
        const existingPicker = document.getElementById('picmo-popover');
        if (existingPicker) {
            existingPicker.remove();
            if (existingPicker.dataset.btnId === btnId) return;
        }

        const rect = triggerBtn.getBoundingClientRect();

        // Create container appended directly to body with viewport fixed positioning
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

        // Handle emoji selection
        picker.addEventListener('emoji:select', selection => {
            const el = document.getElementById(domId);
            if (el) {
                el.focus();

                // Restore active range right before inserting text
                window.writerEngine.restoreSelection();

                // Insert emoji text at exact cursor location
                document.execCommand('insertText', false, selection.emoji);

                // Dispatch input event so Blazor SyncContent captures updates
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

    // Split document content and export the latter part of the string
    splitContent: (domId) => {
        const el = document.getElementById(domId);
        if (!el) return "";

        const selection = window.getSelection();

        if (!selection.rangeCount || !el.contains(selection.anchorNode)) {
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