chrome.sidePanel.setPanelBehavior({
	openPanelOnActionClick: true,
});

chrome.commands.onCommand.addListener(async (command) => {
	console.log(command);
});
