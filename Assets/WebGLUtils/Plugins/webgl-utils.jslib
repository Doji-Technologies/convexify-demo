mergeInto(LibraryManager.library, {
    SetCursorToWait: function () {
        document.getElementById("unity-canvas").style.cursor = "wait";
    },
    ResetCursor: function () {
        document.getElementById("unity-canvas").style.cursor = "default";
    },
});