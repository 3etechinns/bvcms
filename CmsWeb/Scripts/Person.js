﻿$(function () {
    var addrtabs = $("#address-tab").tabs();
    $("#enrollment-tab").tabs();
    $("#member-tab").tabs();
    $("#growth-tab").tabs();
    $("#system-tab").tabs();
    var maintabs = $("#main-tab").tabs();
    $(".submitbutton,.bt").button();
    addrtabs.tabs('select', $('#addrtab').val());
    $('#dialogbox').SearchPeopleInit({ overlay: { background: "#000", opacity: 0.3} });
    $('#clipaddr').live('click', function () {
        var inElement = $('#addrhidden')[0];
        if (inElement.createTextRange) {
            var range = inElement.createTextRange();
            if (range)
                range.execCommand('Copy');
        }
        return false;
    });
    $('#deleteperson').click(function () {
        var href = $(this).attr("href");
        if (confirm('Are you sure you want to delete?')) {
            $.post(href, null, function (ret) {
                if (ret != "ok") {
                    $.blockUI({ message: "delete Failed: " + ret });
                    $('.blockOverlay').attr('title', 'Click to unblock').click($.unblockUI);
                }
                else {
                    $.blockUI({ message: "person deleted" });
                    $('.blockOverlay').attr('title', 'Click to unblock').click(function () {
                        $.unblockUI();
                        window.location = "/";
                    });
                }
            });
        }
        return false;
    });
    $('a.deloptout').live("click", function (ev) {
        ev.preventDefault();
        var href = $(this).attr("href");
        if (confirm('Are you sure you want to delete?')) {
            $.post(href, {}, function (ret) {
                if (ret != "ok")
                    $.growlUI("failed", ret);
                else {
                    $.updateTable($('#user-tab form'));
                    $.growlUI("Success", "OptOut deleted");
                }
            });
        }
    });
    $('#moveperson').click(function (ev) {
        $('#dialogbox').SearchPeople(ev, function (id, peopleid) {
            window.location = "/Merge?PeopleId1=" + $("#PeopleId").val() + "&PeopleId2=" + peopleid;
        });
        return false;
    });

    $.editable.addInputType('datepicker', {
        element: function (settings, original) {
            var input = $('<input>');
            if (settings.width != 'none') { input.width(settings.width); }
            if (settings.height != 'none') { input.height(settings.height); }
            input.attr('autocomplete', 'off');
            $(this).append(input);
            return (input);
        },
        plugin: function (settings, original) {
            var form = this;
            settings.onblur = 'ignore';
            $(this).find('input').datepicker().bind('click', function () {
                $(this).datepicker('show');
                return false;
            }).bind('dateSelected', function (e, selectedDate, $td) {
                $(form).submit();
            });
        }
    });
    $.editable.addInputType("multiselect", {
        element: function (settings, original) {
            var select = $('<select multiple="multiple" />');

            if (settings.width != 'none') { select.width(settings.width); }
            if (settings.size) { select.attr('size', settings.size); }

            $(this).append(select);
            return (select);
        },
        content: function (json, settings, original) {
            for (var key in json) {
                var option = $('<option />').val(key).text(key);
                if (json[key] == true)
                    option.attr("selected", true);
                $('select', this).append(option);
            }
            $("select", this).multiselect({
                close: function (event, ui) {
                    var values = $("select").val();
                },
                position: {
                    my: 'left bottom',
                    at: 'left top'
                }
            });
        }
    });
    $.extraEditable = function (table) {
        $('.editarea', table).editable('/Person/EditExtra/', {
            type: 'textarea',
            submit: 'OK',
            rows: 10,
            width: 600,
            indicator: '<img src="/images/loading.gif">',
            tooltip: 'Click to edit...'
        });
        $(".clickEdit", table).editable("/Person/EditExtra/", {
            indicator: "<img src='/images/loading.gif'>",
            tooltip: "Click to edit...",
            style: 'display: inline',
            width: '300px',
            height: 25,
            submit: 'OK'
        });
        $(".clickDatepicker").editable('/Person/EditExtra/', {
            type: 'datepicker',
            tooltip: 'Click to edit...',
            style: 'display: inline',
            width: '300px',
            submit: 'OK'
        });
        $(".clickSelect", table).editable("/Person/EditExtra/", {
            indicator: '<img src="/images/loading.gif">',
            loadurl: "/Person/ExtraValues/",
            loadtype: "POST",
            type: "select",
            submit: "OK",
            style: 'display: inline'
        });
        $(".clickCheckbox", table).editable('/Person/EditExtra', {
            type: 'checkbox',
            onblur: 'ignore',
            submit: 'OK'
        });
        $('.clickMultiselect', table).editable('/Person/EditExtra', {
            indicator: '<img src="/images/loading.gif">',
            loadurl: "/Person/ExtraValues2/",
            loadtype: "POST",
            type: "multiselect",
            submit: "OK",
            onblur: 'ignore',
            style: 'display: inline'
        });
    };
    $.getTable = function (f, q) {
        q = q || f.serialize();
        $.post(f.attr('action'), q, function (ret) {
            $(f).html(ret).ready(function () {
                $('table.grid > tbody > tr:even', f).addClass('alt');
                //$('.dropdown', f).hoverIntent(dropdownshow, dropdownhide);
                $('.bt').button();
                $(".datepicker").datepicker();
                $.extraEditable('#extravalues');
                $('.tooltip', f).tooltip({ showURL: false, showBody: '|' });
            });
        });
        return false;
    };
    $('#memberDialog').dialog({
        title: 'Member Dialog',
        bgiframe: true,
        autoOpen: false,
        width: 600,
        height: 550,
        modal: true,
        overlay: {
            opacity: 0.5,
            background: "black"
        }, close: function () {
            $('iframe', this).attr("src", "");
        }
    });
    $('form a.membertype').live("click", function (e) {
        e.preventDefault();
        var d = $('#memberDialog');
        $('iframe', d).attr("src", this.href);
        d.dialog("open");
    });
    $('#previous-tab form a.membertype').live("click", function (e) {
        e.preventDefault();
        var d = $('#memberDialog');
        $('iframe', d).attr("src", this.href);
        d.dialog("open");
    });

    $(".CreateAndGo").click(function () {
        if (confirm($(this).attr("confirm")))
            $.post($(this).attr("href"), null, function (ret) {
                window.location = ret;
            });
        return false;
    });

    $("#enrollment-link").click(function () {
        $.showTable($('#current-tab form'));
    });
    $("#previous-link").click(function () {
        $.showTable($('#previous-tab form'));
    });
    $("#pending-link").click(function () {
        $.showTable($('#pending-tab form'));
    });
    $("#attendance-link").click(function () {
        $.showTable($('#attendance-tab form'));
    });
    $("#contacts-link").click(function () {
        $("#contacts-tab form").each(function () {
            $.showTable($(this));
        });
    });
    $("#member-link").click(function () {
        var f = $("#memberdisplay");
        if ($("table", f).size() == 0) {
            $.post(f.attr('action'), null, function (ret) {
                $(f).html(ret).ready(function () {
                    $.UpdateForSection(f);
                    ShowMemberExtras();
                });
            });
            $.showTable($("#extras-tab form"));
            $.extraEditable('#extravalues');
        }
    });
    $("#system-link").click(function () {
        $.showTable($("#user-tab form"));
    });
    $("#changes-link").click(function () {
        $.showTable($("#changes-tab form"));
    });
    $("#volunteer-link").click(function () {
        $.showTable($("#volunteer-tab form"));
    });
    $("#duplicates-link").click(function () {
        $.showTable($("#duplicates-tab form"));
    });
    $("#optouts-link").click(function () {
        $.showTable($("#optouts-tab form"));
    });
    $('#family table.grid > tbody > tr:even').addClass('alt');
    $("#recreg-link").click(function (ev) {
        ev.preventDefault();
        var f = $('#recreg-tab form')
        if ($('table', f).size() > 0)
            return false;
        var q = f.serialize();
        $.post(f.attr('action'), q, function (ret) {
            $(f).html(ret);
        });
        return false;
    });

    $("a.displayedit").live('click', function (ev) {
        ev.preventDefault();
        var f = $(this).closest('form');
        $.post($(this).attr('href'), null, function (ret) {
            $(f).html(ret).ready(function () {
                $.UpdateForSection(f);
            });
        });
        return false;
    });

    $.UpdateForSection = function (f) {
        var acopts = {
            minChars: 3,
            matchContains: 1
        };
        $('#Employer', f).autocomplete("/Person/Employers", acopts);
        $('#School', f).autocomplete("/Person/Schools", acopts);
        $('#Occupation', f).autocomplete("/Person/Occupations", acopts);
        $('#NewChurch,#PrevChurch', f).autocomplete("/Person/Churches", acopts);
        $(".datepicker").datepicker();
        $(".submitbutton,.bt").button();
        $('.dropdown', f).hoverIntent(dropdownshow, dropdownhide);
        $("#verifyaddress").click(function () {
            var f = $(this).closest('form');
            var q = f.serialize();
            $.post($(this).attr('href'), q, function (ret) {
                if (confirm(ret.address + "\nUse this Address?")) {
                    $('#Address1', f).val(ret.Line1);
                    $('#Address2', f).val(ret.Line2);
                    $('#City', f).val(ret.City);
                    $('#State', f).val(ret.State);
                    $('#Zip', f).val(ret.Zip);
                }
            });
            return false;
        });
        return false;
    };
    $("form.DisplayEdit a.submitbutton").live('click', function (ev) {
        ev.preventDefault();
        var f = $(this).closest('form');
        if (!$(f).valid())
            return;
        var q = f.serialize();
        $.post($(this).attr('href'), q, function (ret) {
            $(f).html(ret).ready(function () {
                var bc = $('#businesscard');
                $.post($(bc).attr("href"), null, function (ret) {
                    $(bc).html(ret);
                });
                $('.dropdown', f).hoverIntent(dropdownshow, dropdownhide);
                $(".submitbutton,.bt").button();
            });
        });
        return false;
    });
    $("#future").live('click', function (ev) {
        ev.preventDefault();
        var f = $(this).closest('form');
        var q = f.serialize();
        $.post($(f).attr("action"), q, function (ret) {
            $(f).html(ret);
        });
    });
    $("form.DisplayEdit").submit(function () {
        if (!$("#submitit").val())
            return false;
        return true;
    });
    $.validator.setDefaults({
        highlight: function (input) {
            $(input).addClass("ui-state-highlight");
        },
        unhighlight: function (input) {
            $(input).removeClass("ui-state-highlight");
        },
        rules: {
            "NickName": { maxlength: 15 },
            "Title": { maxlength: 10 },
            "First": { maxlength: 25, required: true },
            "Middle": { maxlength: 15 },
            "Last": { maxlength: 100, required: true },
            "Suffix": { maxlength: 10 },
            "AltName": { maxlength: 100 },
            "Maiden": { maxlength: 20 },
            "HomePhone": { maxlength: 20 },
            "CellPhone": { maxlength: 20 },
            "WorkPhone": { maxlength: 20 },
            "EmailAddress": { maxlength: 150 },
            "School": { maxlength: 60 },
            "Employer": { maxlength: 60 },
            "Occupation": { maxlength: 60 },
            "WeddingDate": { date: true },
            "DeceasedDate": { date: true },
            "Grade": { number: true },
            "Address1": { maxlength: 40 },
            "Address2": { maxlength: 40 },
            "City": { maxlength: 30 },
            "Zip": { maxlength: 15 },
            "FromDt": { date: true },
            "ToDt": { date: true }
        }
    });
    $('#addrf').validate();
    $('#addrp').validate();
    $('#basic').validate();
    $(".atck").live("change", function (ev) {
        var ck = $(this);
        $.post("/Meeting/MarkAttendance/", {
            MeetingId: $(this).attr("mid"),
            PeopleId: $(this).attr("pid"),
            Present: ck.is(':checked')
        }, function (ret) {
            if (ret.error) {
                ck.attr("checked", !ck.is(':checked'));
                alert(ret.error);
            }
            else {
                var f = ck.closest('form');
                var q = f.serialize();
                $.post($(f).attr("action"), q, function (ret) {
                    $(f).html(ret);
                });
            }
        });
    });
    $("#newvalueform").dialog({
        autoOpen: false,
        buttons: {
            "Ok": function () {
                var v = $("input[name='typeval']:checked").val();
                var fn = $("#fieldname").val();
                var va = $("#fieldvalue").val();
                if (fn)
                    $.post("/Person/NewExtraValue/" + $("#PeopleId").val(), { field: fn, type: v, value: va }, function (ret) {
                        if (ret.startsWith("error"))
                            alert(ret);
                        else {
                            $.getTable($("#extras-tab form"));
                            $.extraEditable('#extravalues');
                        }
                        $("#fieldname").val("");
                        $("#fieldvalue").val("");
                    });
                $(this).dialog("close");
            }
        }
    });
    $("#newextravalue").live("click", function (ev) {
        ev.preventDefault();
        var d = $('#newvalueform');
        d.dialog("open");
    });
    $("a.deleteextra").live("click", function (ev) {
        ev.preventDefault();
        if (confirm("are you sure?"))
            $.post("/Person/DeleteExtra/" + $("#PeopleId").val(), { field: $(this).attr("field") }, function (ret) {
                if (ret.startsWith("error"))
                    alert(ret);
                else {
                    $.getTable($("#extras-tab form"));
                    $.extraEditable('#extravalues');
                }
            });
        return false;
    });
    $("a.reverse").live('click', function (ev) {
        ev.preventDefault();
        var f = $(this).closest('form');
        $.post("/Person/Reverse", {
            id: $("#PeopleId").val(),
            field: $(this).attr("field"),
            value: $(this).attr("value"),
            pf: $(this).attr("pf")
        }, function (ret) {
            $(f).html(ret);
        });
    });
    $.editable.addInputType("checkbox", {
        element: function (settings, original) {
            var input = $('<input type="checkbox">');
            $(this).append(input);
            $(input).click(function () {
                var value = $(input).attr("checked") ? 'True' : 'False';
                $(input).val(value);
            });
            return (input);
        },
        content: function (string, settings, original) {
            var checked = string == "True" ? true : false;
            var input = $(':input:first', this);
            $(input).attr("checked", checked);
            var value = $(input).attr("checked") ? 'True' : 'False';
            $(input).val(value);
        }
    });
    //    $.editable.addInputType("multiselect", {
    //        element: function (settings, original) {
    //            var textarea = $('<select />');
    //            if (settings.rows) {
    //                textarea.attr('rows', settings.rows);
    //            } else {
    //                textarea.height(settings.height);
    //            }
    //            if (settings.cols) {
    //                textarea.attr('cols', settings.cols);
    //            } else {
    //                textarea.width(settings.width);
    //            }
    //            $(this).append(textarea);
    //            return (textarea);
    //        },
    //        plugin: function (settings, original) {
    //            $('textarea', this).multiselect();
    //        },
    //        submit: function (settings, original) {
    //            var value = $('#hour_').val() + ':' + $('#min_').val();
    //            $('input', this).val(value);
    //        }
    //    }); 

});
function RebindMemberGrids(from) {
    $.updateTable($('#current-tab form'));
    $.updateTable($('#pending-tab form'));
    $("#memberDialog").dialog('close');
}
function RebindUserInfoGrid(from) {
    $.updateTable($('#user-tab form'));
    $("#memberDialog").dialog('close');
}
