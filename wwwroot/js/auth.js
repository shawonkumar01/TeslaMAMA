function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);

    if (!input || !button) {
        return;
    }

    const showPassword = input.type === "password";

    input.type = showPassword ? "text" : "password";

    button.setAttribute(
        "aria-pressed",
        String(showPassword)
    );

    button.setAttribute(
        "aria-label",
        showPassword ? "Hide password" : "Show password"
    );

    button.classList.toggle("is-visible", showPassword);
}