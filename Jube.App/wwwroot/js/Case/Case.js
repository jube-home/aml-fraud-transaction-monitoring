// noinspection JSObsoletePrivateAccessSyntax,JSJQueryEfficiency

/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License 
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty  
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not, 
 * see <https://www.gnu.org/licenses/>.
 */

var DrillName;
var DrillValue;
var CaseWorkflowGuid;
var CaseId;
var SessionCaseSearchCompiledSqlControllerGuid;
var CaseKey;
var CaseKeyValue;
var SelectedCasesWorkflowFormID;
var SelectedCasesWorkflowDisplayID;
var values = {};
var tabstrip;
var MainTab;
var counter = 10;
var id;
var dateFields = [];
var PendingUpdateCaseInstruction = false;
var PendingUpdateCaseTimer;
var PendingUpdateCaseInstructionRefreshDisplay;
var Priorities;
var Drills;
var Actions;
var Macros;
var Status;
var Displays;
var Forms;
var ResponsePayload;

function formatNote(id, rawNote, createdUser, createdDate, actionId, priorityId) {
    let actionName;
    for (let i = 0; i < Actions.length; i++) {
        if (Actions[i].id === actionId) {
            actionName = Actions[i].name;
            break;
        }
    }

    let priorityName;
    for (let j = 0; j < Priorities.length; j++) {
        if (Priorities[j].id === priorityId) {
            priorityName = Priorities[j].name;
            break;
        }
    }

    return '<div id="note_' + id + '">'
        + '<hr/>'
        + rawNote
        + '<br/>'
        + '<br/>'
        + 'Created User: ' + createdUser + '<br/>'
        + 'Created Date: ' + createdDate + '<br/>'
        + 'Action: ' + actionName + '<br/>'
        + 'Priority: ' + priorityName
        + '<hr/></div>';
}

function BuildNotes() {
    $("#AddNote").data("kendoButton").enable(false);
    $.get("../api/GetCaseNoteByCaseKeyValueQuery?key=" +
        CaseKey +
        '&value=' +
        CaseKeyValue,
        function (data) {
            $.each(data,
                function (i, value) {
                    $("#notes").append(formatNote(data.id,
                        value.note,
                        value.createdUser,
                        value.createdDate,
                        value.actionId,
                        value.priorityId));
                });
        });
}

