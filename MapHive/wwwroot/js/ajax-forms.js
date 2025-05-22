// Simple AJAX form handling
$(document).ready(function () {
    // Handle form submissions with ajax-form class
    $(document).on('submit', 'form[data-ajax="true"]', function (e) {
        e.preventDefault();
        
        var form = $(this);
        var url = form.attr('action');
        var method = form.attr('method') || 'POST';
        var updateTarget = form.attr('data-ajax-update');
        
        $.ajax({
            url: url,
            type: method,
            data: form.serialize(),
            success: function (result) {
                if (updateTarget) {
                    $(updateTarget).html(result);
                    // Re-parse validation on the new form
                    $.validator.unobtrusive.parse(updateTarget + " form");
                } else if (result === "") {
                    // Empty response means success, redirect to profile
                    window.location.href = '/Account/PublicProfileById?accountId=' + form.data('account-id');
                }
            },
            error: function (xhr, status, error) {
                console.error("Form submission error:", error);
                alert("An error occurred. Please try again.");
            }
        });
    });
}); 