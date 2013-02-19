﻿$(function () {
    var $editplaceholderheight = 0;
    $('#conditions a.editconditionlink').live("click",  function () {
        var qid = $(this).closest("tr").attr("qid");
        $editplaceholderheight = $(this).parent().height();
        if ($("#editcondition").is(":visible")) {
            var h = $("#editcondition").attr("orginalheight");
            $("#editconditionplaceholder").animate({ height: h }, 150);
            $("#editcondition").hide("fade", { direction: "up" }, 150, function () {
                $.post('/Search/Advanced/EditCondition/' + qid, null, function (ret) {
                    $("#conditions").html(ret).ready($.AdjustEditCondition);
                });
            });
        }
        else
            $.post('/Search/Advanced/EditCondition/' + qid, null, function (ret) {
                $("#conditions").html(ret).ready($.AdjustEditCondition);
            });
        return false;
    });
    $("a.tip").tooltip({ showBody: "|", showURL: false });
    $.AdjustEditCondition = function () {
        $(".bt").button();
        $('#CodeValues').multiselect();
        var h = $("#editcondition").outerHeight();
        $("#editcondition").attr("orginalheight", $("#editconditionplaceholder").height());
        $("#editconditionplaceholder").animate({ height: h }, 300);
        var pos = $("#editconditionplaceholder").position();
        $("#editcondition").css("left", pos.left).css("top", pos.top - 2);
        $("#editcondition").show("fade", { direction: "up" }, 300);
        $("a.tip").tooltip({ showBody: "|", showURL: false });
    };
    $('#conditions a.addnewclause').live("click", function () {
        var qid = $(this).closest("tr").attr("qid");
        $.post('/Search/Advanced/AddNewCondition/' + qid, {}, function (ret) {
            $("#conditions").html(ret).ready($.AdjustEditCondition);
            $('#QueryConditionSelect').dialog("open");
        });
        return false;
    });
    $('#conditions a.duplicateclause').live("click", function () {
        var qid = $(this).closest("tr").attr("qid");
        $.post('/Search/Advanced/DuplicateCondition/' + qid, {}, function (ret) {
            $("#conditions").html(ret).ready($.AdjustEditCondition);
        });
        return false;
    });
    $('#SaveCondition').live("click", function () {
        var qs = $('#conditionForm').serialize();
        $("#editconditionplaceholder").animate({ height: $editplaceholderheight }, 200);
        $("#editcondition").hide("slide", { direction: "up" }, 200, function () {
            $.post('/Search/Advanced/SaveCondition/', qs, function (ret) {
                $("#conditions").html(ret).ready(function () {
                    $(".bt").button();
                    $("#conditions a.trigger-dropdown").dropdown();
                    $.block();
                    $("#Run").click();
                });
            });
            return false;
        });
    });
    $('#CancelChange').live("click", function () {
        $("#editconditionplaceholder").animate({ height: $editplaceholderheight }, 200);
        $("#editcondition").hide("slide", { direction: "up" }, 300, function () {
            $.post('/Search/Advanced/Reload/', null, function (ret) {
                $("#conditions").html(ret).ready(function () {
                    $(".bt").button();
                    $("#conditions a.trigger-dropdown").dropdown();
                });
            });
        });
        return false;
    });
    $('#conditions a.removeclause').live("click" , function () {
        jConfirm('Confirm Delete?', 'Confirmation Dialog', function (del) {
            if (del) {
                var qid = $("#SelectedId").val();
                $.post('/Search/Advanced/RemoveCondition/' + qid, null, function(ret) {
                    $("#conditions").html(ret).ready(function() {
                        $(".bt").button();
                        $("#conditions a.trigger-dropdown").dropdown();
                        $("#Run").click();
                    });
                });
            }
        });
        return false;
    });
    $('#Comparison').live("change", function (ev) {
        if ($("#CodesDiv").length > 0) {
            var q = $('#conditionForm').serialize();
            $.post('/Search/Advanced/CodesDropdown', q, function (ret) {
                $("#CodesDiv").replaceWith(ret).ready(function () {
                    $('#CodeValues').multiselect();
                });
            });
        }
    });

    $('#QueryConditionHelp').dialog({
        title: 'Help on Condition',
        bgiframe: true,
        autoOpen: false,
        width: 750,
        height: 700,
        modal: true,
        overlay: {
            opacity: 0.5,
            background: "black"
        }, close: function () {
            $('iframe', this).attr("src", "");
        }
    });
    $('a.help').live("click", function (event) {
        event.preventDefault();
        var d = $('#QueryConditionHelp');
        if (this.href.endsWith('-'))
            $('iframe', d).attr("src", this.href + $("#ConditionName").val());
        else
            $('iframe', d).attr("src", this.href);
        d.dialog("open");
    });
    $('#Tags').click(function (ev) {
        $('#TagsPopup').show();
    });
    $(".datepicker").datepicker();
    $(".bt").button();

    $('#Program').live("change", function (ev) {
        $.post('/Search/Advanced/Divisions/' + $(this).val(), null, function (ret) {
            $("#Division").replaceWith(ret);
            $("#Organization").replaceWith("<select id='Organization' name='Organization'><option value='0'>(not specified)</option></select>")
        });
    });
    $('#Divsion').live("change", function (ev) {
        $.post('/Search/Advanced/Organizations/' + $(this).val(), null, function (ret) {
            $("#Organization").replaceWith(ret);
        });
    });
    $('#Run').live("click", function (ev) {
        RefreshList();
        return false;
    });
    $('#Export').click(function (ev) {
        window.location = "/Search/Advanced/Export/" + $("#QueryId").val();
    });
    var dialogOptions = {
        overlay: { background: "#000", opacity: 0.3 },
        modal: true,
        autoOpen: false,
        closeOnEscape: true,
        width: 500
    };
    $('#SaveQueryDiv').dialog(dialogOptions);
    $('#OpenQueryDiv').dialog(dialogOptions);

    $('#ShowSaveQuery').live("click", function (ev) {
        $('#SaveQueryDesc').val($('#Description').text());
        $('#SaveQueryDiv').dialog("open");
    });
    $('#SaveQuery').click(function (ev) {
        $('#SaveQueryDiv').dialog("close");
        $.post("/Search/Advanced/SaveQuery/", {
            SavedQueryDesc: $('#SaveQueryDesc').val(),
            IsPublic: $('#IsPublic').is(':checked')
        }, function (ret) {
            $("#Description").text(ret);
        });
        return false;
    });
    if ($("#AutoRun").val() == "True")
        $("#Run").click();

    $('#QueryConditionSelect').dialog({
        overlay: { background: "#000", opacity: 0.3 },
        bgiframe: true,
        modal: true,
        autoOpen: false,
        closeOnEscape: true,
        width: 750,
        height: 575,
        position: 'top'
    });
    if ($("#SelectedId").val() > 0) {
        $.AdjustEditCondition();
        $('#QueryConditionSelect').dialog("open");
    }
    $("#tabber").tabs();

    $("#SelectCondition").live("click", function (ev) {
        $('#QueryConditionSelect').dialog("open");
        return false;
    });
    $('div.FieldLink a').click(function (ev) {
        ev.preventDefault();
        var qid = $("#SelectedId").val();
        $.post('/Search/Advanced/SelectCondition/' + qid, { conditionName: ev.target.id }, function (ret) {
            $('#QueryConditionSelect').dialog("close");
            $("#conditions").html(ret).ready($.AdjustEditCondition);
        });
        return false;
    });
    $("a.closeit").click(function (ev) {
        $.unblock();
    });
    $.navigate = function (url, data) {
        url += (url.match(/\?/) ? "&" : "?") + data;
        window.location = url;
    };
    $.fn.enabled = function (bool) {
        if (bool)
            $(this).attr("href", "#").removeClass("disabled");
        else
            $(this).removeAttr("href").addClass("disabled");
        return this;
    };
    $("#Run").click();
});

