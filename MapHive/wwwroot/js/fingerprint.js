/**
 * Browser fingerprinting script using Fingerprint.js
 * This script generates a unique identifier for each device based on browser characteristics.
 */

// Initialize the fingerprinting when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeFingerprinting();
});

/**
 * Initializes the fingerprinting process
 */
function initializeFingerprinting() {
    // Load Fingerprint.js from CDN if it's not already loaded
    if (typeof FingerprintJS === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/@fingerprintjs/fingerprintjs@3/dist/fp.min.js';
        script.onload = generateFingerprint;
        document.head.appendChild(script);
    } else {
        generateFingerprint();
    }
}

/**
 * Generates the fingerprint and stores it in a hidden form field
 */
async function generateFingerprint() {
    try {
        // Initialize an agent
        const fpPromise = FingerprintJS.load();
        
        // Get the visitor identifier
        const fp = await fpPromise;
        const result = await fp.get();
        
        // The visitorId is the stable identifier
        const fingerprint = result.visitorId;
        
        // Store the fingerprint in a hidden field if it exists
        const fingerprintField = document.getElementById('DeviceFingerprint');
        if (fingerprintField) {
            fingerprintField.value = fingerprint;
        }
        
        // Also store in localStorage for future use
        localStorage.setItem('deviceFingerprint', fingerprint);
        
        console.log('Device fingerprint generated:', fingerprint);
    } catch (error) {
        console.error('Error generating fingerprint:', error);
    }
}

/**
 * Gets the stored fingerprint from localStorage
 * @returns {string} The stored fingerprint or empty string if not found
 */
function getStoredFingerprint() {
    return localStorage.getItem('deviceFingerprint') || '';
}

/**
 * Applies the fingerprint to any form with the data-use-fingerprint attribute
 */
function applyFingerprintToForms() {
    const fingerprint = getStoredFingerprint();
    if (!fingerprint) {
        return;
    }
    
    // Find all forms that should use the fingerprint
    const forms = document.querySelectorAll('form[data-use-fingerprint]');
    forms.forEach(form => {
        // Check if the form already has a fingerprint field
        let fingerprintField = form.querySelector('input[name="DeviceFingerprint"]');
        
        // If not, create one
        if (!fingerprintField) {
            fingerprintField = document.createElement('input');
            fingerprintField.type = 'hidden';
            fingerprintField.name = 'DeviceFingerprint';
            fingerprintField.id = 'DeviceFingerprint';
            form.appendChild(fingerprintField);
        }
        
        // Set the fingerprint value
        fingerprintField.value = fingerprint;
    });
} 