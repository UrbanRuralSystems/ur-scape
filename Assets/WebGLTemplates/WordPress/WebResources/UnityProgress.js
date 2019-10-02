function UnityProgress(gameInstance, progress) {
    if (!gameInstance.Module)
        return;
    if (!gameInstance.progress) {
        gameInstance.progress = document.createElement("div");
        gameInstance.progress.id = "progressFrame";
        gameInstance.progress.className = "progress";
        gameInstance.progress.full = document.createElement("div");
        gameInstance.progress.full.id = 'progressBar';
        gameInstance.progress.full.className = "full";
        gameInstance.progress.appendChild(gameInstance.progress.full);
        gameInstance.progress.message = document.createElement("div");
        gameInstance.progress.message.id = "messageArea";
		gameInstance.progress.message.innerHTML = "Loading ...";
        gameInstance.progress.appendChild(gameInstance.progress.message);
        gameInstance.container.appendChild(gameInstance.progress);
    }
	if (progress === "complete") {
		gameInstance.progress.style.display = "none";
		if (typeof updateCanvas === "function")
			updateCanvas();
		return;
	}
    gameInstance.progress.full.style.width = (100 * progress) + "%";
}
