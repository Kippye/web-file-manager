// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$('.local-datetime').each(function() {
    var $this = $(this), utcDate = new Date($this.attr('data-utc'));

    if (!utcDate) {
        return;
    }

    $this.text(utcDate.toLocaleString());
});