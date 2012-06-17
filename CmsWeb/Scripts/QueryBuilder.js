﻿var qs = "";
$(function () {
    $("#SelectCondition").SelectCondition();

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
        $('iframe', d).attr("src", this.href + $("#ConditionName").val());
        d.dialog("open");
    });
    $("a.help,.helptip").tooltip({ showBody: "|", showURL: false });
    $('#Tags').click(function (ev) {
        $('#TagsPopup').show();
    });
    $(".datepicker").datepicker({
        dateFormat: 'm/d/yy',
        changeMonth: true,
        changeYear: true
    });
    $(".bt").button();
    $("#selectconditions select").css("width", "100%");
    $("#targetpeople").click(function (ev) {
        ev.preventDefault();
        if ($('a.target[target="people"]').length == 0)
            $("a.target").attr("target", "people");
        else
            $("a.target").removeAttr("target");
        return false;
    });

    HighlightCondition();
    $('#Program').change(function (ev) {
        $.post('/QueryBuilder/GetDivisions/' + $(this).val(), null, function (ret) {
            $('#Division').fillOptions(ret.Divisions);
            $('#Organization').fillOptions(ret.Organizations);
            CascadeDivision();
            $("#selectconditions select").css("width", "100%");
        });
    });
    $('#AddToGroup[href]').live("click", function () {
        qs = $('#conditionForm').serialize();
        $.post('/QueryBuilder/AddToGroup/', qs, function (ret) {
            var a = ret.split("<---------->");
            if (!ShowErrors(a[1]))
                FillConditionGrid(a[0]);
        });
        return false;
    });
    $('#Add[href]').live("click", function () {
        qs = $('#conditionForm').serialize();
        $.post('/QueryBuilder/Add/', qs, function (ret) {
            var a = ret.split("<---------->");
            if (!ShowErrors(a[1]))
                FillConditionGrid(a[0]);
        });
        return false;
    });
    $('#Update[href]').live("click", function () {
        qs = $('#conditionForm').serialize();
        $.post('/QueryBuilder/Update/', qs, function (ret) {
            var a = ret.split("<---------->");
            if (!ShowErrors(a[1]))
                FillConditionGrid(a[0]);
        });
        return false;
    });
    $('#Remove[href]').live("click", function () {
        qs = $('#conditionForm').serialize();
        $.post('/QueryBuilder/Remove/', qs, function (ret) {
            UpdateView(ret);
            $.post('/QueryBuilder/Conditions/', null, function (ret) {
                FillConditionGrid(ret);
            });
        });
        return false;
    });
    $('#Run').click(function (ev) {
        qs = $('#conditionForm').serialize();
        RefreshList();
        return false;
    });
    $('#Export').click(function (ev) {
        window.location = "QueryBuilder/Export/" + $("#QueryId").val();
    });
    dialogOptions = {
        overlay: { background: "#000", opacity: 0.3 },
        bgiframe: true,
        modal: true,
        autoOpen: false,
        closeOnEscape: true,
        width: 500
    };
    $('#SaveQueryDiv').dialog(dialogOptions);
    $('#OpenQueryDiv').dialog(dialogOptions);

    $('#ShowSaveQuery').click(function (ev) {
        $('#SaveQueryDesc').val($('#Description').text());
        $('#SaveQueryDiv').dialog("open");
    });
    $('#ShowOpenQuery').click(function (ev) {
        $.post("/QueryBuilder/SavedQueries", null, function (ret) {
            $('#ExistingQueries').fillOptions(ret);
        });
        $('#OpenQueryDiv').dialog("open");
        return false;
    });
    $('#OpenQuery').click(function (ev) {
        $('#OpenQueryDiv').dialog("close");
        var a = $("#ExistingQueries").val().split(':');
        window.location = "/QueryBuilder/Main/" + a[0];
    });
    $('#SaveQuery').click(function (ev) {
        $('#SaveQueryDiv').dialog("close");
        $.post("/QueryBuilder/SaveQuery/", {
            SavedQueryDesc: $('#SaveQueryDesc').val(),
            IsPublic: $('#IsPublic').is(':checked')
        }, function (ret) {
            $("#Description").text(ret);
        });
        return false;
    });
});
function HighlightCondition() {
    $('#ConditionGrid li a').removeClass('SelectedRow');
    $('#ConditionGrid li a#' + $('#SelectedId').val()).addClass('SelectedRow');
    $('#ConditionGrid li a').click(EditCondition);
    $(".conditionPopup").contextMenu({ menu: 'InsCopyMenu' }, function(action, el, pos) {
	    switch (action) {
	        case "ins":
	            $.post("/QueryBuilder/InsGroupAbove/" + $(el).attr('id'), null, function(ret) {
	                $.navigate("/QueryBuilder/Main/" + ret);
	            });
	            break;
	        case "copy":
	            $.post("/QueryBuilder/CopyAsNew/" + $(el).attr('id'), null, function(ret) {
	                $.navigate("/QueryBuilder/Main/" + ret);
	            });
	            break;
	    }
	});
}
function FillConditionGrid(html) {
    $('#ConditionGrid').html(html).ready(function () {
        HighlightCondition();
        $('#ConditionGrid li a').click(EditCondition);
    });
}
(function($) {
    $.QueryString = function(q, item) {
        var r = new Object();
        $.each(q.split('&'), function() {
            var kv = this.split('=');
            r[kv[0]] = kv[1];
        });
        return r[item];
    };
    $.block = function() {
        $.blockUI({ message: 'working on it...<img src="/content/loading.gif"/>' });
    };
    $.unblock = function() {
        $.unblockUI({ fadeOut: 150 });
    };
    $.navigate = function(url, data) {
        url += (url.match(/\?/) ? "&" : "?") + data;
        window.location = url;
    };
    $.fn.showhide = function(bool) {
        if (bool)
            $(this).show();
        else
            $(this).hide();
        return this;
    };
    $.fn.enabled = function(bool) {
        if (bool)
            $(this).attr("href", "#").removeClass("disabled");
        else
            $(this).removeAttr("href").addClass("disabled");
        return this;
    };
    $.fn.fillOptions = function(a, multiple) {
        var options = '';
        if (a)
            for (var i = 0; i < a.length; i++) {
            options += '<option value="' + a[i].Value + '"';
            if (a[i].Selected)
                options += ' selected=\'selected\''
            options += '>' + a[i].Text + '</option>';
        }
        return this.each(function() {
            var s = "<select id='" + this.id + "' name='" + this.id + "'";
            if (multiple)
                s += " multiple='multiple'";
            s += ">" + options + "</select>";
            $(this).replaceWith(s);
        });
    };
    $.fn.multiSelectRemove = function() {
        $(this).each(function() {
            $(this).next('.multiselect').remove();
            $(this).next('.multiSelectOptions').remove();
        });
        return $(this);
    };
})(jQuery);

