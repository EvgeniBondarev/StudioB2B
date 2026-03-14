window.transactionCanvas = {

    // Current transform state (kept in sync with C# via setTransform)
    _zoom: 1.0,
    _panX: 0.0,
    _panY: 0.0,

    /** Called from C# whenever zoom/pan change so drag calculations stay correct. */
    setTransform: function (zoom, panX, panY) {
        this._zoom = zoom;
        this._panX = panX;
        this._panY = panY;
    },

    /** Attach wheel-zoom listener to the outer container. */
    initWheelZoom: function (outerCanvasId, dotNetRef) {
        const outer = document.getElementById(outerCanvasId);
        if (!outer) return;
        // Remove previous listener if any (component re-renders)
        if (outer._tcWheelHandler) outer.removeEventListener('wheel', outer._tcWheelHandler);
        outer._tcWheelHandler = (e) => {
            e.preventDefault();
            const rect = outer.getBoundingClientRect();
            dotNetRef.invokeMethodAsync('OnCanvasWheel',
                e.clientX - rect.left,
                e.clientY - rect.top,
                e.deltaY);
        };
        outer.addEventListener('wheel', outer._tcWheelHandler, { passive: false });
    },

    /**
     * Enable left-click drag to pan the canvas.
     * Panning is suppressed when the click target is a node, port, or button.
     * The inner div transform is updated directly in the DOM for smooth 60fps panning;
     * Blazor is notified only on mouse-up (OnPanEnd).
     */
    initPan: function (outerCanvasId, innerCanvasId, dotNetRef) {
        const outer = document.getElementById(outerCanvasId);
        if (!outer) return;

        // Default cursor signals the canvas is draggable
        outer.style.cursor = 'grab';

        if (outer._tcPanHandler) outer.removeEventListener('mousedown', outer._tcPanHandler);
        outer._tcPanHandler = (e) => {
            if (e.button !== 0) return;
            // Do not pan when interacting with nodes, ports, or buttons
            if (e.target.closest('[data-status-id]')) return;
            if (e.target.closest('button')) return;

            e.preventDefault();

            const startX    = e.clientX;
            const startY    = e.clientY;
            const startPanX = window.transactionCanvas._panX;
            const startPanY = window.transactionCanvas._panY;

            outer.style.cursor = 'grabbing';

            const onMove = (ev) => {
                const newPanX = startPanX + (ev.clientX - startX);
                const newPanY = startPanY + (ev.clientY - startY);
                window.transactionCanvas._panX = newPanX;
                window.transactionCanvas._panY = newPanY;
                // Update transform directly — no Blazor roundtrip
                const inner = document.getElementById(innerCanvasId);
                if (inner) {
                    const z = window.transactionCanvas._zoom;
                    inner.style.transform =
                        `translate(${newPanX}px,${newPanY}px) scale(${z})`;
                }
            };

            const onUp = () => {
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup',   onUp);
                outer.style.cursor = 'grab';
                // Sync final pan position back to Blazor
                dotNetRef.invokeMethodAsync('OnPanEnd',
                    window.transactionCanvas._panX,
                    window.transactionCanvas._panY);
            };

            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup',   onUp);
        };
        outer.addEventListener('mousedown', outer._tcPanHandler);
    },

    initNodeDrag: function (nodeId, statusId, initialX, initialY, dotNetRef) {
        const el = document.getElementById(nodeId);
        if (!el) return;

        el.style.left = initialX + 'px';
        el.style.top  = initialY + 'px';

        const handle = el.querySelector('.tc-node-header') || el;

        handle.addEventListener('mousedown', (e) => {
            if (e.button !== 0) return;
            if (e.target.closest('.tc-port')) return;
            e.preventDefault();
            e.stopPropagation();

            const startX   = e.clientX;
            const startY   = e.clientY;
            const startL   = parseFloat(el.style.left) || 0;
            const startT   = parseFloat(el.style.top)  || 0;
            const origZ    = el.style.zIndex;
            el.style.zIndex = '50';

            const onMove = (ev) => {
                const z = window.transactionCanvas._zoom;
                el.style.left = Math.max(0, startL + (ev.clientX - startX) / z) + 'px';
                el.style.top  = Math.max(0, startT + (ev.clientY - startY) / z) + 'px';
            };
            const onUp = (ev) => {
                el.style.zIndex = origZ || '10';
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup',   onUp);
                dotNetRef.invokeMethodAsync('OnNodeMoved', statusId,
                    parseFloat(el.style.left),
                    parseFloat(el.style.top));
            };
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup',   onUp);
        });
    },

    startConnect: function (sourceStatusId, nodeId, outerCanvasId, svgId, dotNetRef) {
        const nodeEl = document.getElementById(nodeId);
        const outer  = document.getElementById(outerCanvasId); // outer fixed container
        const svg    = document.getElementById(svgId);
        if (!nodeEl || !outer || !svg) return;

        const outerRect = outer.getBoundingClientRect();
        const z  = this._zoom;
        const px = this._panX;
        const py = this._panY;

        // Port start in logical (inner) space
        const startX = (parseFloat(nodeEl.style.left) || 0) + nodeEl.offsetWidth;
        const startY = (parseFloat(nodeEl.style.top)  || 0) + nodeEl.offsetHeight / 2;

        // Get or create temp path (lives inside the scaled SVG → logical coords)
        let tmp = document.getElementById('tc-temp-path');
        if (!tmp) {
            tmp = document.createElementNS('http://www.w3.org/2000/svg', 'path');
            tmp.id = 'tc-temp-path';
            tmp.setAttribute('stroke', '#6366f1');
            tmp.setAttribute('stroke-width', '2');
            tmp.setAttribute('stroke-dasharray', '7,4');
            tmp.setAttribute('fill', 'none');
            tmp.setAttribute('marker-end', 'url(#tc-arrow-head-default)');
            svg.appendChild(tmp);
        }
        tmp.style.display = '';

        // Convert screen → logical coords
        const toLogical = (cx, cy) => ({
            x: (cx - outerRect.left - px) / z,
            y: (cy - outerRect.top  - py) / z
        });

        const bezier = (x1, y1, x2, y2) => {
            const cx = Math.max(60, Math.abs(x2 - x1) / 2);
            return `M${x1},${y1} C${x1+cx},${y1} ${x2-cx},${y2} ${x2},${y2}`;
        };

        const onMove = (e) => {
            const p = toLogical(e.clientX, e.clientY);
            tmp.setAttribute('d', bezier(startX, startY, p.x, p.y));
        };
        const onUp = (e) => {
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup',   onUp);
            tmp.setAttribute('d', '');
            tmp.style.display = 'none';

            const elements = document.elementsFromPoint(e.clientX, e.clientY);
            let targetId = null;
            for (const el of elements) {
                const node = el.closest ? el.closest('[data-status-id]') : null;
                if (node && node.dataset.statusId && node.dataset.statusId !== sourceStatusId) {
                    targetId = node.dataset.statusId;
                    break;
                }
            }
            if (targetId) dotNetRef.invokeMethodAsync('OnConnectionMade', sourceStatusId, targetId);
        };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup',   onUp);
    },

    setNodePosition: function (nodeId, x, y) {
        const el = document.getElementById(nodeId);
        if (el) { el.style.left = x + 'px'; el.style.top = y + 'px'; }
    },

    savePositions: function (key, json) {
        try { localStorage.setItem(key, json); } catch (e) { }
    },

    loadPositions: function (key) {
        try { return localStorage.getItem(key) || '{}'; } catch (e) { return '{}'; }
    }
};
