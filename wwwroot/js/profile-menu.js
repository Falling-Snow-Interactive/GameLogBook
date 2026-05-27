window.gameLogBookProfileMenu = (() => {
    const handlers = new WeakMap();

    function cleanup(element) {
        const handler = handlers.get(element);

        if (!handler) {
            return;
        }

        document.removeEventListener("pointerdown", handler, true);
        handlers.delete(element);
    }

    function observeClickAway(element, dotNetReference) {
        if (!element || !dotNetReference) {
            return;
        }

        cleanup(element);

        const handler = event => {
            if (element.contains(event.target)) {
                return;
            }

            dotNetReference.invokeMethodAsync("CloseProfileMenu");
        };

        document.addEventListener("pointerdown", handler, true);
        handlers.set(element, handler);
    }

    return {
        observeClickAway,
        cleanup
    };
})();
