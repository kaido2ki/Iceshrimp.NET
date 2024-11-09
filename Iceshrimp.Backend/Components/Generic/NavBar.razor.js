function toggleHamburger(e) {
    e.preventDefault();

    for (let el of document.getElementsByClassName("hamburger-menu")) {
        if (el.classList.contains("hidden")) {
            el.classList.remove('hidden');
        } else {
            el.classList.add('hidden');
        }
    }
}