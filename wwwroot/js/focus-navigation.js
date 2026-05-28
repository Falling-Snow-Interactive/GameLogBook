window.gameLogBookFocus = (() => {
    const modalObservers = new WeakMap();
    let globalHandlerAttached = false;

    const focusableSelector = [
        "a[href]",
        "area[href]",
        "button:not([disabled])",
        "input:not([disabled]):not([type='hidden'])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "summary",
        "[contenteditable='true']",
        "[tabindex]:not([tabindex='-1'])"
    ].join(",");

    const scopedFormSelector = [
        "[data-focus-scope='modal']",
        "form",
        ".popup",
        ".game-details-form",
        ".platform-details-form",
        ".playthrough-details-form",
        ".details-form",
        ".popup-form",
        ".steamgriddb-search-panel"
    ].join(",");

    function isVisible(element) {
        const styles = window.getComputedStyle(element);

        return styles.visibility !== "hidden"
            && styles.display !== "none"
            && element.getClientRects().length > 0;
    }

    function isFocusable(element) {
        const ariaHidden = element.getAttribute("aria-hidden");

        return element.matches(focusableSelector)
            && element.tabIndex >= 0
            && !element.hasAttribute("disabled")
            && ariaHidden !== "true"
            && isVisible(element);
    }

    function getFocusableElements(scope) {
        if (!scope) {
            return [];
        }

        const elements = Array.from(scope.querySelectorAll(focusableSelector))
            .filter(isFocusable);

        return elements.sort((first, second) => {
            const firstIsClose = first.classList.contains("close-button") ? 1 : 0;
            const secondIsClose = second.classList.contains("close-button") ? 1 : 0;

            if (firstIsClose !== secondIsClose) {
                return firstIsClose - secondIsClose;
            }

            return 0;
        });
    }

    function getFirstInput(scope) {
        const fields = getFocusableElements(scope)
            .filter(element => element.matches("input:not([type='file']), select, textarea, [contenteditable='true']"));

        return fields[0] ?? getFocusableElements(scope)[0] ?? null;
    }

    function focusElement(element) {
        if (!element) {
            return false;
        }

        element.focus({ preventScroll: true });
        element.scrollIntoView({ block: "nearest", inline: "nearest" });
        return true;
    }

    function handleTab(event, scope, trapFocus) {
        const focusableElements = getFocusableElements(scope);

        if (focusableElements.length === 0) {
            event.preventDefault();
            return;
        }

        const activeElement = document.activeElement;
        const currentIndex = focusableElements.indexOf(activeElement);
        const nextIndex = currentIndex === -1
            ? 0
            : currentIndex + (event.shiftKey ? -1 : 1);

        if (!trapFocus && (nextIndex < 0 || nextIndex >= focusableElements.length)) {
            return;
        }

        event.preventDefault();

        const targetIndex = trapFocus
            ? (nextIndex + focusableElements.length) % focusableElements.length
            : nextIndex;

        focusElement(focusableElements[targetIndex]);
    }

    function getActiveScope() {
        const activeElement = document.activeElement;

        if (!activeElement || activeElement === document.body) {
            return null;
        }

        return activeElement.closest("[data-focus-scope='modal']")
            ?? activeElement.closest(scopedFormSelector);
    }

    function attachGlobalHandler() {
        if (globalHandlerAttached) {
            return;
        }

        document.addEventListener("keydown", event => {
            if (event.key !== "Tab") {
                return;
            }

            const scope = getActiveScope();

            if (!scope) {
                return;
            }

            handleTab(event, scope, scope.matches("[data-focus-scope='modal'], .popup"));
        }, true);

        globalHandlerAttached = true;
    }

    function observeModal(element) {
        if (!element) {
            return;
        }

        attachGlobalHandler();
        cleanupModal(element, false);
        const previousActiveElement = document.activeElement instanceof HTMLElement
            ? document.activeElement
            : null;

        const focusInitialElement = () => {
            if (element.contains(document.activeElement)) {
                return;
            }

            focusElement(getFirstInput(element));
        };

        window.requestAnimationFrame(focusInitialElement);
        window.setTimeout(focusInitialElement, 0);

        modalObservers.set(element, previousActiveElement);
    }

    function cleanupModal(element, restoreFocus = true) {
        if (!element || !modalObservers.has(element)) {
            return;
        }

        const previousActiveElement = modalObservers.get(element);
        modalObservers.delete(element);

        if (restoreFocus && previousActiveElement?.isConnected) {
            focusElement(previousActiveElement);
        }
    }

    attachGlobalHandler();

    return {
        observeModal,
        cleanupModal
    };
})();
