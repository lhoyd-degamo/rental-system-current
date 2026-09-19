// ==========================================
// Borrow / Create - item search, pagination,
// item selection, and form validation.
// Extracted from Views/Borrow/Create.cshtml
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const PAGE_SIZE = 10;

    const searchBox =
        document.getElementById("itemSearch");

    const selectedBody =
        document.getElementById("selectedItemsBody");

    const availableBody =
        document.getElementById("availableItemsBody");

    const paginationList =
        document.getElementById("availableItemsPagination");

    const form =
        document.getElementById("borrowForm");

    const selectedItems = new Map();

    // ==========================================
    // PREVENT ENTER KEY FROM SUBMITTING THE FORM
    // The search box lives inside <form id="borrowForm">,
    // so pressing Enter while searching would otherwise
    // submit the whole Borrow form instead of just
    // filtering the list.
    // ==========================================

    searchBox.addEventListener("keydown", function (e) {

        if (e.key === "Enter") {

            e.preventDefault();

        }

    });


    // GET ALL AVAILABLE ITEM ROWS
    const allRows =
        Array.from(
            availableBody.querySelectorAll(
                ".available-item-row"
            )
        );


    let filteredRows = allRows.slice();

    let currentPage = 1;


    // ==========================================
    // SEARCH ITEM NAME ONLY
    // ==========================================

    searchBox.addEventListener("input", function() {

        const search =
            this.value
                .toLowerCase()
                .trim();


        filteredRows =
            allRows.filter(function(row) {

                // ONLY ITEM NAME
                const itemName =
                    row.dataset.search || "";

                return itemName.includes(search);

            });


        // Reset to page 1 after searching
        currentPage = 1;

        renderAvailablePage();

    });


    // ==========================================
    // RENDER AVAILABLE ITEMS
    // ==========================================

    function renderAvailablePage() {

        const totalPages =
            Math.max(
                1,
                Math.ceil(
                    filteredRows.length / PAGE_SIZE
                )
            );


        if (currentPage > totalPages) {
            currentPage = totalPages;
        }


        // Hide all rows
        allRows.forEach(function(row) {

            row.style.display = "none";

        });


        // Remove old no-result message
        availableBody
            .querySelectorAll(".no-results-row")
            .forEach(function(row) {

                row.remove();

            });


        // NO RESULTS
        if (filteredRows.length === 0) {

            const emptyRow =
                document.createElement("tr");

            emptyRow.className =
                "no-results-row";


            const emptyCell =
                document.createElement("td");

            emptyCell.colSpan = 6;

            emptyCell.className =
                "text-center py-4";

            emptyCell.textContent =
                "No matching item name found.";


            emptyRow.appendChild(emptyCell);

            availableBody.appendChild(emptyRow);

        }

        // RESULTS
        else {

            const start =
                (currentPage - 1) * PAGE_SIZE;

            const end =
                start + PAGE_SIZE;


            filteredRows
                .slice(start, end)
                .forEach(function(row) {

                    row.style.display = "";

                });

        }


        renderPaginationControls(totalPages);

    }


    // ==========================================
    // PAGINATION
    // ==========================================

    function renderPaginationControls(totalPages) {

        paginationList.innerHTML = "";


        if (totalPages <= 1) {
            return;
        }


        function addPageItem(
            label,
            page,
            opts
        ) {

            opts = opts || {};


            const li =
                document.createElement("li");


            li.className =
                "page-item" +
                (opts.disabled
                    ? " disabled"
                    : "") +
                (opts.active
                    ? " active"
                    : "");


            const link =
                document.createElement("a");


            link.className =
                "page-link";

            link.href = "#";

            link.textContent =
                label;


            link.addEventListener(
                "click",
                function(e) {

                    e.preventDefault();


                    if (
                        opts.disabled ||
                        opts.active
                    ) {
                        return;
                    }


                    currentPage = page;

                    renderAvailablePage();

                }
            );


            li.appendChild(link);

            paginationList.appendChild(li);

        }


        // PREVIOUS
        addPageItem(
            "Prev",
            currentPage - 1,
            {
                disabled:
                    currentPage === 1
            }
        );


        // PAGE NUMBERS
        for (
            let page = 1;
            page <= totalPages;
            page++
        ) {

            addPageItem(
                String(page),
                page,
                {
                    active:
                        page === currentPage
                }
            );

        }


        // NEXT
        addPageItem(
            "Next",
            currentPage + 1,
            {
                disabled:
                    currentPage === totalPages
            }
        );

    }


    // ==========================================
    // ADD ITEM
    // ==========================================

    allRows.forEach(function(row) {

        const button =
            row.querySelector(
                ".add-item-btn"
            );


        button.addEventListener(
            "click",
            function() {

                const id =
                    this.dataset.id;


                // Prevent duplicate item
                if (selectedItems.has(id)) {
                    return;
                }


                selectedItems.set(
                    id,
                    {
                        id: id,
                        code:
                            this.dataset.code,
                        name:
                            this.dataset.name,
                        category:
                            this.dataset.category,
                        size:
                            this.dataset.size,
                        price:
                            this.dataset.price
                    }
                );


                this.disabled = true;

                this.innerText =
                    "Added";


                renderSelectedItems();

            }
        );

    });


    // ==========================================
    // DISPLAY SELECTED ITEMS
    // ==========================================

    function renderSelectedItems() {

        selectedBody.innerHTML = "";


        // Remove old hidden inputs
        document
            .querySelectorAll(
                ".selected-item-input"
            )
            .forEach(function(input) {

                input.remove();

            });


        // NO SELECTED ITEMS
        if (selectedItems.size === 0) {

            const row =
                document.createElement("tr");


            const cell =
                document.createElement("td");


            cell.colSpan = 6;

            cell.className =
                "text-center py-4";

            cell.textContent =
                "No items selected.";


            row.appendChild(cell);

            selectedBody.appendChild(row);

            return;

        }


        // DISPLAY SELECTED ITEMS
        selectedItems.forEach(
            function(item) {

                const row =
                    document.createElement("tr");


                function addCell(text) {

                    const td =
                        document.createElement("td");

                    td.textContent =
                        text;

                    row.appendChild(td);

                    return td;

                }


                addCell(
                    item.code || ""
                );

                addCell(
                    item.name || ""
                );

                addCell(
                    item.category || ""
                );

                addCell(
                    item.size || ""
                );


                addCell(
                    "\u20B1" +
                    Number(
                        item.price
                    ).toLocaleString(
                        "en-PH",
                        {
                            minimumFractionDigits: 2
                        }
                    )
                );


                // ACTION CELL
                const actionCell =
                    document.createElement("td");


                const removeButton =
                    document.createElement(
                        "button"
                    );


                removeButton.type =
                    "button";

                removeButton.className =
                    "btn remove-item";

                removeButton.dataset.id =
                    item.id;

                removeButton.textContent =
                    "Remove";


                removeButton.addEventListener(
                    "click",
                    function() {

                        const id =
                            this.dataset.id;


                        selectedItems.delete(id);


                        const addButton =
                            document.querySelector(
                                '.add-item-btn[data-id="' +
                                id +
                                '"]'
                            );


                        if (addButton) {

                            addButton.disabled =
                                false;

                            addButton.innerText =
                                "Add";

                        }


                        renderSelectedItems();

                    }
                );


                actionCell.appendChild(
                    removeButton
                );


                row.appendChild(
                    actionCell
                );


                selectedBody.appendChild(
                    row
                );


                // =================================
                // HIDDEN ITEM ID
                // =================================

                const input =
                    document.createElement(
                        "input"
                    );


                input.type = "hidden";

                input.name =
                    "SelectedItemIDs";

                input.value =
                    item.id;

                input.className =
                    "selected-item-input";


                form.appendChild(input);

            }
        );

    }


    // ==========================================
    // FORM VALIDATION
    // ==========================================

    form.addEventListener(
        "submit",
        function(event) {

            // Must have item
            if (selectedItems.size === 0) {

                event.preventDefault();

                alert(
                    "Please add at least one item."
                );

                return;

            }


            const borrowDate =
                document.getElementById(
                    "borrowDate"
                );


            const returnDate =
                document.getElementById(
                    "returnDate"
                );


            const today =
                new Date()
                    .toISOString()
                    .split("T")[0];


            // Borrow date cannot be future
            if (
                borrowDate.value >
                today
            ) {

                event.preventDefault();

                alert(
                    "Borrow date cannot be a future date."
                );

                return;

            }


            // Return date required
            if (!returnDate.value) {

                event.preventDefault();

                alert(
                    "Please select a return date."
                );

                return;

            }


            // Return date cannot be earlier
            if (
                returnDate.value <
                borrowDate.value
            ) {

                event.preventDefault();

                alert(
                    "Return date cannot be earlier than the borrow date."
                );

                return;

            }

        }
    );


    // ==========================================
    // DATE RESTRICTIONS
    // ==========================================

    const borrowDate =
        document.getElementById(
            "borrowDate"
        );


    const returnDate =
        document.getElementById(
            "returnDate"
        );


    // Borrow date cannot be future
    borrowDate.max =
        new Date()
            .toISOString()
            .split("T")[0];


    // Return date minimum follows borrow date
    borrowDate.addEventListener(
        "change",
        function() {

            returnDate.min =
                this.value;

        }
    );


    // ==========================================
    // INITIAL RENDER
    // ==========================================

    renderAvailablePage();

});

