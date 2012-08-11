﻿$(function () {
    $.RefreshPage = function () {
        var f = $('#form');
        var q = f.serialize();
        $.blockUI();
        $.post("/OrgMembers/List", q, function (ret) {
            $(f).html(ret).ready(function () {
                $('table.grid > tbody > tr:even', f).addClass('alt');
                $.unblockUI();
                $(".bt").button();
                $("#manage select").css("width", "100%");
            });
        });
    }
    $(".bt").button();
    $("select").css("width", "100%");
    $("#form").delegate('#ProgId', "change", $.RefreshPage);
    $("#form").delegate('#DivId', "change", $.RefreshPage);
    $("#form").delegate('#SourceId', "change", $.RefreshPage);
    $("#form").delegate('#TargetId', "change", $.RefreshPage);
    $("#form").delegate('#MembersOnly', "change", $.RefreshPage);
    $("#form").delegate('#Grades', "change", $.RefreshPage);
    $("#form").delegate('#move', "click", function (e) {
        e.preventDefault();
        var f = $('#form');
        var q = f.serialize();
        $.blockUI();
        $.post("/OrgMembers/Move", q, function (ret) {
            $(f).html(ret).ready(function () {
                $('table.grid > tbody > tr:even', f).addClass('alt');
                $.unblockUI();
                $.growlUI("Move", "Completed");
                $(".bt").button();
                $("#manage select").css("width", "100%");
            });
        });
    });
    $("#SelectAll").live("click", function () {
        if ($(this).attr("checked"))
            $("table.grid input[name='List']").attr('checked', true);
        else
            $("table.grid input[name='List']").removeAttr('checked');
    });
    $.blockUI.defaults.growlCSS = {
        width: '350px',
        top: '40%',
        left: '35%',
        right: '10px',
        border: 'none',
        padding: '5px',
        opacity: '0.7',
        cursor: null,
        color: '#fff',
        backgroundColor: '#000',
        '-webkit-border-radius': '10px',
        '-moz-border-radius': '10px'
    };
    $.growlUI = function (title, message, timeout) {
        var $m = $('<div class="growlUI"></div>');
        if (title) $m.append('<h1>' + title + '</h1>');
        if (message) $m.append('<h2>' + message + '</h2>');
        if (timeout == undefined) timeout = 3000;
        $.blockUI({
            message: $m, fadeIn: 400, fadeOut: 700, centerY: false,
            timeout: timeout, showOverlay: false,
            css: $.blockUI.defaults.growlCSS
        });
    };
    $("#EmailNotices").live("click", function (e) {
        e.preventDefault();
        var f = $("#form");
        var q = f.serialize();
        $.post("/OrgMembers/EmailNotices", q, function (ret) {
            $(f).html(ret).ready(function () {
                $('table.grid > tbody > tr:even', f).addClass('alt');
                $.growlUI("Email Notices", "emails sent");
                $(".bt").button();
                $("#manage select").css("width", "100%");
            });
        });
        return false;
    });
    $("#form").submit(function () {
        return false;
    });

    //    $('input.check').click(UpdateTotals);
    $('form table.grid a.sortable').live("click", function (ev) {
        var newsort = $(this).text();
        var oldsort = $("#Sort").val();
        $("#Sort").val(newsort);
        var dir = $("#Dir").val();
        if (oldsort == newsort && dir == 'asc')
            $("#Dir").val('desc');
        else
            $("#Dir").val('asc');
        $.RefreshPage();
    });
    //$('#total').text($('.check').length);
    //    UpdateTotals = function() {
    //        $('#ttotal').text($('.check:checked').length);
    //    }
});