function RefreshList(qs) {
    if (!qs)
        qs = $("#conditionForm").serialize();

    $.block();
    $.ajax({
        type: "POST",
        url: "/Search/Advanced/Results/",
        data: qs,
        timeout: 1200000, // in milliseconds
        success: function (ret) {
            $('#toolbar').show();
            $('#Results').html(ret);
            $('#people tbody tr:even').addClass('alt');
            $('a.taguntag').click(function (ev) {
                $.post('/Search/Advanced/ToggleTag/' + $(this).attr('value'), null, function (ret) {
                    if (ret.error)
                        alert(ret.error);
                    else
                        $(ev.target).text(ret.HasTag ? "Remove" : "Add");
                });
                return false;
            });
            $('#people thead a.sortable').click(function (ev) {
                var newsort = $(this).text();
                var oldsort = $("#Sort").val();
                $("#Sort").val(newsort);
                var dir = $("#Direction").val();
                if (oldsort == newsort && dir == 'asc')
                    $("#Direction").val('desc');
                else
                    $("#Direction").val('asc');
                RefreshList();
                return false;
            });
            $.unblock();
        },
        error: function (request, status, err) {
            $.unblock();
            alert(err);
        }
    });
}
function GotoPage(pg) {
    var qs = $('#conditionForm').serialize() + "&Page=" + pg;
    RefreshList(qs);
    return false;
}
function SetPageSize(sz) {
    var qs = $('#conditionForm').serialize() + "&PageSize=" + sz;
    RefreshList(qs);
    return false;
}

function ShowErrors(j) {
    $('.validate').each(function () {
        $(this).next(".error").remove();
    });
    var e = eval('(' + j + ')');
    if (e.count == 0)
        return false;
    $('.validate').each(function () {
        if (e[this.id])
            $(this).after("<span class='error'> " + e[this.id] + "</span>");
    });
    return true;
}