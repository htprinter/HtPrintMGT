const draggingStyle = window.document.createElement("style");
draggingStyle.innerHTML = ":root.x-dragging{cursor:move!important}";
window.document.head.appendChild(draggingStyle);
window.addEventListener("mousedown", event => {
    const target = event.target;
    if (!target) return;
    /** @type {HTMLElement} */
    const parent = target.closest("[data-drag-parent]");
    if (!parent) return;
    const draggingElement = parent.closest(parent.dataset.dragParent);
    if (!draggingElement) return;
    const rect = draggingElement.getBoundingClientRect();
    const beginElementX = rect.left;
    const beginElementY = rect.top;
    const beginMouseX = event.clientX;
    const beginMouseY = event.clientY;
    /** @type {(event:MouseEvent)=>void} */
    const dragging = event => {
        const diffX = event.clientX - beginMouseX;
        const diffY = event.clientY - beginMouseY;
        draggingElement.style.left = `${beginElementX + diffX}px`;
        draggingElement.style.top = `${beginElementY + diffY}px`;
    }
    const endDrag = () => {
        window.document.documentElement.classList.remove("x-dragging");
        window.removeEventListener("mousemove", dragging);
        window.removeEventListener("mouseup", endDrag);
    }
    window.document.documentElement.classList.add("x-dragging");
    window.addEventListener("mouseup", endDrag);
    window.addEventListener("mousemove", dragging);
});