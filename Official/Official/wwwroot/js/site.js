function scrollToBottom() {
    const chatMessages = document.getElementById("chatMessages");
    if (!chatMessages) {
        console.warn("No #chatMessages element found");
        return;
    }
    //console.log("Chat element found");
    chatMessages.scrollTop = chatMessages.scrollHeight;
}
