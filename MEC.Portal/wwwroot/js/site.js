document.addEventListener("DOMContentLoaded", function () {
    const phoneInputs = Array.from(document.querySelectorAll("[data-phone-input='true']"));
    phoneInputs.forEach(initializePhoneInput);

    const dateYearSelects = Array.from(document.querySelectorAll("[data-date-year-select='true']"));
    dateYearSelects.forEach(initializeDateYearSelect);

    const editors = Array.from(document.querySelectorAll("[data-child-editor='true']"));
    editors.forEach(initializeChildEditor);
});

function initializePhoneInput(input) {
    const formatPhoneNumber = () => {
        const digits = input.value.replace(/\D/g, "").slice(0, 11);
        const groups = [
            digits.slice(0, 4),
            digits.slice(4, 7),
            digits.slice(7, 9),
            digits.slice(9, 11)
        ];

        input.value = groups.filter(Boolean).join(" ");
    };

    input.addEventListener("input", formatPhoneNumber);
    formatPhoneNumber();
}

function initializeDateYearSelect(select) {
    const targetId = select.getAttribute("data-date-year-target");
    const dateInput = targetId ? document.getElementById(targetId) : null;
    if (!dateInput) {
        return;
    }

    select.addEventListener("change", function () {
        const year = Number.parseInt(select.value, 10);
        if (!Number.isInteger(year)) {
            return;
        }

        const currentDateParts = dateInput.value.split("-");
        const month = currentDateParts[1] || "01";
        const requestedDay = Number.parseInt(currentDateParts[2] || "1", 10);
        const lastDayOfMonth = new Date(year, Number.parseInt(month, 10), 0).getDate();
        const day = String(Math.min(requestedDay, lastDayOfMonth)).padStart(2, "0");

        dateInput.value = year + "-" + month + "-" + day;
        dateInput.dispatchEvent(new Event("change", { bubbles: true }));
        dateInput.focus();
    });
}

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
