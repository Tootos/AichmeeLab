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
    // initInlineToolbar: (dotNetRef) => {
    //     document.addEventListener('selectionchange', () => {
    //         // Continuously track selection inside editable blocks
    //         window.writerEngine.saveSelection();

    //         const toolbar = document.getElementById('inline-toolbar');
            
    //         // Safe guard against unmounted DOM toolbar
    //         if (!toolbar) return;

    //         const selection = window.getSelection();
    //         if (selection.rangeCount > 0 && !selection.isCollapsed) {
    //             const range = selection.getRangeAt(0);
    //             const rect = range.getBoundingClientRect();

    //             toolbar.style.display = 'flex';
    //             toolbar.style.top = `${rect.top - 45 + window.scrollY}px`;
    //             toolbar.style.left = `${rect.left + (rect.width / 2) - (toolbar.offsetWidth / 2) - 100}px`;
    //         } else {
    //             toolbar.style.display = 'none';
    //         }

    //         if (dotNetRef && selection && selection.rangeCount > 0) {
    //         const activeEl = document.activeElement;
    //         if (activeEl && activeEl.isContentEditable) {
    //             const colors = window.writerEngine.getSelectionColors();
    //             dotNetRef.invokeMethodAsync('UpdateToolbarColors', colors.textColor, colors.bgColor);
    //         }
    //     }
    //     });
    // },

    // getSelectionColors: () => {
    //     const selection = window.getSelection();
    // if (!selection || selection.rangeCount === 0) return { textColor: '#000000', bgColor: '#ffffff' };

    // let node = selection.anchorNode;
    // if (!node) return { textColor: '#000000', bgColor: '#ffffff' };

    // if (node.nodeType === Node.TEXT_NODE) {
    //     node = node.parentElement;
    // }

    // const computed = window.getComputedStyle(node);

    // // Reads exact computed style under cursor (falling back to parent/CSS defaults)
    // const textColor = window.writerEngine.rgbToHex(computed.color) || '#000000';
    // const bgColor = window.writerEngine.rgbToHex(computed.backgroundColor) || '#ffffff';

    // return { textColor, bgColor };
    // },

    // rgbToHex: (rgb) => {
    //     if (!rgb || rgb === 'transparent' || rgb === 'rgba(0, 0, 0, 0)') return null;
    //     const matches = rgb.match(/\d+/g);
    //     if (!matches || matches.length < 3) return null;
    //     return "#" + matches.slice(0, 3).map(x => parseInt(x).toString(16).padStart(2, '0')).join('');
    // },

    cleanPastedHandler: () => {
    document.addEventListener('paste', (e) => {
        const target = e.target;
        // Only sanitize if pasting inside a contenteditable block
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

    // 1. Strip background & dark-mode styles injected directly on doc.body
    doc.body.removeAttribute('style');
    doc.body.removeAttribute('color');
    doc.body.removeAttribute('class');
    doc.body.removeAttribute('bgcolor');

    // 2. Strip attributes from all child elements inside body
    const allElements = doc.body.querySelectorAll('*');
    allElements.forEach(el => {
        el.removeAttribute('style');
        el.removeAttribute('color');
        el.removeAttribute('face');
        el.removeAttribute('class');
        el.removeAttribute('bgcolor');
    });

    return doc.body.innerHTML;
},

// Reset formatting when pressing Enter
 initEnterKeyReset:() => {
    if (window.writerEngine._enterHandlerInitialized) return;
    window.writerEngine._enterHandlerInitialized = true;

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            const target = e.target;
            if (!target || !target.isContentEditable) return;

            // Allow normal paragraph break creation, then strip lingering inline color styles
            setTimeout(() => {
                document.execCommand('removeFormat', false, null);
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
        const el = document.getElementById(domId);
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