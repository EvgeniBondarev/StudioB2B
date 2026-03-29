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

