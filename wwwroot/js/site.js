// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    const openBtn = document.getElementById("sidebarToggle");
    const closeBtn = document.getElementById("sidebarClose");
    const sidebar = document.getElementById("sidebar");
    const main = document.getElementById("main");


    // Check if elements exist
    if (!openBtn || !closeBtn || !sidebar || !main) {
        console.log("Sidebar elements not found");
        return;
    }


    openBtn.addEventListener("click", function () {

        sidebar.classList.add("show");

        main.classList.add("shift");

        openBtn.style.display = "none";

        closeBtn.style.display = "block";

    });



    closeBtn.addEventListener("click", function () {

        sidebar.classList.remove("show");

        main.classList.remove("shift");

        closeBtn.style.display = "none";

        openBtn.style.display = "block";

    });


});