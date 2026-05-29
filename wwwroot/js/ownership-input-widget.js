window.gameLogBookOwnershipInput = (() => {
    const previousRectsByGrid = new WeakMap();

    function getCards(grid) {
        return Array.from(grid.querySelectorAll("[data-ownership-platform-id]"));
    }

    function getRects(grid) {
        const rects = new Map();

        for (const card of getCards(grid)) {
            rects.set(card.dataset.ownershipPlatformId, card.getBoundingClientRect());
        }

        return rects;
    }

    function prepare(grid) {
        if (!grid) {
            return;
        }

        previousRectsByGrid.set(grid, getRects(grid));
    }

    function animate(grid) {
        if (!grid || window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
            previousRectsByGrid.delete(grid);
            return;
        }

        const previousRects = previousRectsByGrid.get(grid);
        previousRectsByGrid.delete(grid);

        if (!previousRects) {
            return;
        }

        for (const card of getCards(grid)) {
            const previousRect = previousRects.get(card.dataset.ownershipPlatformId);

            if (!previousRect) {
                continue;
            }

            const nextRect = card.getBoundingClientRect();
            const deltaX = previousRect.left - nextRect.left;
            const deltaY = previousRect.top - nextRect.top;

            if (Math.abs(deltaX) < 0.5 && Math.abs(deltaY) < 0.5) {
                continue;
            }

            card.animate(
                [
                    { transform: `translate(${deltaX}px, ${deltaY}px)` },
                    { transform: "translate(0, 0)" }
                ],
                {
                    duration: 260,
                    easing: "cubic-bezier(0.2, 0, 0, 1)"
                }
            );
        }
    }

    return {
        prepare,
        animate
    };
})();
