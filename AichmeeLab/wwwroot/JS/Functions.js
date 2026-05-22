// Infinite Scroll
window.initScrollObserver = (dotNetHelper, elementId) => {
    const options = {
        root: null, // Use the browser viewport
        rootMargin: '200px', // Trigger 200px before the user hits the bottom
        threshold: 0.1
    };

    const observer = new IntersectionObserver((entries) => {
        // If the anchor div is visible on screen
        if (entries[0].isIntersecting) {
            // Call the [JSInvokable] method in Home.razor
            dotNetHelper.invokeMethodAsync('LoadMorePostsAsync');
        }
    }, options);

    const el = document.getElementById(elementId);
    if (el) {
        observer.observe(el);
    }

    
};

//Detect Phone
window.isMobileDevice = () => {
    return window.innerWidth < 641; 
};

// Sidebar Swipe Interop
window.initializeSwipe = (dotNetHelper) => {
    let touchStartX = 0;
    let touchStartY = 0;

    document.addEventListener('touchstart', (e) => {
        touchStartX = e.changedTouches[0].screenX;
        touchStartY = e.changedTouches[0].screenY;
    }, { passive: true });

    document.addEventListener('touchend', (e) => {
        const touchEndX = e.changedTouches[0].screenX;
        const touchEndY = e.changedTouches[0].screenY;
        
        const deltaX = touchEndX - touchStartX;
        const deltaY = Math.abs(touchEndY - touchStartY);

        if (Math.abs(deltaX) > 70 && deltaY < 45) {
            // Swipe Right from edge (Open)
            if (deltaX > 0 && touchStartX < 220) {
                dotNetHelper.invokeMethodAsync('SideBarToggle');
            } 
            // Swipe Left anywhere (Close)
            else if (deltaX < 0) {
                dotNetHelper.invokeMethodAsync('SideBarToggle');
            }
        }
    }, { passive: true });
};