var buildUrl = "{{{ BASE_URL }}}/Build";
var config = {
	dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
	frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
	codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
#if MEMORY_FILENAME
	memoryUrl: buildUrl + "/{{{ MEMORY_FILENAME }}}",
#endif
#if SYMBOLS_FILENAME
	symbolsUrl: buildUrl + "/{{{ SYMBOLS_FILENAME }}}",
#endif
	streamingAssetsUrl: "StreamingAssets",
	companyName: "{{{ COMPANY_NAME }}}",
	productName: "{{{ PRODUCT_NAME }}}",
	productVersion: "{{{ PRODUCT_VERSION }}}",
};


var unityInstance = null;
var script = document.createElement("script");
script.src = buildUrl + "/{{{ LOADER_FILENAME }}}";
script.onload = () => {
	var canvas = document.getElementById("unity-canvas");
	var progressBar = document.getElementById("unity-progress-bar");
	createUnityInstance(canvas, config, (progress) => {
		progressBar.style.width = 100 * progress + "%";
	}).then((instance) => {
		unityInstance = instance;

		var loadingBar = document.getElementById('unity-loading-bar');
		loadingBar.style.display = "none";

		updateCanvas();

		var fullscreenButton = document.getElementById('unity-fullscreen-button');
		fullscreenButton.onclick = () => {
			unityInstance.SetFullscreen(1);
		};

		var fullscreenLabel = document.getElementById('unity-fullscreen-label');
		fullscreenLabel.style.visibility = null;
		fullscreenButton.style.visibility = null;

	}).catch((message) => {
		alert(message);
	});
};
document.body.appendChild(script);
	
function GetDataPath() { return "{{{ BASE_URL }}}/"; }

function ToFullScreen() {
	window.removeEventListener('resize', updateCanvas, false);  
	setTimeout(function(){ window.addEventListener('resize', updateCanvas, false); }, 1000);
	unityInstance.SetFullscreen(1);
}

var width2height = 0.5625;	// 16:9

window.addEventListener('load', initCanvas, false);
function initCanvas() {
	var container = document.getElementById('unity-container');
	var w2h = parseInt(container.style.height) / parseInt(container.style.width);
    if (isNaN(w2h))
        w2h = screen.height / screen.width;
	if (!isNaN(w2h))
		width2height = w2h;

	// Make the parent container visible (should be initially invisible in CSS)
	container.style.display = "block";

	updateCanvas();
}

window.addEventListener('resize', updateCanvas, false);
function updateCanvas() {
	var container = document.getElementById('unity-container');
	var canvas = document.getElementById('unity-canvas');
    
	var width = container.clientWidth;
	var height = Math.round(width * width2height);
	if (canvas.clientHeight !== height)
	{
		canvas.style.width = width + 'px';
		canvas.style.height = height + 'px';
	}
}
