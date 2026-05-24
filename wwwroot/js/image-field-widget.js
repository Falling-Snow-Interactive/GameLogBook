window.gameLogBookImageField = (() => {
    const observers = new WeakMap();

    function updatePreviewSize(widget, controls) {
        const height = controls.getBoundingClientRect().height;

        if (!height) {
            return;
        }

        widget.style.setProperty("--image-field-preview-height", `${height}px`);
        widget.style.setProperty("--image-field-preview-width", `${height * 2 / 3}px`);
    }

    function cleanup(widget) {
        const observer = observers.get(widget);

        if (!observer) {
            return;
        }

        observer.disconnect();
        observers.delete(widget);
    }

    function observe(widget, controls) {
        if (!widget || !controls) {
            return;
        }

        cleanup(widget);

        const update = () => {
            window.requestAnimationFrame(() => updatePreviewSize(widget, controls));
        };

        const observer = new ResizeObserver(update);
        observer.observe(controls);
        observers.set(widget, observer);

        update();
    }

    return {
        observe,
        cleanup
    };
})();
