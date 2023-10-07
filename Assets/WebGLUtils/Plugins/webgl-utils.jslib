mergeInto(LibraryManager.library, {
    SetCursorToWait: function () {
        document.getElementById("unity-canvas").style.cursor = "wait";
    },
    ResetCursor: function () {
        document.getElementById("unity-canvas").style.cursor = "default";
    },
    DownloadFile : function(byteArrayPointer, byteArrayLength, fileNamePointer, fileTypePointer) {
        var byteArray = new Uint8Array(Module.HEAPU8.buffer, byteArrayPointer, byteArrayLength);
        var fileName = Pointer_stringify(fileNamePointer);
        var fileType = Pointer_stringify(fileTypePointer);

        var blob = new Blob([byteArray], { type: fileType });
        var url = URL.createObjectURL(blob);

        var a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.style.display = 'none';

        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        URL.revokeObjectURL(url);
    },
    DownloadFileStream: function(streamPointer, fileNamePointer, fileTypePointer) {
        var stream = Module.FS_stream_manager.getStreamFromPointer(streamPointer);
        if (!stream) {
            console.error("Stream not found");
            return;
        }

        var fileName = Pointer_stringify(fileNamePointer);
        var fileType = Pointer_stringify(fileTypePointer);

        var blob = new Blob([new Uint8Array(stream.contents, 0, stream.contents.length)], { type: fileType });
        var url = URL.createObjectURL(blob);

        var a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.style.display = 'none';

        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        URL.revokeObjectURL(url);
    }
});