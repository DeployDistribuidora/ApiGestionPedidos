// Función reutilizable para habilitar búsqueda con Enter y botón
function enableSearchOnEnter(inputSelector, buttonSelector, searchCallback) {
    const input = document.querySelector(inputSelector);
    const button = document.querySelector(buttonSelector);

    if (input && button) {
        button.addEventListener("click", searchCallback);
        input.addEventListener("keydown", (e) => {
            if (e.key === "Enter") {
                e.preventDefault(); // Evita el comportamiento predeterminado
                searchCallback();
            }
        });
    }
}

// Exporta la función para uso global
export { enableSearchOnEnter };
