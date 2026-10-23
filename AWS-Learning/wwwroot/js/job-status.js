(() => {
    const panel = document.getElementById('job-panel');
    const toggle = document.getElementById('auto-refresh');
    const message = document.getElementById('refresh-message');
    if (!panel || !toggle || !message) return;
    let running = false;
    setInterval(async () => {
        if (!toggle.checked || running || document.hidden) return;
        running = true;
        try {
            const response = await fetch(panel.dataset.statusUrl, {
                headers: { Accept: 'application/json' }, cache: 'no-store'
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data.detail || 'Status could not be retrieved.');
            if (data.status !== panel.dataset.initialStatus) {
                window.location.reload();
                return;
            }
            message.textContent = `Checked at ${new Date().toLocaleTimeString()} · ${data.status}`;
        } catch (error) {
            message.textContent = error.message || 'Status check failed. Try refreshing manually.';
        } finally {
            running = false;
        }
    }, 5000);
})();