$(document).ready(function () {
    tabstrip = $("#tabstrip").kendoTabStrip({animation: false});
    MainTab = $("#MainTab").kendoTabStrip({animation: false});

    $("#editor").kendoEditor({
        keydown: function () {
            if ($("#editor").data("kendoEditor").value().length > 0) {
                $("#AddNote").data("kendoButton").enable(true);
            } else {
                $("#AddNote").data("kendoButton").enable(false);
            }
        },
        tools: [
            "bold",
            "italic",
            "underline",
            "undo",
            "redo",
            "justifyLeft",
            "justifyCenter",
            "justifyRight",
            "insertUnorderedList",
            "createLink",
            "unlink",
            "insertImage",
            "tableWizard",
            "createTable",
            "addRowAbove",
            "addRowBelow",
            "addColumnLeft",
            "addColumnRight",
            "deleteRow",
            "deleteColumn",
            "mergeCellsHorizontally",
            "mergeCellsVertically",
            "splitCellHorizontally",
            "splitCellVertically",
            "tableAlignLeft",
            "tableAlignCenter",
            "tableAlignRight",
            "formatting",
            {
                name: "fontName",
                items: [
                    {text: "Andale Mono", value: "\"Andale Mono\""}, // Font-family names composed of several words should be wrapped in \" \"
                    {text: "Arial", value: "Arial"},
                    {text: "Arial Black", value: "\"Arial Black\""},
                    {text: "Book Antiqua", value: "\"Book Antiqua\""},
                    {text: "Comic Sans MS", value: "\"Comic Sans MS\""},
                    {text: "Courier New", value: "\"Courier New\""},
                    {text: "Georgia", value: "Georgia"},
                    {text: "Helvetica", value: "Helvetica"},
                    {text: "Impact", value: "Impact"},
                    {text: "Symbol", value: "Symbol"},
                    {text: "Tahoma", value: "Tahoma"},
                    {text: "Terminal", value: "Terminal"},
                    {text: "Times New Roman", value: "\"Times New Roman\""},
                    {text: "Trebuchet MS", value: "\"Trebuchet MS\""},
                    {text: "Verdana", value: "Verdana"},
                ]
            },
            "fontSize",
            "foreColor",
            "backColor",
        ]
    });

    $("#AddNote").kendoButton();

    $("#AddNote").click(function () {
        $("#AddNote").data("kendoButton").enable(false);

        let rawNote = $("#editor").data("kendoEditor").value();

        let note = {
            note: rawNote,
            actionId: $("#Action").data("kendoDropDownList").value(),
            priorityId: $("#Priority").data("kendoDropDownList").value(),
            caseId: CaseId,
            caseKey: CaseKey,
            caseKeyValue: CaseKeyValue,
            payload: JSON.stringify(values)
        };

        $.ajax({
            url: "../api/CaseNote",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(note),
            success: function (data) {
                $("#notes").prepend(formatNote(data.id,
                    data.note,
                    data.createdUser,
                    data.createdDate,
                    data.actionId,
                    data.priorityId));

                $("#editor").data('kendoEditor').value('');
                $("#Action").data("kendoDropDownList").select(0);
                $("#Priority").data("kendoDropDownList").select(0);
            }
        });
    });

    $("#Priority").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $("#Action").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $("#Status").change(function () {
        UpdateCase(2);
    });

    $("#Peek").kendoButton();

    $("#Peek").click(function () {
        window.location.href = '/Case/CaseSearch';
    });

    $("#CaseFormSubmitButton").kendoButton();

    $("#Skim").kendoButton();

    $("#Skim").click(function () {
        tabstrip.select(GetTab(1));
        if (CaseId) {
            CaseId = null;
        }

        $('#journal').kendoGrid('destroy').empty();
        $('#notes').empty();
        $('#audit').kendoGrid('destroy').empty();
        $('#forms').kendoGrid('destroy').empty();
        const upload = $("#files").data('kendoUpload');
        upload.destroy();
        $(".k-upload").remove();
        GetCase();
    });

    $("#RefreshCaseJournal").click(function (e) {
        e.preventDefault();
        $("#Drilling").text('Fetching ' + CaseKey + ' = ' + CaseKeyValue);
        $("#Drilling").show();
        $('#journal').kendoGrid('destroy').empty();
        DrillName = CaseKey;
        DrillValue = CaseKeyValue;

        $.get("/api/GetCaseJournalQuery",
            {
                drillName: CaseKey,
                drillValue: CaseKeyValue,
                caseWorkflowGuid: CaseWorkflowGuid,
                limit: $("#Top").val(),
                activationsOnly: $("#ActivationsOnly").prop("checked"),
                responseElevation: $("#ResponseElevation").val()
            },
            function (data) {
                generateGridCase(data);
                $("#Drilling").hide();
            });
    });

    $("#Drill").click(function (e) {
        e.preventDefault();
        $("#Drilling").text('Fetching ' + DrillName + ' = ' + DrillValue);
        $("#Drilling").show();
        $('#journal').kendoGrid('destroy').empty();

        $.get("/api/GetCaseJournalQuery",
            {
                drillName: DrillName,
                drillValue: DrillValue,
                caseWorkflowGuid: CaseWorkflowGuid,
                limit: $("#Top").val(),
                activationsOnly: $("#ActivationsOnly").prop("checked"),
                responseElevation: $("#ResponseElevation").val()
            },
            function (data) {
                generateGridCase(data);
                $("#Drilling").hide();
            });
    });

    $("#Status").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $("#ClosedStatus").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value",
        change: function () {
            UpdateCase(0);
        }
    });

    $("#CaseFormSubmitButton").click(function (e) {
        e.preventDefault();
        const clonedArray = JSON.parse(JSON.stringify(values));
        const $inputs = jQuery('#CaseFormHTML :input');
        $inputs.each(function () {
            clonedArray[this.name] = jQuery(this).val();
        });

        const buttonObject = $("#CaseFormSubmitButton").kendoButton().data("kendoButton");
        buttonObject.enable(false);
        $("#PleaseWaitSpan").show();

        $.ajax({
            url: "../api/CaseWorkflowFormEntry",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({
                payload: JSON.stringify(clonedArray),
                caseWorkflowFormId: SelectedCasesWorkflowFormID,
                caseKey: CaseKey,
                caseKeyValue: CaseKeyValue,
                caseId: CaseId
            }),
            success: function () {
                $("#CaseFormResponse").show();
                $("#CaseFormSubmitDiv").hide();
                $("#CaseFormHTML").hide();

                $("#CaseFormResponse").css('color', 'green');
                $("#CaseFormResponse").html("Done.");

                $('#forms').data("kendoGrid").dataSource.read();
            }
        });
    });

    const refreshCaseJournal = $("#RefreshCaseJournal").kendoButton();
    refreshCaseJournal.width(70);

    $("#Drill").kendoButton();
    $("#Drill").data("kendoButton");
    $("#Drill").width(70);

    $("#ActivationsOnly").kendoSwitch();

    $("#Locked").kendoSwitch({
        change: function () {
            UpdateCase(0);
        },
        onLabel: "Yes",
        offLabel: "No",
    });

    $("#Diary").kendoSwitch({
        change: function () {
            UpdateCase(0);
        },
        onLabel: "Yes",
        offLabel: "No"
    });

    $("#Top").kendoNumericTextBox({
        format: "n",
        decimals: 0
    });

    $("#ResponseElevation").kendoNumericTextBox({
        format: "n",
        decimals: 0
    });

    $("#LockedUser").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value",
        change: function () {
            UpdateCase(0);
        }
    });

    $("#datetimepicker").kendoDateTimePicker({
        change: function () {
            UpdateCase(0);
        },
        value: new Date()
    });

    $("#CaseFormsMenu").kendoMenu(
        {
            orientation: 'vertical',
            select: onSelectCaseFormMenu
        }
    );

    $("#CaseDisplaysMenu").kendoMenu(
        {
            orientation: 'vertical',
            select: onSelectCaseDisplayMenu
        }
    );

    $("#Splitter").kendoSplitter({
        panes: [
            {collapsible: true, size: 300}
        ]
    });

    $("#CaseDisplaysSplitter").kendoSplitter({
        panes: [
            {collapsible: true, size: 300}
        ]
    });

    $('#Rate').barrating({
        theme: 'css-stars',
        onSelect: function (value, text, event) {
            if (event !== undefined) {
                UpdateCase(0);
            }
        }
    });

    CaseId = getUrlVars()["CaseId"];
    SessionCaseSearchCompiledSqlControllerGuid = getUrlVars()["SessionCaseSearchCompiledSqlControllerGuid"];

    if (!SessionCaseSearchCompiledSqlControllerGuid) {
        $('#Skim').hide();
    } else {
        $('#Skim').show();
    }

    GetCase();
    tabstrip = $("#tabstrip").kendoTabStrip().data("kendoTabStrip");
});

