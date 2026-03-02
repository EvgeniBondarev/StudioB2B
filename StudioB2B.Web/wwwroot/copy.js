window.studioCopyText = async function (text) {
    try {
        if (!navigator.clipboard) {
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.opacity = '0';
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
        } else {
            await navigator.clipboard.writeText(text);
        }
        return true;
    } catch (e) {
        console.error('Failed to copy text', e);
        return false;
    }
};

