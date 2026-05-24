window.gameLogBookImageField = (() => {
    const observers = new WeakMap();

    function updatePreviewSize(widget, controls) {
        const styles = window.getComputedStyle(widget);
        const preview = widget.querySelector(".image-field-preview");
        const previewStyles = preview ? window.getComputedStyle(preview) : null;
        const aspectWidth = Number.parseFloat(styles.getPropertyValue("--image-field-preview-aspect-width")) || 2;
        const aspectHeight = Number.parseFloat(styles.getPropertyValue("--image-field-preview-aspect-height")) || 3;
        const maxWidth = Number.parseFloat(previewStyles?.maxWidth);
        const maxHeight = Number.parseFloat(previewStyles?.maxHeight);
        const ratio = aspectWidth / aspectHeight;
        const isWide = ratio > 1;
        let height = isWide
            ? widget.getBoundingClientRect().width / ratio
            : controls.getBoundingClientRect().height;

        if (!height) {
            return;
        }

        if (maxHeight > 0) {
            height = Math.min(height, maxHeight);
        }

        let width = height * ratio;

        if (!isWide && maxWidth > 0 && width > maxWidth) {
            width = maxWidth;
            height = width / ratio;
        }

        widget.style.setProperty("--image-field-preview-height", `${height}px`);
        widget.style.setProperty("--image-field-preview-width", `${width}px`);
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
        observer.observe(widget);
        observer.observe(controls);
        observers.set(widget, observer);

        update();
    }

    return {
        observe,
        cleanup
    };
})();