function LockCaseBar(lock) {
    const closed = $("#ClosedStatus").data("kendoDropDownList");
    const status = $("#Status").data("kendoDropDownList");
    const lockedUser = $("#LockedUser").data("kendoDropDownList");
    const picker = $("#datetimepicker").data("kendoDateTimePicker");
    const next = $("#Skim").data("kendoButton");
    const back = $("#Peek").data("kendoButton");
    if (lock) {
        closed.enable(false);
        $("#Locked").data("kendoSwitch").enable(false);
        $("#Diary").data("kendoSwitch").enable(false);
        lockedUser.enable(false);
        $('#Rate').barrating('readonly', true);
        picker.enable(false);
        status.enable(false);
        next.enable(false);
        back.enable(false);
    } else {
        closed.enable(true);
        $("#Locked").data("kendoSwitch").enable(true);
        $("#Diary").data("kendoSwitch").enable(true);
        lockedUser.enable(true);
        $('#Rate').barrating('readonly', false);
        status.enable(true);
        picker.enable(true);
        next.enable(true);
        back.enable(true);
    }
}

function UpdateCaseTimeout() {
    if (!PendingUpdateCaseInstruction) {
        $('#Updating').text("Updating");
        $('#Updating').show();
        $('#Updating').show();

        LockCaseBar(true);

        const autocomplete = $("#LockedUser").data("kendoDropDownList");
        const valuesJson = JSON.stringify(values);

        const data = {
            closedStatusId: $("#ClosedStatus").data("kendoDropDownList").value(),
            locked: $("#Locked").prop("checked"),
            lockedUser: autocomplete.value(),
            CaseWorkflowStatusGuid: $("#Status").data("kendoDropDownList").value(),
            diary: $("#Diary").prop("checked"),
            diaryDate: $("#datetimepicker").data("kendoDateTimePicker").value(),
            id: CaseId,
            rating: $('#Rate').val(),
            payload: valuesJson
        };

        $.ajax({
            url: "../api/Case",
            type: "PUT",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            error: function (jqXHR, textStatus, errorThrown) {
                if (jqXHR.status === 400) {
                    $("#ErrorMessage").show();
                    let responseObject = jQuery.parseJSON(jqXHR.responseText);
                    DisplayServerValidationErrors(responseObject);
                    FadeInAfterUpdate();
                } else {
                    $("#ErrorMessage").html(processingFailed);
                }
            },
            success: function () {
                if (PendingUpdateCaseInstructionRefreshDisplay === 1) {
                    $('#journal').kendoGrid('destroy').empty();
                    $('#notes').kendoGrid('destroy').empty();
                    $('#forms').kendoGrid('destroy').empty();
                    $('#audit').kendoGrid('destroy').empty();

                    const upload = $("#files").data('kendoUpload');
                    upload.destroy();

                    $(".k-upload").remove();

                    GetCase();
                } else if (PendingUpdateCaseInstructionRefreshDisplay === 2) {
                    GetCase();
                }
                FadeInAfterUpdate();
            }
        });
    }
}

function FadeInAfterUpdate() {
    $('#Updating').fadeOut();
    PendingUpdateCaseInstruction = false;
    LockCaseBar(false);
    clearTimeout();
}

function UpdateCase(refreshDisplay) {
    $("#ErrorMessage").hide();

    const next = $("#Skim").data("kendoButton");
    next.enable(false);

    const back = $("#Peek").data("kendoButton");
    back.enable(false);

    PendingUpdateCaseInstructionRefreshDisplay = refreshDisplay;
    if (typeof PendingUpdateCaseTimer !== "undefined") {
        clearTimeout(PendingUpdateCaseTimer);
    }
    PendingUpdateCaseTimer = setTimeout('UpdateCaseTimeout();', 1300);
}

function CallMacro(e) {
    $("#PleaseWaitMacro").show();
    const macrosLength = Macros.length;
    for (let i = 0; i < macrosLength; i++) {
        if (Macros[i].id === e) {
            try {
                const data = {
                    caseWorkflowMacroId: Macros[i].id,
                    payload: JSON.stringify(values)
                };

                $.ajax({
                    url: "../api/CaseWorkflowMacroExecution",
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    data: JSON.stringify(data),
                    success: function () {
                        alert("Done.");
                    }
                });

                eval(Macros[i].javascript);

            } catch (error) {
                console.log('Error: ' + error + ' for function ' + Macros[i].Javascript);
            }
            break;
        }
    }
    $("#PleaseWaitMacro").hide();
}

function NavigateCase(newCaseId) {
    CaseId = newCaseId;
    $("#ErrorOnUpdate").hide();
    $('#journal').kendoGrid('destroy').empty();
    $('#notes').empty();
    $('#audit').kendoGrid('destroy').empty();
    $('#forms').kendoGrid('destroy').empty();
    const upload = $("#files").data('kendoUpload');
    upload.destroy();
    $(".k-upload").remove();
    GetCase();
}

