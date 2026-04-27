/**
 * Editor.js Multi-Instance Interop
 * Stores instances in a Map keyed by the DOM element ID.
 */
window.editorInstances = {};

// --- Named Event Handlers (Modified to handle specific IDs) ---

const handleDragOver = (e) => {
    e.preventDefault();
    const el = e.currentTarget;
    const wrapper = el.closest('.editor-wrapper') || el.parentElement;
    wrapper.classList.add('dragging-active');
};

const handleDragLeave = (e) => {
    const el = e.currentTarget;
    const wrapper = el.closest('.editor-wrapper') || el.parentElement;
    wrapper.classList.remove('dragging-active', 'invalid-file');
    el.style.backgroundColor = "transparent";
};

/**
 * Handle .docx drop for a specific editor instance
 */
const handleDrop = (e, id) => {
    e.preventDefault();
    const el = e.currentTarget;
    const wrapper = el.closest('.editor-wrapper') || el.parentElement;
    wrapper.classList.remove('dragging-active');

    const file = e.dataTransfer.files[0];
    const isDocx = file.type === "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    if (!isDocx) {
        alert("Invalid file type! Only .docx is accepted.");
        return;
    }

    const reader = new FileReader();
    reader.onload = async (event) => {
        const arrayBuffer = event.target.result;
        mammoth.convertToHtml({ arrayBuffer: arrayBuffer })
            .then(function (result) {
                const html = result.value;
                const instance = window.editorInstances[id];
                // Safety check for instance before rendering
                if (instance && typeof instance.blocks?.renderFromHTML === 'function') {
                    instance.blocks.clear();
                    instance.blocks.renderFromHTML(html);
                }
            })
            .catch(err => console.error("Mammoth error:", err));
    };
    reader.readAsArrayBuffer(file);
};

// --- Window Functions ---

/**
 * Initializes a new Editor.js instance for a specific block
 */
window.setupEditor = async (id, data) => {
    const el = document.getElementById(id);
    if (!el) return;

    if (el.innerHTML.trim() !== "" || window.editorInstances[id]) {
        console.warn(`Purging existing content in ${id} to prevent nesting.`);
        
        // 1. Destroy existing instance if it exists in memory
        if (window.editorInstances[id]) {
            try {
                await window.editorInstances[id].isReady;
                await window.editorInstances[id].destroy();
            } catch (e) {}
            delete window.editorInstances[id];
        }
        
        // 2. Physically wipe the HTML
        el.innerHTML = "";
    }

    // const existing = window.editorInstances[id];
    // if (existing) {
    //     try {
    //         if (typeof existing.destroy === 'function') {
    //             await existing.isReady;
    //             await existing.destroy();
    //         }
    //     } catch (e) {
    //         console.warn(`Soft-cleanup for ${id}:`, e);
    //     }
    //     delete window.editorInstances[id];
    // }

    let parsedData = { blocks: [] };
    if (data && data !== "null" && data.trim() !== "") {
        try {
            parsedData = typeof data === 'string' ? JSON.parse(data) : data;
        } catch (e) {
            console.error(`Parse error for ${id}`, e);
        }
    }

    // 2. Initialize
    const newInstance = new EditorJS({
        holder: id,
        minHeight: 0,
        data: parsedData,
        tools: {
            header: Header,
            list: {
                class: EditorjsList,
                inlineToolbar: true,
                config: { defaultStyle: 'unordered' }
            }
        },
        onReady: () => {
            el.addEventListener('dragover', handleDragOver);
            el.addEventListener('dragleave', handleDragLeave);
            el.addEventListener('drop', (e) => handleDrop(e, id));
        }
    });

    window.editorInstances[id] = newInstance;
};

/**
 * Saves a specific editor block and returns the JSON string
 */
