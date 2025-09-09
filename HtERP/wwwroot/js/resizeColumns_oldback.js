const resizeStyle = window.document.createElement("style");
resizeStyle.innerHTML = ":root.x-resizing-column{cursor:col-resize!important}";
window.document.head.appendChild(resizeStyle);
window.addEventListener("mousedown", event => {
    const target = event.target;
    if (!target) return;
    /** @type {HTMLElement} */
    const parent = target.closest(".resize-handle");
    if (!parent) return;
    const resizingElement = parent.closest("th");
    if (!resizingElement) return;
    const rect = resizingElement.getBoundingClientRect();
    const beginElementW = rect.width;
    const beginMouseX = event.clientX;
    if (!resizingElement.dataset.originalStyle) {
        resizingElement.dataset.originalStyle = resizingElement.style.width;
    }
    /** @type {(event:MouseEvent)=>void} */
    const resizing = event => {
        const diff = event.clientX - beginMouseX;
        resizingElement.style.width = `${Math.max(30, beginElementW + diff)}px`;
    }
    const endResize = () => {
        window.document.documentElement.classList.remove("x-resizing-column");
        window.removeEventListener("mousemove", resizing);
        window.removeEventListener("mouseup", endResize);
    }
    window.document.documentElement.classList.add("x-resizing-column");
    window.addEventListener("mouseup", endResize);
    window.addEventListener("mousemove", resizing);
});
function resetWidth() {
    for (const th of document.querySelectorAll("th[data-original-style]")) {
        th.style.width = th.dataset.originalStyle;
    }
}