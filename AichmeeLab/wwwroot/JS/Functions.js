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