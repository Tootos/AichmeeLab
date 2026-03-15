let editorInstance;

window.setupEditor = (id, data) => {

    const el = document.getElementById(id);
    if (!el || el.childElementCount > 0) return;

    // Parse the data if it exists
    let parsedData = {};
    if (data) {
        try {
            parsedData = JSON.parse(data);
        } catch (e) {
            console.error("Failed to parse initial EditorJS data", e);
        }
    }
     
    window.editorInstance = new EditorJS({
        holder: id,
        minHeight: 0,
        data: parsedData,
        tools: {
            header: Header,
            list: EditorjsList // Direct use of the new variable name
        }
    });

    // FIX: Look for the wrapper around the element, not inside it
    const wrapper = el.closest('.editor-wrapper') || el.parentElement;
    // --- Drag and Drop Logic ---
    el.addEventListener('dragover', (e) => {
        const files = e.dataTransfer.items;
        e.preventDefault();

        wrapper.classList.add('dragging-active');
    });

    el.addEventListener('dragleave', () => {
        wrapper.classList.remove('dragging-active', 'invalid-file');
        el.style.backgroundColor = "transparent";
    });

    el.addEventListener('drop', (e) => {

        e.preventDefault();
        wrapper.classList.remove('dragging-active');

        const file = e.dataTransfer.files[0];

        // 1. Validation: Check if it's the right file type
        const isDocx = file.type === "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        if (!isDocx) {
            alert("Invalid file type! Only .docx is accepted.");

            setTimeout(() => {
                wrapper.classList.remove('invalid-file');
            }, 2000);

            return;
        }else {
            const reader = new FileReader();
            reader.onload = async (event) => {
                const arrayBuffer = event.target.result;

                // 2. Convert Docx to HTML using Mammoth
                mammoth.convertToHtml({ arrayBuffer: arrayBuffer })
                    .then(function (result) {
                        const html = result.value; // The generated HTML

                        // 3. Optional: Convert HTML to Editor.js blocks
                        window.editorInstance.blocks.clear();

                        // 2. Inject the HTML string. 
                        // Editor.js will automatically parse <h2> as Header blocks, 
                        // <ul> as List blocks, and <p> as Paragraph blocks.
                        window.editorInstance.blocks.renderFromHTML(html);

                    })
                    .catch(function (err) {
                        console.error(err);
                    });
            };
            reader.readAsArrayBuffer(file);
        }
    });

};

window.updateEditorData = async (content) => {
    if (window.editorInstance && window.editorInstance.isReady) {
        await window.editorInstance.isReady
        if(content){
const data = JSON.parse(content);
        window.editorInstance.render(data); // Clears old blocks and renders new ones
        }else{
            window.editorInstance.blocks.clear();
        }     
    } else if (window.editorInstance) {
        window.editorInstance.blocks.clear(); // Clear for new articles
    }
};

window.saveEditorData = async () => {
    const data = await window.editorInstance.save();
    return JSON.stringify(data);
};