function scrollToBottom() {
    const chatMessages = document.getElementById("chatMessages");
    if (!chatMessages) {
        console.warn("No #chatMessages element found");
        return;
    }
    chatMessages.scrollTop = chatMessages.scrollHeight;
}
