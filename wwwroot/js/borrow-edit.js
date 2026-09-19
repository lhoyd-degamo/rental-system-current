// ==========================================
// Borrow / Edit - click-to-select item cards
// Extracted from Views/Borrow/Edit.cshtml
// ==========================================


document.addEventListener("DOMContentLoaded", function() {

    const itemCards =
        document.querySelectorAll(".item-card");

    itemCards.forEach(function(card) {

        const checkbox =
            card.querySelector("input[type='checkbox']");

        card.addEventListener("click", function() {

            checkbox.checked =
                !checkbox.checked;

            card.classList.toggle(
                "selected",
                checkbox.checked
            );

        });

    });

});

