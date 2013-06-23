﻿$(function () {
    $(".datepicker").datepicker();
    $("#Dt1").change(function () {
        $("#Dt2").val("");
        $.reloadmeetings();
    });
    $("#Dt2").change(function () {
        $.reloadmeetings();
    });
    $.reloadmeetings = function() {
        $("#meetingsform").submit();
    };
    $('#Inactive').change($.reloadmeetings);
    $('#NoZero').change($.reloadmeetings);
    $('table.grid tbody tr:even').addClass('alt');
    $("a.sortable").click(function (ev) {
        ev.preventDefault();
        var newsort = $(this).text();
        var oldsort = $("#Sort").val();
        $("#Sort").val(newsort);
        var dir = $("#Direction").val();
        if (oldsort == newsort && dir == 'asc')
            $("#Direction").val('desc');
        else
            $("#Direction").val('asc');
        $.reloadmeetings();
    });
});
