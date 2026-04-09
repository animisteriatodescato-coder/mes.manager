// AI Assistant Panel — scroll helper
window.AiAssistant = {
    scrollToBottom: function (elementId) {
        var el = document.getElementById(elementId);
        if (el) el.scrollTop = el.scrollHeight;
    }
};
