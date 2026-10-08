mergeInto(LibraryManager.library, {
    ConvexifyOpenFilePicker: function (gameObjectNamePtr, methodNamePtr, acceptPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        var input = document.getElementById("convexify-file-input");
        if (!input) {
            input = document.createElement("input");
            input.type = "file";
            input.id = "convexify-file-input";
            input.style.display = "none";
            document.body.appendChild(input);
        }
        input.accept = UTF8ToString(acceptPtr);
        input.onchange = function () {
            var file = input.files && input.files[0];
            input.value = "";
            if (!file) return;
            var info = { url: URL.createObjectURL(file), fileName: file.name, size: String(file.size) };
            SendMessage(gameObjectName, methodName, JSON.stringify(info));
        };
        input.click();
    },

    ConvexifyDownloadFile: function (bytesPtr, length, fileNamePtr, mimeTypePtr) {
        var bytes = HEAPU8.slice(bytesPtr, bytesPtr + length);
        var blob = new Blob([bytes], { type: UTF8ToString(mimeTypePtr) });
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = UTF8ToString(fileNamePtr);
        a.style.display = "none";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }
});
