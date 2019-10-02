mergeInto(LibraryManager.library, {

    GetWebUrl: function () {
        var returnStr = GetDataPath();
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
	
	GoFullScreen: function() {
		ToFullScreen();
	},
	
	OpenUrlInTab: function(urlPtr)
    {
    	var url = UTF8ToString(urlPtr);
		
		function onMouseUpOpenUrl() {
			window.open(url);
			document.removeEventListener('mouseup', onMouseUpOpenUrl);
		}
		document.addEventListener('mouseup', onMouseUpOpenUrl);
    },
	
	Log: function(msg)        { console.log(UTF8ToString(msg)); },
	LogWarning: function(msg) { console.warn(UTF8ToString(msg)); },
	LogError: function(msg)   { console.error(UTF8ToString(msg)); },

});

// Downloader Plugin
mergeInto(LibraryManager.library, {
	DownloadFile: function(filenamePtr, array, size) {
		var filename = UTF8ToString(filenamePtr);

		var bytes = new Uint8Array(size);
		for (var i = 0; i < size; i++)
		{
			bytes[i] = HEAPU8[array + i];
		}

		var blob = new Blob([bytes], {type: 'application/zip'});
		var link = document.createElement('a');
		link.download = filename;
		if (window.webkitURL != null)
		{
			link.href = window.webkitURL.createObjectURL(blob);
			link.click();
			window.webkitURL.revokeObjectURL(link.href);
		}
		else
		{
			link.href = window.URL.createObjectURL(blob);
			link.click();
			window.URL.revokeObjectURL(link.href);
		}
		link.remove();
	}
});

