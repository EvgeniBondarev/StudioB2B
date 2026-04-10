window.taskBoardInfiniteScroll = {
    _observer: null,

    init: function (sentinel, dotnetHelper) {
        if (this._observer) {
            this._observer.disconnect();
        }
        if (!sentinel) return;

        this._observer = new IntersectionObserver(function (entries) {
            if (entries[0].isIntersecting) {
                dotnetHelper.invokeMethodAsync('LoadMoreDoneAsync');
            }
        }, { threshold: 0.1 });

        this._observer.observe(sentinel);
    },

    dispose: function () {
        if (this._observer) {
            this._observer.disconnect();
            this._observer = null;
        }
    }
};

window.taskBoardEsc = {
    _handler: null,

    init: function (dotnetHelper) {
        this.dispose();
        this._handler = function (e) {
            if (e.key === 'Escape') {
                dotnetHelper.invokeMethodAsync('OnEscapeKeyAsync');
            }
        };
        document.addEventListener('keydown', this._handler);
    },

    dispose: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
    }
};

window.scrollChatOverlayToBottom = function () {
    const el = document.querySelector('.task-detail-overlay .chat-messages-area');
    if (el) el.scrollTop = el.scrollHeight;
};

