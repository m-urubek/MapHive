// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Store the original console.error function
const originalConsoleError = console.error;

// Function to display a user-friendly error message
function showUserFriendlyJavaScriptError(message, level = 'warning') {
    // Remove any existing custom error message
    const existingError = document.getElementById('userFriendlyJavaScriptError');
    if (existingError) {
        existingError.remove();
    }

    const alertClass = `alert-${level}`; // e.g., alert-warning for orange

    const errorWrapper = document.createElement('div');
    errorWrapper.id = 'userFriendlyJavaScriptError';
    errorWrapper.className = 'user-friendly-message'; // Use same class for positioning
    errorWrapper.style.position = 'fixed';
    errorWrapper.style.top = '0';
    errorWrapper.style.left = '0';
    errorWrapper.style.width = '100%';
    errorWrapper.style.zIndex = '10000'; // Ensure it's on top
    errorWrapper.style.padding = '10px';


    const alertDiv = document.createElement('div');
    alertDiv.className = `alert ${alertClass} alert-dismissible fade show`;
    alertDiv.setAttribute('role', 'alert');
    alertDiv.style.maxWidth = '800px';
    alertDiv.style.margin = '0 auto';
    alertDiv.style.boxShadow = '0 4px 8px rgba(0,0,0,0.1)';

    const messageSpan = document.createElement('span');
    // Sanitize message slightly - in a real app, use a proper sanitizer if message can contain HTML
    const tempDiv = document.createElement('div');
    tempDiv.textContent = message;
    messageSpan.innerHTML = tempDiv.innerHTML; // Basic way to display text that might look like HTML

    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'btn-close';
    closeButton.setAttribute('data-bs-dismiss', 'alert');
    closeButton.setAttribute('aria-label', 'Close');

    alertDiv.appendChild(messageSpan);
    alertDiv.appendChild(closeButton);
    errorWrapper.appendChild(alertDiv);
    document.body.insertBefore(errorWrapper, document.body.firstChild); // Insert at the top of the body

    // Auto-hide the message after 15 seconds
    setTimeout(function() {
        if (errorWrapper && errorWrapper.parentNode) {
            const bsAlert = bootstrap.Alert.getInstance(alertDiv) || new bootstrap.Alert(alertDiv);
            if (bsAlert) {
                bsAlert.close();
            } else { // Fallback if bootstrap.Alert is not available or fails
                 errorWrapper.remove();
            }
        }
    }, 15000);
}

// Override console.error
console.error = function () {
    // Call the original console.error so the message still appears in the console
    originalConsoleError.apply(console, arguments);

    // Show user-friendly message
    const message = Array.from(arguments).map(arg => 
        (typeof arg === 'object' && arg !== null) ? JSON.stringify(arg) : String(arg)
    ).join(' ');
    showUserFriendlyJavaScriptError("An error occurred: " + message, 'warning');
};

// You can also catch unhandled promise rejections
window.addEventListener('unhandledrejection', function (event) {
    const reason = (event.reason instanceof Error) ? event.reason.message : String(event.reason);
    const errorMessage = 'Unhandled promise rejection: ' + reason;
    
    originalConsoleError(errorMessage, event); // Log to console first
    showUserFriendlyJavaScriptError(errorMessage, 'warning'); // Then show UI message
});

// And global errors not caught by try/catch
window.onerror = function (message, source, lineno, colno, error) {
    const fullMessage = `Global error: ${message} at ${source} line ${lineno}:${colno}`;
    // Log to console first
    originalConsoleError(fullMessage, error ? error : ''); 

    // Show user-friendly message
    showUserFriendlyJavaScriptError(fullMessage, 'warning');
    
    return false; // Prevent default browser error handling
};

// Initialize theme on page load
;(function() {
  const html = document.documentElement;
  const isAuthenticated = document.body.dataset.authenticated === 'true';
  if (isAuthenticated) {
    // Fetch user preference from server
    fetch('/Account/GetDarkModePreference', { credentials: 'same-origin' })
      .then(res => res.json())
      .then(data => {
        if (data.enabled) {
          html.classList.add('dark-mode');
          html.classList.remove('light-mode');
        } else {
          html.classList.remove('dark-mode');
          html.classList.add('light-mode');
        }
        localStorage.setItem('theme', data.enabled ? 'dark' : 'light');
      });
  } else {
    // Not authenticated: respect stored preference or system
    const stored = localStorage.getItem('theme');
    if (stored === 'dark') {
      html.classList.add('dark-mode');
      html.classList.remove('light-mode');
    } else if (stored === 'light') {
      html.classList.remove('dark-mode');
      html.classList.add('light-mode');
    }
    // else default to system preference via CSS media query
  }

  document.addEventListener('DOMContentLoaded', function() {
    const toggle = document.getElementById('theme-toggle');
    if (!toggle) return;
    // Set initial toggle icon based on current theme
    toggle.textContent = html.classList.contains('dark-mode') ? '☀️' : '🌙';
    // Center the toggle icon
    toggle.classList.add('d-flex', 'align-items-center', 'justify-content-center');
    toggle.addEventListener('click', function() {
      // Update toggle icon after theme change
      const isDark = html.classList.toggle('dark-mode');
      html.classList.toggle('light-mode', !isDark);
      // Correctly update toggle icon after theme change
      toggle.textContent = isDark ? '☀️' : '🌙';
      localStorage.setItem('theme', isDark ? 'dark' : 'light');
      if (isAuthenticated) {
        fetch('/Account/SetDarkModePreference?enabled=' + isDark, { credentials: 'same-origin' });
      }
      // Dispatch a themeChanged event for subscribers
      const themeChangedEvent = new CustomEvent('themeChanged', { detail: { dark: isDark } });
      html.dispatchEvent(themeChangedEvent);
    });
    // Trigger fade-in once DOM is ready
    document.body.classList.add('loaded');
  });
})();