function GetCase() {
    $('#Updating').text("Fetching");
    $('#Updating').show();

    $.get("../api/UserInTenant",
        function (data) {
            $.each(data,
                function (i, value) {
                    let lockUserDropDown = $("#LockedUser");

                    let exists = false;
                    for (let i = 0; i < lockUserDropDown.getKendoDropDownList().dataSource.data().length; i++) {
                        let dataItem = lockUserDropDown.getKendoDropDownList().dataSource.data()[i];
                        if (dataItem.value === value.user) {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists) {
                        lockUserDropDown.getKendoDropDownList().dataSource.add({
                            "value": value.user,
                            "text": value.user
                        })
                    }
                });

            if (CaseId) {
                $.ajax({
                    url: "../api/GetCaseByIdQuery/" + CaseId,
                    type: 'GET',
                    success: function (data) {
                        SetOutCase(data);
                    },
                    error: function (xhr, status, error) {
                        //Not implemented.
                    }
                });
            } else {
                $.ajax({
                    url: "../api/GetCaseBySessionCaseSearchCompileQuery/" + SessionCaseSearchCompiledSqlControllerGuid,
                    type: 'GET',
                    success: function (data) {
                        SetOutCase(data);
                    },
                    error: function () {
                        id = setInterval(function () {
                                counter--;
                                if (counter < 0) {
                                    clearInterval(id);
                                    $('#Updating').hide();
                                    counter = 10;
                                    GetCase();
                                } else {
                                    $('#Updating').text("Nothing to skim retrying in " +
                                        counter.toString() +
                                        ".");
                                    $('#Updating').show();
                                }
                            },
                            1000);
                    }
                });
            }
        });
}

function SetOutCase(data) {
    if (!data) {

    } else {
        $('#Updating').text("");

        CaseKey = data.caseKey;
        CaseKeyValue = data.caseKeyValue;
        DrillName = CaseKey;
        DrillValue = CaseKeyValue;
        CaseId = data.id;
        CaseWorkflowGuid = data.caseWorkflowGuid;

        switch (data.lastClosedStatus) {
            case 0:
                $('#CaseStatusColour').css("color", "red");
                break;
            case 1:
                $('#CaseStatusColour').css("color", "blue");
                break;
            case 2:
                $('#CaseStatusColour').css("color", "green");
                break;
            case 3:
                $('#CaseStatusColour').css("color", "yellow");
                break;
            case 4:
                $('#CaseStatusColour').css("color", "silver");
                break;
            default:
                $('#CaseStatusColour').css("color", "teal");
        }

        $("#ClosedStatus").data("kendoDropDownList").value(data.closedStatusId);

        if (data.locked) {
            $("#Locked").data("kendoSwitch").check(true);
        } else {
            $("#Locked").data("kendoSwitch").check(false);
        }

        if (data.diary) {
            $("#Diary").data("kendoSwitch").check(true);
        } else {
            $("#Diary").data("kendoSwitch").check(false);
        }

        $("#datetimepicker").data("kendoDateTimePicker").value(data.diaryDate);
        $("#ClosedUser").html(data.closedUser);
        $("#LockedUser").data("kendoDropDownList").value(data.lockedUser);
        $("#DiaryUser").html(data.diaryUser);

        if (data.lastClosedStatus === 0) {
            $("#LastClosedStatus").html('Open');
        } else if (data.lastClosedStatus === 1) {
            $("#LastClosedStatus").html('Suspend Open');
        } else if (data.lastClosedStatus === 2) {
            $("#LastClosedStatus").html('Suspend Closed');
        } else if (data.lastClosedStatus === 3) {
            $("#LastClosedStatus").html('Closed');
        }

        $("#CaseId").text(data.id);
        $("#CaseKey").text(data.caseKey);
        $("#CaseKeyValue").text(data.caseKeyValue);
        $('#Rate').barrating('set', data.rating);

        $("#StatusTable").css('background-color', data.backColor);
        $("#StatusTable").css('color', data.foreColor);

        createFilesUpload();
        createActivations(data.activation);

        const ddl = $("#Status").data("kendoDropDownList");
        ddl.dataSource.data([]);
        ddl.text("");
        ddl.value("");

        Status = data.caseWorkflowStatusGuid;

        $.get("../api/CaseWorkflowStatus/ByCasesWorkflowGuidActiveOnly/" + CaseWorkflowGuid,
            function (data) {
                $.each(data,
                    function (i, value) {
                        $("#Status").getKendoDropDownList().dataSource.add({
                            "value": value.guid,
                            "text": value.name
                        });
                    });

                const dropdownlist = $("#Status").data("kendoDropDownList");

                dropdownlist.select(function (dataItem) {
                    return dataItem.value === Status;
                });
            });

        ResponsePayload = data.formattedPayload;


        if (Actions === undefined) {
            $.get("../api/CaseWorkflowPriority/ByCasesWorkflowGuidActiveOnly/" + CaseWorkflowGuid,
                function (data) {
                    Priorities = data;

                    $.each(data,
                        function (i, value) {
                            $("#Priority").getKendoDropDownList().dataSource.add({
                                "value": value.id,
                                "text": value.name
                            })
                        });

                    $.get("../api/CaseWorkflowAction/ByCasesWorkflowGuidActiveOnly/" + CaseWorkflowGuid,
                        function (data) {
                            Actions = data;

                            $.each(data,
                                function (i, value) {
                                    $("#Action").getKendoDropDownList().dataSource.add({
                                        "value": value.id,
                                        "text": value.name
                                    })
                                });

                            BuildNotes();
                        });
                });
        } else {
            BuildNotes();
        }

        if (Macros === undefined) {
            $.get("../api/CaseWorkflowMacro/ByCasesWorkflowGuidActiveOnly/" + CaseWorkflowGuid,
                function (data) {
                    Macros = data;
                    for (let i = 0; i < data.length; i++) {
                        const imageLocation = '../icons/' +
                            data[i].imageLocation;
                        const onClickJavaScript = 'CallMacro(' + data[i].id + ');';
                        $("#Icons").append($('<img>',
                            {
                                id: data[i].id,
                                src: imageLocation,
                                alt: data[i].name,
                                class: 'icon',
                                onclick: onClickJavaScript,
                                title: data[i].name
                            }));
                    }
                });
        }

        $.get("../api/CaseWorkflowXPath/ByCasesWorkflowGuidActiveOnlyDrill/" + CaseWorkflowGuid,
            function (data) {
                Drills = data;
            });

        dataSourceCases = new kendo.data.DataSource({
            transport: {
                read: {
                    url: "../api/GetCaseByCaseKeyValueQuery?key=" +
                        CaseKey +
                        '&value=' +
                        CaseKeyValue,
                    dataType: "json"
                },
                parameterMap: function (options, operation) {
                    if (operation !== "read" && options.models) {
                        return {models: kendo.stringify(options.models)};
                    }
                }
            },
            batch: true,
            pageSize: 20,
            schema: {
                model: {
                    id: "id",
                    fields: {
                        id: {type: "string"},
                        closedUser: {type: "string"},
                        caseKey: {type: "string"},
                        caseKeyValue: {type: "string"},
                        locked: {type: "boolean"},
                        lockedUser: {type: "string"},
                        caseWorkflowStatusName: {type: "string"},
                        diary: {type: "boolean"},
                        diaryUser: {type: "string"},
                        diaryDate: {type: "date"},
                        createdDate: {type: "date"},
                        closedStatusId: {type: "number"}
                    }
                }
            }
        });

        $("#cases").kendoGrid({
            dataSource: dataSourceCases,
            pageable: false,
            height: 446,
            scrollable: true,
            filterable: false,
            columns: [
                {
                    field: "id",
                    title: "Id",
                    template:
                        "<a href='\\#' alt='' onclick='NavigateCase(#=id#)'>#=id#</a>"
                },
                {
                    field: "closedStatusId",
                    title: "Closed",
                    template: "#= closedStatusId == 3 ? 'Yes' : 'No' #"
                },
                {field: "closedUser", title: "Closed User"},
                {field: "locked", title: "Locked", template: "#= locked ? 'Yes' : 'No' #"},
                {field: "lockedUser", title: "Locked User"},
                {field: "caseWorkflowStatusName", title: "Workflow"},
                {field: "diary", title: "Diary", template: "#= diary ? 'Yes' : 'No' #"},
                {field: "diaryUser", title: "Diary User"},
                {field: "diaryDate", title: "Diary Date"}
            ]
        });

        const dataSourceAudit = new kendo.data.DataSource({
            transport: {
                read: {
                    url: "../api/GetCaseEventByCaseKeyValueQuery?key=" +
                        CaseKey +
                        '&value=' +
                        CaseKeyValue,
                    dataType: "json"
                },
                parameterMap: function (options, operation) {
                    if (operation !== "read" && options.models) {
                        return {models: kendo.stringify(options.models)};
                    }
                }
            },
            batch: true,
            pageSize: 20,
            schema: {
                model: {
                    id: "id",
                    fields: {
                        caseEventType: {type: "string"},
                        before: {type: "string"},
                        after: {type: "string"},
                        caseId: {type: "number"},
                        createdUser: {type: "string"},
                        createdDate: {type: "string"}
                    }
                }
            }
        });

        const dataSourceForms = new kendo.data.DataSource({
            transport: {
                read: {
                    url: "/api/GetCaseWorkflowFormEntryByCaseKeyValueQuery?key=" +
                        CaseKey +
                        "&value=" +
                        CaseKeyValue,
                    dataType: "json"
                },
                parameterMap: function (options, operation) {
                    if (operation !== "read" && options.models) {
                        return {models: kendo.stringify(options.models)};
                    }
                }
            },
            batch: true,
            pageSize: 20,
            schema: {
                model: {
                    id: "id",
                    fields: {
                        name: {type: "string"},
                        caseId: {type: "string"},
                        createdUser: {type: "string"},
                        createdDate: {type: "string"}
                    }
                }
            }
        });

        $("#audit").kendoGrid({
            dataSource: dataSourceAudit,
            height: 446,
            scrollable: true,
            filterable: false,
            columns: [
                {field: "caseEventType", title: "Event"},
                {field: "before", title: "Before"},
                {field: "after", title: "After"},
                {
                    field: "id",
                    title: "Id",
                    template:
                        "<a href='\\#' onclick='NavigateCase(#=caseId#)'>#=caseId#</a>"
                },
                {field: "createdUser", title: "Created User"},
                {field: "createdDate", title: "Created Date"}
            ]
        });

        $("#forms").kendoGrid({
            dataSource: dataSourceForms,
            height: 446,
            scrollable: true,
            filterable: false,
            detailInit: detailInit,
            columns: [
                {field: "name", title: "Form"},
                {
                    field: "caseId",
                    title: "Case ID",
                    template:
                        "<a href='\\#' onclick='NavigateCase(#=caseId#)'>#=caseId#</a>"
                },
                {field: "createdUser", title: "User"},
                {field: "createdDate", title: "Created Date"}
            ]
        });

        function detailInit(e) {
            $("<div/>").appendTo(e.detailCell).kendoGrid({
                dataSource: {
                    transport: {
                        read: {
                            dataType: "json",
                            url:
                                "../api/CaseWorkflowFormEntryValue/ByCaseWorkflowFormEntryId?id=" +
                                e.data.id
                        },
                        schema: {
                            model: {
                                fields: {
                                    name: {
                                        type: "string"
                                    },
                                    value: {
                                        type: "string"
                                    }
                                }
                            }
                        }
                    }
                },
                columns: [
                    {
                        field: "name",
                        title: "Name",
                        width: "110px"
                    },
                    {
                        field: "value",
                        title: "Value",
                        width: "110px"
                    }
                ]
            });
        }

        $("#Drilling").text('Fetching ' + CaseKey + ' = ' + CaseKeyValue);
        $("#Drilling").show();

        $.get("/api/GetCaseJournalQuery",
            {
                drillName: CaseKey,
                drillValue: CaseKeyValue,
                caseWorkflowGuid: CaseWorkflowGuid,
                limit: $("#Top").val(),
                activationsOnly: $("#ActivationsOnly").prop("checked"),
                responseElevation: $("#ResponseElevation").val()
            },
            function (data) {
                generateGridCase(data);

                $("#Drilling").hide();

                $("#CaseFormSubmitDiv").hide();

                const menuForms = $("#CaseFormsMenu").data("kendoMenu");
                if (Forms === undefined) {
                    $.get("../api/CaseWorkflowForm/ByCasesWorkflowGuidActiveOnly/" + CaseWorkflowGuid,
                        function (data) {
                            Forms = data;

                            $("#CaseFormsMenu>li").each(function () {
                                menuForms.remove(this);
                            });

                            $.each(data,
                                function (i, value) {
                                    menuForms.append([
                                        {
                                            text: value.name,
                                            attr: {
                                                'data-item-key': value.id
                                            }
                                        }
                                    ]);
                                });
                        });
                } else {
                    $("#CaseFormsMenu>li").each(function () {
                        menuForms.remove(this);
                    });

                    $.each(Forms,
                        function (i, value) {
                            menuForms.append([
                                {
                                    text: value.name,
                                    attr: {
                                        'data-item-key': value.id
                                    }
                                }
                            ]);
                        });
                }

                const menuDisplays = $("#CaseDisplaysMenu").data("kendoMenu");
                if (Displays === undefined) {
                    $.get("../api/CaseWorkflowDisplay/ByCasesWorkflowGuidActiveOnly/" + CaseWorkflowGuid,
                        function (data) {
                            Displays = data;
                            $("#CaseDisplaysMenu>li").each(function () {
                                menuDisplays.remove(this);
                            });

                            $.each(data,
                                function (i, value) {
                                    menuDisplays.append([
                                        {
                                            text: value.name,
                                            attr: {
                                                'data-item-key': value.id
                                            }
                                        }
                                    ]);
                                });

                            menuDisplays.append([
                                {
                                    text: 'Default',
                                    attr: {
                                        'data-item-key': 'Default'
                                    }
                                }
                            ]);
                        });
                } else {
                    $("#CaseDisplaysMenu>li").each(function () {
                        menuDisplays.remove(this);
                    });

                    $.each(Displays,
                        function (i, value) {
                            menuDisplays.append([
                                {
                                    text: value.name,
                                    attr: {
                                        'data-item-key': value.id
                                    }
                                }
                            ]);
                        });

                    menuDisplays.append([
                        {
                            text: 'Default',
                            attr: {
                                'data-item-key': 'Default'
                            }
                        }
                    ]);
                }
                DisplayMenu('Default');
            });

        if (data.enableVisualisation) {
            ShowParams = false;
            VisualisationRegistryGuid = data.visualisationRegistryGuid;
            InitVisualisation();
        }
    }
}

function onSelectCaseFormMenu(e) {
    SelectedCasesWorkflowFormID = $(e.item).data('item-key');

    $.each(Forms,
        function (i, value) {
            if (value.id === SelectedCasesWorkflowFormID) {
                $("#CaseFormSubmitDiv").show();
                $("#CaseFormHTML").show();
                $("#PleaseWaitSpan").hide();
                $("#CaseFormResponse").hide();

                const buttonObject = $("#CaseFormSubmitButton").kendoButton().data("kendoButton");
                buttonObject.enable(true);

                $("#CaseFormHTML").html(value.html);
                return false;
            }
        });
}

function DisplayMenu(selectedCasesWorkflowDisplayId) {
    if (selectedCasesWorkflowDisplayId === 'Default') {
        let tableBackColor;
        let tableForeColor;

        let html = '<div><table id="PayloadTable" style="width: 90%">';
        values = {};
        values["CaseKey"] = CaseKey;
        values["CaseKeyValue"] = CaseKeyValue;

        $.each(ResponsePayload,
            function (key, value) {
                values[value.name] = value.value;

                if (value.conditionalRegularExpressionFormatting) {
                    if (value.cellFormatForeRow === true && value.cellFormatBackRow === true && value.existsMatch === true) {
                        html += '<tr>';
                        tableBackColor = value.cellFormatBackColor;
                        tableForeColor = value.cellFormatForeColor;
                    } else if (value.cellFormatForeRow === false && value.cellFormatBackRow === true && value.existsMatch === true) {
                        html += '<tr style="color:' + value.cellFormatForeColor + '">';
                        tableBackColor = value.cellFormatBackColor;
                    } else if (value.cellFormatForeRow === true && value.cellFormatBackRow === false && value.existsMatch === true) {
                        html += '<tr style="background-color:' + value.cellFormatForeColor + '">';
                        tableForeColor = value.cellFormatBackColor;
                    } else if (value.existsMatch === true) {
                        html += '<tr style="background-color:' +
                            value.cellFormatBackColor +
                            ';color:' +
                            value.cellFormatForeColor +
                            '">';
                    }
                } else {
                    html += '<tr>';
                }
                html += '<td style="width: 200px">' + value.name + ':</td>';
                html += '<td>' + value.value + '</td>';
                html += '</tr>';
            });
        html += '</table></div>';

        $('#CaseDisplayHTML').html(html);
        $("#PayloadTable").css('background-color', tableBackColor);
        $("#PayloadTable").css('color', tableForeColor);
    } else {
        $.each(Displays,
            function (i, displayValue) {
                if (displayValue.id === selectedCasesWorkflowDisplayId) {
                    $("#CaseDisplayHTML").show();

                    let finalHtml = displayValue.html;
                    const tokens = displayValue.html.match(/\[@(.*?)]/g);
                    $.each(tokens,
                        function (j, tokenValue) {
                            const token = tokenValue.slice(2, -2);
                            $.each(ResponsePayload,
                                function (k, responsePayloadValue) {
                                    if (token === responsePayloadValue.name) {
                                        finalHtml = finalHtml.replace(tokens[j], responsePayloadValue.value);
                                    }
                                });
                        });

                    $("#CaseDisplayHTML").html(finalHtml);
                    return false;
                }
            });
    }

}

function onSelectCaseDisplayMenu(e) {
    SelectedCasesWorkflowDisplayID = $(e.item).data('item-key');
    DisplayMenu(SelectedCasesWorkflowDisplayID);
}

function getUrlVars() {
    const vars = [];
    let hash;
    const hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (let i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

function onChanged() {
    const cell = this.select();
    const cellIndex = cell[0].cellIndex;
    const column = this.columns[cellIndex];
    const dataItem = this.dataItem(cell.closest("tr"));
    DrillName = column.field;
    DrillValue = dataItem[column.field];

    const arrayLength = Drills.length;
    let found = 0;
    for (let i = 0; i < arrayLength; i++) {
        if (DrillName === Drills[i].name) {
            found = 1;
        }
    }

    const button = $("#Drill").data("kendoButton");
    if (found === 1) {
        button.enable(true);
    } else {
        button.enable(false);
    }
}

function generateGridCase(gridData) {
    let parseFunction;
    if (dateFields.length > 0) {
        // noinspection JSUnusedAssignment
        parseFunction = function (response) {
            for (let i = 0; i < response.length; i++) {
                for (let fieldIndex = 0; fieldIndex < dateFields.length; fieldIndex++) {
                    const record = response[i];
                    record[dateFields[fieldIndex]] = kendo.parseDate(record[dateFields[fieldIndex]]);
                }
            }
            return response;
        };
    }

    const model = generateModel(gridData[0]);
    const columns = generateColumns(gridData[0]);

    $("#journal").kendoGrid({
        toolbar: ["excel"],
        groupable: true,
        excel: {
            fileName: "Case Key Journal " + CaseKey + ".xlsx",
            proxyURL: "https://proxy.jube.io",
            filterable: true,
            allPages: true
        },
        selectable: "cell",
        dataSource: {
            data: gridData,
            pageSize: 10,
            schema: {
                model: model
            }
        },
        dataBound: SetPlacementColor,
        change: onChanged,
        height: 446,
        scrollable: true,
        columns: columns,
        filterable: true,
        sortable: true,
        resizable: true,
        reorderable: true,
        pageable: {
            refresh: false,
            pageSizes: true,
            buttonCount: 5
        },
        columnResize: function () {
            const that = this;
            setTimeout(function () {
                    SaveCaseKeyJournalSession(that.columns);
                },
                5);
        },
        columnReorder: function () {
            const that = this;
            setTimeout(function () {
                    SaveCaseKeyJournalSession(that.columns);
                },
                5);
        }
    });
}

function GetCellPosition(columns, name) {
    for (let i = 0; i < columns.length; i++) {
        if (columns[i].field === name) {
            return i;
        }
    }
}

function SetPlacementColor() {
    try {
        const dataElement = $("#journal").find(".k-grid-content");
        $("#journal").find(".k-grid-toolbar").insertAfter(dataElement);
        const fakeScroll = $("#dummyScroll");
        fakeScroll.width(dataElement.children(0).width() + 'px');

        dataElement.scroll(function () {
            $("#dummyScrollWrapper").scrollLeft(dataElement.scrollLeft());
        });

        $("#dummyScrollWrapper").scroll(function () {
            dataElement.scrollLeft($("#dummyScrollWrapper").scrollLeft());
        });

        const grid = $('#journal').data('kendoGrid');

        const rows = grid.tbody.children();
        for (let j = 0; j < rows.length; j++) {
            const row = $(rows[j]);
            const dataItem = grid.dataItem(row);

            const boldLine = dataItem.get("BoldLine");
            if (boldLine) {
                const boldLineMatchOnKey = dataItem.get(boldLine.boldLineKey);
                // noinspection JSReferencingMutableVariableFromClosure
                const matchValue = ResponsePayload.find(element => element.name === boldLine.boldLineKey).value;

                if (boldLineMatchOnKey === matchValue) {
                    row.css("color", boldLine.boldLineFormatForeColor);
                    row.css("background-color", boldLine.boldLineFormatBackColor);
                    row.css("font-weight", "bold");
                }
            }

            const cellFormats = dataItem.get("CellFormat");
            if (cellFormats) {
                for (let c = 0; c < cellFormats.length; c++) {
                    const cell = cellFormats[c];
                    const position = GetCellPosition(grid.columns, cell.cellFormatKey);
                    const td = row.children()[position];
                    if (cell.cellFormatForeRow === true && cell.cellFormatBackRow === true) {
                        row.css("color", cell.cellFormatForeColor);
                        row.css("background-color", cell.cellFormatBackColor);
                    } else if (cell.cellFormatForeRow === false && cell.cellFormatBackRow === true) {
                        td.style.color = cell.cellFormatForeColor;
                        row.css("background-color", cell.cellFormatBackColor);
                    } else if (cell.cellFormatForeRow === true && cell.cellFormatBackRow === false) {
                        td.bgColor = cell.cellFormatBackColor;
                        row.css("color", cell.cellFormatForeColor);
                    } else {
                        td.bgColor = cell.cellFormatBackColor;
                        td.style.color = cell.cellFormatForeColor;
                    }
                }
            }
        }
    } catch (err) {
        console.log(err.message);
    }
}

function onSuccess(e) {
    const file0Uid = e.files[0].uid;
    $(".k-file[data-uid='" + file0Uid + "']").find(".k-file-name")
        .html("<a target=\"_blank\" href='../api/CaseFile?id=" + e.response.id + "'>" + e.files[0].name + "</a>");
}

function onUpload(e) {
    e.data = {
        caseKey: CaseKey,
        caseKeyValue: CaseKeyValue,
        caseId: CaseId
    };
}

function onRemove(e) {
    e.data = {
        id: e.files[0].id
    };
}

function createFilesUpload() {
    $('<input type="file" name="files" id="files">').insertBefore("#PlaceholderFiles");

    $.get("/api/CaseFile/ByCaseKeyValue",
        {
            key: CaseKey,
            value: CaseKeyValue
        },
        function (data) {
            $("#files").kendoUpload({
                async: {
                    saveUrl: "../api/CaseFile/Upload",
                    removeUrl: "../api/CaseFile/Remove",
                    multiple: true
                },
                files: data,
                upload: onUpload,
                remove: onRemove,
                success: onSuccess,
                template: kendo.template($('#fileTemplate').html())
            }).data("kendoUpload");
        });
}

function createActivations(activationsData) {
    $("#Activations").kendoGrid({
        dataSource: {
            data: activationsData,
            schema: {
                model: {
                    fields: {
                        name: {type: "string"}
                    }
                }
            },
            pageSize: 20
        },
        height: 446,
        scrollable: true,
        sortable: true,
        filterable: false,
        pageable: false,
        columns: [
            {field: "name", title: "Name"}
        ]
    });
}

function customEditor(container, options) {
    $('<textarea required name="' + options.field + '" style="height: 100px; width: 100%;" />')
        .appendTo(container);
    /* .kendoAutoComplete({
          minLength: 3,
          dataTextField: "NoteSuggestion",
          filter: "contains",
          dataSource: {
              type: "json",
              serverFiltering: true,
              transport: { read: "/Service/GetNoteSuggestions.ashx" }
          }
      });*/
}

function SaveCaseKeyJournalSession(columns) {
    $.ajax({
        url: "../api/SessionCaseJournal",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            caseWorkflowGuid: caseWorkflowGuid,
            json: JSON.stringify(columns)
        }),
        success: function (data) {
            //Ignore
        }
    });
}

function ExistsColumnInGridData(name, gridData) {
    let found = false;
    for (let property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            if (property === name) {
                found = true;
                break;
            }
        }
    }
    return found;
}

