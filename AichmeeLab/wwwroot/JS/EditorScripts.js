let editorInstance;

// --- Named Event Handlers (Prevents Memory Leaks) ---
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

const handleDrop = (e) => {
    e.preventDefault();
    const el = e.currentTarget;
    const wrapper = el.closest('.editor-wrapper') || el.parentElement;
    wrapper.classList.remove('dragging-active');

    const file = e.dataTransfer.files[0];
    const isDocx = file.type === "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    if (!isDocx) {
        alert("Invalid file type! Only .docx is accepted.");
        setTimeout(() => {
            wrapper.classList.remove('invalid-file');
        }, 2000);
        return;
    }

    const reader = new FileReader();
    reader.onload = async (event) => {
        const arrayBuffer = event.target.result;
        mammoth.convertToHtml({ arrayBuffer: arrayBuffer })
            .then(function (result) {
                const html = result.value;
                if (window.editorInstance) {
                    window.editorInstance.blocks.clear();
                    // Note: Ensure your Editor.js version/plugins support renderFromHTML
                    window.editorInstance.blocks.renderFromHTML(html);
                }
            })
            .catch(function (err) {
                console.error("Mammoth conversion error:", err);
            });
    };
    reader.readAsArrayBuffer(file);
};

// --- Window Functions ---

window.setupEditor = async (id, data) => {
    const el = document.getElementById(id);
    if (!el) return;

    // 1. If there is an old instance, kill it
    if (window.editorInstance) {
        await window.editorInstance.destroy();
        window.editorInstance = null;
    }

    // 2. Ensure the element is empty before starting
    el.innerHTML = '';

    let parsedData = {blocks: []};
    if (data && data !== "null" && data.trim() !== "") {
        try {
            parsedData = typeof data === 'string' ? JSON.parse(data) : data;
            
            if (!parsedData.blocks) {
                parsedData.blocks = [];
            }
        } catch (e) {
            console.error("Failed to parse initial EditorJS data", e);
        }
    }

    // 3. Initialize Instance
    window.editorInstance = new EditorJS({
        holder: id,
        minHeight: 0,
        data: parsedData,
        tools: {
            header: Header,
            list: {
            class: EditorjsList, 
            inlineToolbar: true,
            config: {
                defaultStyle: 'unordered' 
            }}
        },
        onReady: () => {
            // Attach Drag & Drop listeners only when editor is ready
            el.addEventListener('dragover', handleDragOver);
            el.addEventListener('dragleave', handleDragLeave);
            el.addEventListener('drop', handleDrop);
        }
    });
};

window.updateEditorData = async (content) => {
    if (!window.editorInstance) {
        console.warn("Editor instance not found during update.");
        return;
    }

    try {
        await window.editorInstance.isReady;

        if (content && content.trim() !== "" && content !== "null") {
            const data = JSON.parse(content);
            await window.editorInstance.render(data);
        } else {
            await window.editorInstance.blocks.clear();
        }
    } catch (err) {
        console.error("EditorJS Update Error:", err);
    }
};

window.destroyEditor = async () => {
    if (window.editorInstance) {
        try {
            const el = document.getElementById('editorjs');
            if (el) {
                el.removeEventListener('dragover', handleDragOver);
                el.removeEventListener('dragleave', handleDragLeave);
                el.removeEventListener('drop', handleDrop);
            }
            
            if (typeof window.editorInstance.destroy === 'function') {
                await window.editorInstance.destroy();
            }
            window.editorInstance = null;
        } catch (e) {
            console.error("Error during destroyEditor:", e);
        }
    }
};

window.saveEditorData = async () => {
    if (!window.editorInstance) return "{}";
    await window.editorInstance.isReady;
    const data = await window.editorInstance.save();
    return JSON.stringify(data);
};