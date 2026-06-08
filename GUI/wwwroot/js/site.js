/**
 * Premium Toast Notifications System
 * Handles elegant slide-in toasts with auto-dismiss timers and automatic clearance of stale notifications.
 */

function showPremiumToast(message, type = 'success') {
    let container = document.getElementById('premiumToastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'premiumToastContainer';
        container.className = 'premium-toast-container';
        document.body.appendChild(container);
    }

    // Dismiss any existing toasts to guarantee only one is displayed at a time
    const existingToasts = container.querySelectorAll('.premium-toast');
    existingToasts.forEach(toast => {
        toast.classList.remove('show');
        if (toast.dataset.timeoutId) {
            clearTimeout(parseInt(toast.dataset.timeoutId, 10));
        }
        setTimeout(() => toast.remove(), 400);
    });

    const toast = document.createElement('div');
    toast.className = `premium-toast ${type}`;
    
    const iconSvg = type === 'success' 
        ? `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>`
        : `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>`;

    const title = type === 'success' ? 'Thành công' : 'Lỗi';

    toast.innerHTML = `
        <div class="premium-toast-icon">${iconSvg}</div>
        <div class="premium-toast-body">
            <div class="premium-toast-title">${title}</div>
            <div class="premium-toast-message">${message}</div>
        </div>
        <button type="button" class="premium-toast-close" onclick="closePremiumToast(this.parentElement)">&times;</button>
        <div class="premium-toast-progress"></div>
    `;

    container.appendChild(toast);

    // Force browser reflow to enable CSS transition
    toast.offsetHeight;

    // Slide-in and animate progress bar scaling down to zero over 5 seconds
    toast.classList.add('show');
    const progress = toast.querySelector('.premium-toast-progress');
    progress.style.transition = 'transform 5000ms linear';
    progress.style.transform = 'scaleX(0)';

    // Set auto-dismiss timer
    const timeoutId = setTimeout(() => {
        closePremiumToast(toast);
    }, 5000);

    toast.dataset.timeoutId = timeoutId;
}

function closePremiumToast(toast) {
    if (!toast) return;
    toast.classList.remove('show');
    if (toast.dataset.timeoutId) {
        clearTimeout(parseInt(toast.dataset.timeoutId, 10));
    }
    setTimeout(() => toast.remove(), 400);
}
