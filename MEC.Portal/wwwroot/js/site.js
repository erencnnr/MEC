document.addEventListener("DOMContentLoaded", function () {
    const editors = Array.from(document.querySelectorAll("[data-child-editor='true']"));
    editors.forEach(initializeChildEditor);
});

function initializeChildEditor(editor) {
    const list = editor.querySelector("[data-child-list='true']");
    const template = editor.querySelector("[data-child-template='true']");
    const addButton = editor.querySelector("[data-add-child='true']");

    if (!list || !template || !addButton) {
        return;
    }

    const syncRows = () => {
        const rows = Array.from(list.querySelectorAll("[data-child-row='true']"));
        rows.forEach((row, index) => {
            const label = row.querySelector("[data-child-label='true']");
            if (label) {
                label.textContent = "Çocuk " + (index + 1);
            }

            const fields = Array.from(row.querySelectorAll("[data-field]"));
            fields.forEach((field) => {
                const fieldName = field.getAttribute("data-field");
                if (!fieldName) {
                    return;
                }

                field.setAttribute("name", "Children[" + index + "]." + fieldName);
                field.setAttribute("id", "Children_" + index + "__" + fieldName);
            });
        });
    };

    addButton.addEventListener("click", function () {
        const fragment = template.content.cloneNode(true);
        list.appendChild(fragment);
        syncRows();
    });

    editor.addEventListener("click", function (event) {
        const target = event.target;
        if (!(target instanceof HTMLElement)) {
            return;
        }

        const removeButton = target.closest("[data-remove-child='true']");
        if (!removeButton) {
            return;
        }

        const row = removeButton.closest("[data-child-row='true']");
        if (!row) {
            return;
        }

        row.remove();
        syncRows();
    });

    syncRows();
}
