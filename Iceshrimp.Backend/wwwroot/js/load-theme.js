onload = (ev) => {
    const themePrefs = window.localStorage.getItem("theme");
    if (themePrefs === "light") document.body.classList.add("theme-light");
    else if (themePrefs === "dark") document.body.classList.add("theme-dark");
};