window.saveEditorInstance = async (id) => {
    const instance = window.editorInstances[id];
    if (!instance) return "{\"blocks\":[]}";

    try {
        await instance.isReady;
        const data = await instance.save();
        return JSON.stringify(data);
    } catch (err) {
        console.error(`Error saving instance ${id}:`, err);
        return "{\"blocks\":[]}";
    }
};
/**
 * Save ALL editor blocks, return json strings and ids
 */
window.saveAllEditors = async () => {
    const results = [];
    const keys = Object.keys(window.editorInstances);

    for (const id of keys) {
        const instance = window.editorInstances[id];
        if (instance && typeof instance.save === 'function') {
            try {
                await instance.isReady;
                const data = await instance.save();
                results.push({
                    elementId: id,
                    content: JSON.stringify(data)
                });
            } catch (err) {
                console.error(`Error sweeping instance ${id}:`, err);
            }
        }
    }
    return results;
};

/**
 * Updates data for a specific instance
 */
window.updateEditorData = async (id, content) => {
    const instance = window.editorInstances[id];
    if (!instance) return;

    try {
        await instance.isReady;
        if (content && content.trim() !== "" && content !== "null") {
            const data = JSON.parse(content);
            await instance.render(data);
        } else {
            await instance.blocks.clear();
        }
    } catch (err) {
        console.error(`EditorJS Update Error for ${id}:`, err);
    }
};

/**
 * Destroys a specific editor block
 */
window.destroyEditor = async (id) => {
    const instance = window.editorInstances[id];
    if (instance) {
        try {
            const el = document.getElementById(id);
            if (el) {
                el.removeEventListener('dragover', handleDragOver);
                el.removeEventListener('dragleave', handleDragLeave);
                el.removeEventListener('drop', handleDrop);
            }

            await instance.destroy();
            delete window.editorInstances[id];
        } catch (e) {
            console.error(`Error during destroyEditor for ${id}:`, e);
        }
    }
};


/**
 * Destroys all active editor instances and cleans up listeners
 */
window.destroyAllEditors = async () => {
    if (!window.editorInstances) return;

    // Get all editor IDs currently tracked
    const ids = Object.keys(window.editorInstances);

    // Map each ID to a destruction promise to run them efficiently
    const destroyPromises = ids.map(async (id) => {
        try {
            const instance = window.editorInstances[id];
            const el = document.getElementById(id);

            // Clean up event listeners if the element still exists
            if (el) {
                el.removeEventListener('dragover', handleDragOver);
                el.removeEventListener('dragleave', handleDragLeave);
                el.removeEventListener('drop', handleDrop);
                el.innerHTML = '';
                el.className = '';
            }

            // Destroy the Editor.js instance
            if (instance && typeof instance.destroy === 'function') {
                await instance.destroy();
            }

            // 3. Remove from our tracking object
            delete window.editorInstances[id];
        } catch (e) {
            console.error(`Failed to destroy editor instance ${id}:`, e);
        }
    });

    // Wait for all editors to be fully cleaned up
    await Promise.all(destroyPromises);

    // Optional: Force clear the object just to be safe
    window.editorInstances = {};
};

window.destroyAllExceptRoot = async () => {
    const ids = Object.keys(window.editorInstances);
    for (const id of ids) {
        if (id !== 'editor-0') {
            const instance = window.editorInstances[id];
            if (instance && typeof instance.destroy === 'function') {
                await instance.isReady;
                await instance.destroy();
            }
            delete window.editorInstances[id];
            
            const el = document.getElementById(id);
            if (el) el.innerHTML = ''; 
        }
    }
};
/**
 * Checks if an Editor.js instance is currently active for a specific ID
 */
window.editorExists = (id) => {
    //  Check if the dictionary itself exists
    if (!window.editorInstances) return false;

    //  Check if the key exists
    const instance = window.editorInstances[id];

    //  Ensure it's not null and has been initialized
    // (We check for 'save' or 'destroy' to ensure it's a real Editor object)
    return !!(instance && typeof instance.save === 'function');
};
