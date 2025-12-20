function scrollToBottom() {
    var chatMessages = document.getElementsByClassName("chat-messages")[0];
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}