function ExistsColumnInNewColumns(name, columns) {
    let found = false;
    for (let property in columns) {
        if (Object.prototype.hasOwnProperty.call(columns, property)) {
            if (columns[property].field === name) {
                found = true;
                break;
            }
        }
    }
    return found;
}

function isHidden(name) {
    if (name === 'BoldLine') {
        return true;
    } else return name === 'CellFormat';
}

function generateColumns(gridData) {
    const columns = [];

    $.ajax({
        url: "../api/SessionCaseJournal/ByCasesWorkflowGuid/" + CaseWorkflowGuid,
        type: 'GET',
        async: false,
        cache: false,
        timeout: 30000,
        error: function () {
            return true;
        },
        success: function (data) {
            let column;
            let property;
            if (data) {
                const savedColumns = JSON.parse(data.json);
                for (property in savedColumns) {
                    if (Object.prototype.hasOwnProperty.call(savedColumns, property)) {
                        if (ExistsColumnInGridData(savedColumns[property].field, gridData)) {
                            column = {};
                            column["width"] = savedColumns[property].width;
                            column["field"] = savedColumns[property].field;
                            column["title"] = savedColumns[property].title;

                            if (isHidden(savedColumns[property].field)) {
                                column["hidden"] = true;
                            }
                            columns.push(column);
                        }
                    }
                }

                for (property in gridData) {
                    if (Object.prototype.hasOwnProperty.call(gridData, property)) {
                        if (ExistsColumnInNewColumns(property, columns) !== true) {
                            column = {};
                            column["width"] = "400px;";
                            column["field"] = property;
                            column["title"] = property;
                            if (isHidden(property)) {
                                column["hidden"] = true;
                            }
                            columns.push(column);
                        }
                    }
                }
            } else {
                for (property in gridData) {
                    if (Object.prototype.hasOwnProperty.call(gridData, property)) {
                        column = {};
                        column["width"] = "400px;";
                        column["field"] = property;
                        column["title"] = property;

                        if (isHidden(property)) {
                            column["hidden"] = true;
                        }
                        columns.push(column);
                    }
                }
            }
        }
    });

    return columns;
}

