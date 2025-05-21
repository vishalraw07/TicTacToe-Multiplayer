mergeInto(LibraryManager.library, {
    CopyToClipboard: function(text) {
        var textarea = document.createElement("textarea");
        textarea.value = Pointer_stringify(text);
        document.body.appendChild(textarea);
        textarea.select();
        try {
            document.execCommand('copy');
            console.log('Text copied to clipboard');
        } catch (err) {
            console.error('Failed to copy: ', err);
        }
        document.body.removeChild(textarea);
    }
});