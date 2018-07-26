mergeInto(LibraryManager.library, {

    GetWebUrl: function () {
        var returnStr = GetDataPath();
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
	
	Log: function(msg)        { console.log(Pointer_stringify(msg)); },
	LogWarning: function(msg) { console.warn(Pointer_stringify(msg)); },
	LogError: function(msg)   { console.error(Pointer_stringify(msg)); },

});