function generateModel(gridData) {
    const model = {};
    model.id = "Id";
    const fields = {};
    for (let property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            const propType = typeof gridData[property];

            if (propType === "number") {
                fields[property] = {
                    type: "number",
                    validation: {
                        required: true
                    }
                };
            } else if (propType === "boolean") {
                fields[property] = {
                    type: "boolean",
                    validation: {
                        required: true
                    }
                };
            } else if (propType === "string") {
                const parsedDate = new Date(gridData[property]);
                if (isNaN(parsedDate.getTime)) {
                    fields[property] = {
                        validation: {
                            required: true
                        }
                    };
                } else {
                    fields[property] = {
                        type: "date",
                        validation: {
                            required: true
                        }
                    };
                    dateFields.push(property);
                }
            } else {
                fields[property] = {
                    validation: {
                        required: true
                    }
                };
            }

        }
    }
    model.fields = fields;

    return model;
}

function GetTab(target) {
    return tabstrip.tabGroup.children("li").eq(target);
}

function DisplayServerValidationErrors(responseObject) {
    let errorMessage = $("#ErrorMessage");
    errorMessage.html("Server validation errors occured:").append('<br/>')
    let list = errorMessage.append("<ul>");
    for (let key in responseObject.errors) {
        list.append('<li>' + responseObject.errors[key].errorMessage + '</li>')
    }
}