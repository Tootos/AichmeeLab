window.writerEngine = {

    // Load data from C# to Html
    loadContent: (domId, htmlContent) => {
        const el = document.getElementById(domId);
        if (!el) return;

        // If content is null/empty, seed it with the default P tag
        if (!htmlContent || htmlContent.trim() === "") {
            el.innerHTML = "<p><br></p>";
        } else {
            // Inject the HTML from the DB
            el.innerHTML = htmlContent;
        }

        // 3. Set the paragraph separator rule for future typing
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

    // Move the side menu position relative to the position of the cursor
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

        // If we have a valid height (we are on text)
        if (rect.height > 0) {
            offsetTop = rect.top - parentRect.top;
        } else {
            // FALLBACK: If we are on an empty line, we need to find the cursor's Y
            // We create a temporary element to measure the line height
            const dummy = document.createElement("span");
            dummy.textContent = "\u200b"; // zero-width space
            range.insertNode(dummy);
            offsetTop = dummy.getBoundingClientRect().top - parentRect.top;
            dummy.remove();
        }

        // APPLY THE POSITION
        // We add a tiny adjustment (2-5px) to center the '+' icon with the text line
        menu.style.display = 'flex';
        menu.style.top = (offsetTop + 2) + "px";
        menu.classList.add('is-active');
    },

    initInlineToolbar: () => {
        const toolbar = document.getElementById('inline-toolbar');

        document.addEventListener('selectionchange', () => {
            const selection = window.getSelection();
            if (selection.rangeCount > 0 && !selection.isCollapsed) {
                const range = selection.getRangeAt(0);
                const rect = range.getBoundingClientRect();

                toolbar.style.display = 'flex';
                toolbar.style.top = `${rect.top - 45 + window.scrollY}px`;
                toolbar.style.left = `${rect.left + (rect.width / 2) - (toolbar.offsetWidth / 2)}px`;
            } else {
                toolbar.style.display = 'none';
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
        if (el) {
            el.focus();
            const url = prompt("Enter the URL:");
            if (url) {
                document.execCommand('createLink', false, url);
                el.dispatchEvent(new Event('input', { bubbles: true }));
            }
        }
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