function RefreshList() {
    $.block();
    $.ajax({
        type: "POST",
        url: "/QueryBuilder/Results/",
        data: qs,
        timeout: 1200000, // in milliseconds
        success: function (ret) {
            $('#toolbar').show();
            $('#Results').html(ret);
            $('#people tbody tr:even').addClass('alt');
            $('a.taguntag').click(function (ev) {
                $.post('/QueryBuilder/ToggleTag/' + $(this).attr('value'), null, function (ret) {
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
                qs = $('#conditionForm').serialize();
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
    qs = $('#conditionForm').serialize() + "&Page=" + pg;
    RefreshList();
    return false;
}
function SetPageSize(sz) {
    qs = $('#conditionForm').serialize() + "&PageSize=" + sz;
    RefreshList();
    return false;
}
function CascadeDivision() {
    $('#Division').change(function(ev) {
        $.post('/QueryBuilder/GetOrganizations/' + $(this).val(), null, function(ret) {
            $('#Organization').fillOptions(ret);
            $("#selectconditions select").css("width", "100%");
        });
    });
}
function CascadeComparison() {
    
    $('#Comparison').change(function(ev) {
        $.post('/QueryBuilder/GetCodes/', {
            ConditionName: $('#ConditionName').val(),
            Comparison: $('#Comparison').val()
        }, function(ret) {
            UpdateCodes(ret);
        });
    });
}
function EditCondition(ev) {
    $('#ConditionGrid li').removeClass('SelectedRow');
    var qid = $(this).parent("li").attr("id");
    $('#ConditionGrid li#' + qid).addClass('SelectedRow');
    $.post('/QueryBuilder/EditCondition/' + qid, null, function(ret) {
        UpdateView(ret);
    });
    return false;
}

(function($) {
    var o = {};
    $.fn.SelectCondition = function() {
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
        $("#tabber").tabs();
        this.click(function(ev) {
            o.Id = this.id;
            $('#QueryConditionSelect').dialog("open");
            return false;
        });
        $('div.FieldLink a').click(function(ev) {
            $.post('/QueryBuilder/SelectCondition/', {
                ConditionName: ev.target.id,
                Id: $('#SelectedId').val()
            }, function(ret) {
                //$.unblockUI();
                $('#QueryConditionSelect').dialog("close");
                UpdateView(ret);
            });
            return false;
        });
        $("a.closeit").click(function(ev) {
            $.unblockUI();
        });
        return this;
    };
})(jQuery);


function UpdateCodes(ret) {
    $('#CodeValues').multiselect("destroy");
    $('#CodeValues').remove();
    $('#CodeValue').remove();
    if (ret.CodeVisible || ret.CodesVisible) {
        if (ret.SelectMultiple) {
            $('#values').after('<select id="CodeValues"></select>');
            $('#CodeValues').fillOptions(ret.CodeData, true);
            $('#CodeValues').multiselect();
        }
        else {
            $('#values').after('<select id="CodeValue"></select>');
            $('#CodeValue').fillOptions(ret.CodeData);
        }
    }
}

function ShowErrors(j) {
    $('.validate').each(function() {
        $(this).next(".error").remove();
    });
    var e = eval('(' + j + ')');
    if (e.count == 0)
        return false;
    $('.validate').each(function() {
        if (e[this.id])
            $(this).after("<span class='error'> " + e[this.id] + "</span>");
    });
    return true;
}
function UpdateView(vs) {
    $('#SelectedId').val(vs.SelectedId);
    UpdateCodes(vs);
    $('#Comparison').fillOptions(vs.CompareData);
    if (vs.CodeVisible || vs.CodesVisible)
        CascadeComparison();
    $('#Division').fillOptions(vs.DivisionData);
    CascadeDivision();
    $('#Organization').fillOptions(vs.OrganizationData);
    $('#TagsDiv').showhide(vs.TagsVisible);
    if (vs.TagsVisible) {
        $('#tagvalues').multiSelectRemove();
        $('#Tags').remove();
        $('#tagvalues').after('<select id="Tags"></select>');
        $('#Tags').fillOptions(vs.TagData, true);
        $('#Tags').multiselect();
    } 

    $('#ConditionName').val(vs.ConditionName);
    $('#ConditionText').text(vs.ConditionText);
    $('#TextValue').val(vs.TextValue);
    $('#IntegerValue').val(vs.IntegerValue);
    $('#NumberValue').val(vs.NumberValue);
    $('#DateValue').val(vs.DateValue);
    if(vs.CodeValue != "")
        $('#CodeValue').val(vs.CodeValue);
    $('#CodesValue').val(vs.CodesValue);
    $('#Program').val(vs.Program);
    $('#Division').val(vs.Division);
    $('#Organization').val(vs.Organization);
    $('#View').val(vs.View);
    $('#SavedQueryDesc').val(vs.SavedQueryDesc);
    $('#Ministry').val(vs.Ministry);
    $('#Schedule').val(vs.Schedule);
    $('#Campus').val(vs.Campus);
    $('#OrgType').val(vs.OrgType);
    $('#Days').val(vs.Days);
    $('#Age').val(vs.Age);
    $('#Quarters').val(vs.Quarters);
    $('#StartDate').val(vs.StartDate);
    $('#EndDate').val(vs.EndDate);
    if (vs.Comparison)
        $('#Comparison').val(vs.Comparison);
    else
        $('#Comparison')[0].selected = true;

    $('#SchedDiv').showhide(vs.ScheduleVisible);
    $('#CampusDiv').showhide(vs.CampusVisible);
    $('#OrgTypeDiv').showhide(vs.OrgTypeVisible);
    $('#ProgDiv').showhide(vs.ProgramVisible);
    $('#DivDiv').showhide(vs.DivisionVisible);
    $('#EndDiv').showhide(vs.EndDateVisible);
    $('#StartDiv').showhide(vs.StartDateVisible);
    $('#OrgDiv').showhide(vs.OrganizationVisible);
    $('#ViewDiv').showhide(vs.ViewVisible);
    $('#DaysDiv').showhide(vs.DaysVisible);
    $('#AgeDiv').showhide(vs.AgeVisible);
    $('#OtherLabel').text(vs.OtherLabel);
    $('#SavedQueryDiv').showhide(vs.SavedQueryVisible);
    $('#MinistryDiv').showhide(vs.MinistryVisible);
    $('#QuartersDiv').showhide(vs.QuartersVisible);
    $('#QuartersLabel').text(vs.QuartersLabel);

    $('#RightPanel').showhide(vs.RightPanelVisible);
    $('#TextValue').showhide(vs.TextVisible);
    $('#CodeValue').showhide(vs.CodeVisible);
    $('#CodesValue').showhide(vs.CodesVisible);
    $('#DateValue').showhide(vs.DateVisible);
    $('#NumberValue').showhide(vs.NumberVisible);
    $('#IntegerValue').showhide(vs.IntegerVisible);

    $('#Update').enabled(vs.UpdateEnabled);
    $('#Add').enabled(vs.AddEnabled);
    $('#AddToGroup').enabled(vs.AddToGroupEnabled);
    $('#Remove').enabled(vs.RemoveEnabled);
//    var widest = null;
//    $("#selectconditions select").each(function() {
//      if (widest == null)
//        widest = $(this);
//      else
//      if ($(this).width() > widest.width())
//        widest = $(this);
//    });
    $("#selectconditions select").css("width", "100%");
}
