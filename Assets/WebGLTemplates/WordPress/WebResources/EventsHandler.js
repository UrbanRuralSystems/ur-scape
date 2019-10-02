var FullscreenButtonHeight = 70;
var gameWidthToHeight = 0.5625;	// 16:9

function initCanvas() {
	var game = document.getElementById('gameContainer');
	gameWidthToHeight = parseInt(game.style.height) / parseInt(game.style.width);
    if (isNaN(gameWidthToHeight))
        gameWidthToHeight = screen.height / screen.width;

	// make the parent container visible (should be initially invisible in CSS)
	var parentContainer = document.getElementById('gameAndButtonContainer');
	parentContainer.style.display = "block";

	updateCanvas();
}
  
function updateCanvas() {
	var parentContainer = document.getElementById('gameAndButtonContainer');
	var game = document.getElementById('gameContainer');
    
	var width = parentContainer.clientWidth;
	if (game.clientWidth !== width)
	{
		var height = width * gameWidthToHeight;

		game.style.width = width + 'px';
		game.style.height = height + 'px';
	}
}

function ToFullScreen() {
	window.removeEventListener('resize', updateCanvas, false);  
	setTimeout(function(){ window.addEventListener('resize', updateCanvas, false); }, 1000);
	gameInstance.SetFullscreen(1);
}

window.addEventListener('load', initCanvas, false);
window.addEventListener('resize', updateCanvas, false);
