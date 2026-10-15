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

let CurrentConversationId = null;
let ThinkingIndicator = null;
let ThinkingIndicatorTick = null;
let PendingAttachments = [];

const ConversationStreamState = new Map();

let ActiveSelectionRow = null;

let HasRealExchange = false;

let AskJoobyTranscript = null;
let AskJoobyInput = null;
let AskJoobyConversations = null;
let AskJoobyStopButton = null;
let AskJoobySendButton = null;
let AskJoobyConversationSearch = null;
let AskJoobyUpload = null;

const AskJoobyOffline = window.AskJoobyHasInferenceBackend === false;
const Connection = AskJoobyCreateConnection();
const ConnectionReady = AskJoobyOffline ? Promise.resolve(Connection) : Connection.start();

function WaitForConnectedAsync() {
    if (Connection.state === AskJoobyConnectionState.Connected) {
        return Promise.resolve(Connection);
    }

    if (Connection.state === AskJoobyConnectionState.Disconnected) {
        return ConnectionReady.then(function () {
            return Connection;
        });
    }

    return new Promise(function (resolve, reject) {
        Connection.onreconnected(function () {
            resolve(Connection);
        });
        Connection.onclose(function (error) {
            reject(error || new Error("Connection closed."));
        });
    });
}

function FetchOfflineWelcomeAsync() {
    return $.get("/api/AskJoobyConversation/StartupStats")
        .then(function (stats) {
            return AskJoobyPickOfflineWelcome(stats);
        }, function () {
            return AskJoobyPickOfflineWelcome(null);
        });
}

function AskJoobyRelativeTime(value) {
    if (!value) {
        return "";
    }

    const then = new Date(value).getTime();
    const seconds = Math.max(0, Math.floor((Date.now() - then) / 1000));

    if (seconds < 60) {
        return "just now";
    }

    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
        return minutes + "m ago";
    }

    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
        return hours + "h ago";
    }

    const days = Math.floor(hours / 24);
    return days + "d ago";
}

function LoadConversations() {
    AskJoobyConversations.data("kendoListView").dataSource.read();
}

let TranscriptFollowingEnd = true;

function TranscriptIsAtEnd(element) {
    return element.scrollHeight - element.scrollTop - element.clientHeight <= 48;
}

function ScrollTranscriptToBottom(force) {
    const transcript = AskJoobyTranscript;
    if (!transcript.length) {
        return;
    }

    if (!force && !TranscriptFollowingEnd) {
        return;
    }

    transcript.scrollTop(transcript[0].scrollHeight);
    TranscriptFollowingEnd = true;
}

function BuildAuditLink(labelText, urlSuffix) {
    const link = $("<a/>").attr("href", "#").text(labelText);
    link.on("click", function (e) {
        if (CurrentConversationId === null) {
            e.preventDefault();
            return;
        }

        $(this).attr("href", "/api/AskJoobyConversation/" + CurrentConversationId + urlSuffix);
    });

    return link;
}

function PinAuditLinkToEnd(endDate) {
    $("#askJoobyTranscript > .askJoobyAuditLinkRow").remove();

    if (!HasRealExchange) {
        return;
    }

    const row = $("<div/>").addClass("askJoobyAuditLinkRow");

    const time = AskJoobyFormatTimestamp(endDate);
    if (time !== "") {
        row.append($("<span/>").addClass("askJoobyTranscriptStamp").text("Last updated " + time));
    }

    const links = $("<span/>").addClass("askJoobyAuditLinks");

    links.append(BuildAuditLink("Download Chat Transcript", "/Transcript"));

    if (window.AskJoobyIsLandlord === true) {
        links.append(BuildAuditLink("Download Chat Technical Audit", "/Audit"));
    }

    row.append(links);
    AskJoobyTranscript.append(row);
}

function AppendToTranscript(element) {
    AskJoobyTranscript.append(element);
    PinAuditLinkToEnd(new Date());
}

function PinTranscriptStart(startDate) {
    $("#askJoobyTranscript > .askJoobyTranscriptStart").remove();

    const time = AskJoobyFormatTimestamp(startDate);
    if (time === "") {
        return;
    }

    AskJoobyTranscript.prepend($("<div/>").addClass("askJoobyTranscriptStart").text("Started " + time));
}

function ShowThinking() {
    if (ThinkingIndicator !== null) {
        return;
    }

    ThinkingIndicator = $(
        "<div class=\"askJoobyThinking\">" +
        "<span class=\"askJoobyThinkingBuffalo\" aria-hidden=\"true\" title=\"Gallopy gallopy gallopy\"></span>" +
        "<span class=\"askJoobyThinkingWord askJoobyThinkingDot1\">Gallopy</span>" +
        "<span class=\"askJoobyThinkingWord askJoobyThinkingDot2\">Gallopy</span>" +
        "<span class=\"askJoobyThinkingWord askJoobyThinkingDot3\">Gallopy</span>" +
        "<span class=\"askJoobyThinkingElapsed\"></span></div>");
    AppendToTranscript(ThinkingIndicator);

    const startedAt = Date.now();
    const elapsed = ThinkingIndicator.find(".askJoobyThinkingElapsed");
    ThinkingIndicatorTick = setInterval(function () {
        elapsed.text(Math.round((Date.now() - startedAt) / 1000) + "s");
    }, 1000);
    ScrollTranscriptToBottom();
}

function HideThinking() {
    if (ThinkingIndicator === null) {
        return;
    }

    if (ThinkingIndicatorTick !== null) {
        clearInterval(ThinkingIndicatorTick);
        ThinkingIndicatorTick = null;
    }

    ThinkingIndicator.remove();
    ThinkingIndicator = null;
}

function AppendMessage(role, text, inputTokens, outputTokens, durationMs, attachments, messageId, isGreeting,
                       timestamp, cachedInputTokens, joobrikCount, distinctJoobrikCount, totalCost) {
    if (role === "User") {
        HasRealExchange = true;
    }

    HideThinking();
    const row = $("<div/>").addClass("askJoobyRow").addClass("askJoobyRow" + role);
    const avatar = $("<div/>").addClass("askJoobyAvatar").addClass("askJoobyAvatar" + role);
    if (role === "User") {
        avatar.text("U");
    }

    const wrap = $("<div/>").addClass("askJoobyBubbleWrap");
    const bubble = $("<div/>").addClass("askJoobyBubble").addClass("askJoobyBubble" + role);
    bubble.append(AskJoobyRenderMarkdown(text));
    PrependAttachments(bubble, attachments);
    wrap.append(bubble);
    AppendMeta(wrap, timestamp, inputTokens, outputTokens, durationMs, cachedInputTokens, joobrikCount,
        distinctJoobrikCount, totalCost);
    if (role === "Assistant" && messageId && !isGreeting) {
        AppendFeedbackControls(wrap, messageId);
    }

    row.append(avatar).append(wrap);
    AppendToTranscript(row);
    ScrollTranscriptToBottom(role === "User");
    return bubble;
}

function AppendFeedbackControls(wrap, messageId) {
    wrap.find(".askJoobyFeedback").remove();

    const feedback = $("<div/>").addClass("askJoobyFeedback");
    const up = $("<button/>").attr("type", "button").addClass("askJoobyFeedbackButton")
        .attr("aria-label", "Good response, worth remembering").text("👍");
    const down = $("<button/>").attr("type", "button").addClass("askJoobyFeedbackButton")
        .attr("aria-label", "Bad response, worth remembering as an example to avoid").text("👎");

    function submit(isPositive, clicked, other) {
        clicked.prop("disabled", true).addClass("askJoobyFeedbackChosen");
        other.prop("disabled", true);

        WaitForConnectedAsync().then(function () {
            return Connection.invoke("SubmitFeedbackAsync", CurrentConversationId, messageId, isPositive);
        }).then(function (confirmation) {
            AppendActivity("askJoobyStep", confirmation);
        }, function (error) {
            clicked.prop("disabled", false).removeClass("askJoobyFeedbackChosen");
            other.prop("disabled", false);
            AppendActivity("askJoobyError", "Couldn't save feedback: " + (error && error.message ? error.message : error));
        });
    }

    up.on("click", function () {
        submit(true, up, down);
    });
    down.on("click", function () {
        submit(false, down, up);
    });

    feedback.append(up).append(down);
    wrap.append(feedback);
}

function AppendMeta(wrap, timestamp, inputTokens, outputTokens, durationMs, cachedInputTokens, joobrikCount,
                    distinctJoobrikCount, totalCost) {
    const parts = [];
    const time = AskJoobyFormatTimestamp(timestamp);
    if (time !== "") {
        parts.push(time);
    }

    const stats = AskJoobyFormatMeta(inputTokens, outputTokens, durationMs, cachedInputTokens, joobrikCount,
        distinctJoobrikCount, totalCost);
    if (stats !== "") {
        parts.push(stats);
    }

    wrap.find(".askJoobyMeta").remove();
    if (parts.length === 0) {
        return;
    }

    wrap.append($("<div/>").addClass("askJoobyMeta").text(parts.join(" · ")));
}

function PrependAttachments(bubble, attachments) {
    if (!attachments || attachments.length === 0) {
        return;
    }

    const firstBlock = bubble.children().first();
    const target = firstBlock.length > 0 ? firstBlock : bubble;

    for (let i = attachments.length - 1; i >= 0; i--) {
        const attachment = attachments[i];
        if (/\.svg$/i.test(attachment.name || "")) {
            RenderSvgAttachment(bubble, attachment);
            continue;
        }

        const chip = $("<a/>").addClass("askJoobyAttachmentChip")
            .attr("href", "/api/AskJoobyConversation/Files/" + attachment.id)
            .attr("target", "_blank")
            .attr("rel", "noopener")
            .text("📎 " + attachment.name);
        target.prepend(" ").prepend(chip);
    }
}

function RenderSvgAttachment(bubble, attachment) {
    const figure = $("<div/>").addClass("askJoobyChartFigure");
    bubble.append(figure);

    fetch("/api/AskJoobyConversation/Files/" + attachment.id, {credentials: "same-origin"})
        .then(function (response) {
            return response.ok ? response.text() : null;
        })
        .then(function (svgSource) {
            if (svgSource === null) {
                return;
            }

            const trimmed = String(svgSource).trim();
            if (!/^<svg[\s>]/i.test(trimmed) || !/<\/svg>\s*$/i.test(trimmed)) {
                return;
            }

            $("<img/>", {alt: attachment.name}).addClass("askJoobySvgDiagram")
                .attr("src", "data:image/svg+xml;charset=utf-8," + encodeURIComponent(trimmed))
                .appendTo(figure);
        }, function () {
        });
}

let LastActivityAgent = null;

const AskJoobyAgentPrefix = /^\[([A-Za-z0-9 .#_-]{1,48})]\s/;

function AgentOfActivityText(text) {
    const match = AskJoobyAgentPrefix.exec(text || "");
    return match === null ? null : match[1];
}

function AppendActivity(cssClass, text, timestamp, detail, icon) {
    HideThinking();
    const effectiveTimestamp = timestamp || new Date();
    const agent = AgentOfActivityText(text);
    const shifted = LastActivityAgent !== null && agent !== LastActivityAgent;
    LastActivityAgent = agent;

    if (cssClass === "askJoobyDelegationBanner" || cssClass === "askJoobyHandoffBanner" ||
        cssClass === "askJoobyValidationBanner" || cssClass === "askJoobyRevisionBanner") {
        const banner = $("<div/>").addClass(cssClass);
        banner.append($("<span/>").addClass("askJoobyActivityText").text(text));
        AppendActivityDisclosure(banner, detail);
        AppendActivityStamp(banner, effectiveTimestamp);
        AppendToTranscript(WrapActivityWithDetail(banner, detail, true)
            .addClass(shifted ? "askJoobyActivityShift" : ""));
        ScrollTranscriptToBottom();
        return;
    }

    if (cssClass === "askJoobyThought" || cssClass === "askJoobyThinkingBubble") {
        const row = $("<div/>").addClass("askJoobyRow askJoobyRowAssistant");
        const avatar = $("<div/>").addClass("askJoobyAvatar askJoobyAvatarAssistant");
        const wrap = $("<div/>").addClass("askJoobyBubbleWrap");
        const bubbleClass = cssClass === "askJoobyThought" ? "askJoobyThoughtBubble" : "askJoobyThinkingBubble";
        const bubble = $("<div/>").addClass("askJoobyBubble").addClass(bubbleClass);
        bubble.html(AskJoobyRenderMarkdown(text));
        wrap.append(bubble);
        AppendMeta(wrap, effectiveTimestamp);
        row.append(avatar).append(wrap);
        AppendToTranscript(row);
        ScrollTranscriptToBottom();
        return;
    }

    const activityRow = $("<div/>").addClass("askJoobyActivityRow")
        .addClass(shifted ? "askJoobyActivityShift" : "");
    const line = $("<div/>").addClass("askJoobyActivity").addClass(cssClass);
    const toolIcon = AskJoobyToolIcon(icon);
    if (toolIcon) {
        line.addClass("askJoobyToolIconed").append(toolIcon);
    }
    line.append($("<span/>").addClass("askJoobyActivityText").text(text));
    AppendActivityDisclosure(line, detail);
    AppendActivityStamp(line, effectiveTimestamp);
    activityRow.append(WrapActivityWithDetail(line, detail));
    AppendToTranscript(activityRow);
    ScrollTranscriptToBottom();
}

function AppendActivityDisclosure(line, detail) {
    if (!detail) {
        return;
    }

    line.addClass("askJoobyActivityExpandable");
    line.append($("<span/>").addClass("askJoobyActivityDisclosure").attr("aria-hidden", "true").text("\u203A"));
    line.attr("role", "button").attr("tabindex", "0").attr("aria-expanded", "false")
        .attr("title", "Show what was passed and what came back");
}

function WrapActivityWithDetail(line, detail, asMarkdown) {
    if (!detail) {
        return line;
    }

    const holder = $("<div/>").addClass("askJoobyActivityHolder");
    const panel = $("<div/>").addClass("askJoobyActivityDetail").attr("hidden", "hidden");

    if (asMarkdown === true) {
        panel.addClass("askJoobyActivityDetailRich").html(AskJoobyRenderMarkdown(detail));
    } else {
        panel.text(detail);
    }

    function Toggle() {
        const open = line.attr("aria-expanded") === "true";
        line.attr("aria-expanded", open ? "false" : "true");
        line.toggleClass("askJoobyActivityOpen", !open);
        panel.attr("hidden", open ? "hidden" : null);
    }

    line.on("click", Toggle);
    line.on("keydown", function (event) {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            Toggle();
        }
    });

    return holder.append(line).append(panel);
}

function AppendActivityStamp(element, timestamp) {
    const stamp = AskJoobyFormatTimestamp(timestamp);
    if (stamp !== "") {
        element.append($("<span/>").addClass("askJoobyActivityStamp").text(stamp));
    }
}

function AppendSelectionRow(options, hintText) {
    if (!options || options.length === 0) {
        return;
    }

    DisableSelectionRow(ActiveSelectionRow);

    const row = $("<div/>").addClass("askJoobySelectionRow");
    options.forEach(function (option) {
        const label = $("<label/>").addClass("askJoobySelectionOption");
        const checkbox = $("<input/>").attr("type", "checkbox").addClass("askJoobySelectableInput").val(option);
        checkbox.on("change", function () {
            label.toggleClass("askJoobySelectionOptionSelected", checkbox.prop("checked"));
            UpdateSendButtonState();
        });
        label.append(checkbox).append($("<span/>").text(option));
        row.append(label);
    });

    const controls = $("<div/>").addClass("askJoobySelectionControls");
    if (hintText) {
        controls.append($("<span/>").addClass("askJoobyClarificationHint").text(hintText));
    }

    const goButton = $("<button/>").attr("type", "button").addClass("askJoobyGoButton").text("Go");
    goButton.on("click", SendCurrentMessage);
    controls.append(goButton);
    row.append(controls);

    ActiveSelectionRow = row;
    AppendToTranscript(row);
    UpdateSendButtonState();
    ScrollTranscriptToBottom();
}

function DisableSelectionRow(row) {
    if (row === null) {
        return;
    }

    row.find(".askJoobySelectableInput").prop("disabled", true);
    row.find(".askJoobyGoButton").prop("disabled", true);
}

function AppendSuggestions(suggestions) {
    AppendSelectionRow(suggestions, null);
}

function AppendClarification(options, allowFreeText) {
    AppendSelectionRow(options,
        allowFreeText ? "Tick one or more, or just type your own answer below." : "Tick one or more.");
}

function ThinkingPassKey(agentLabel) {
    return agentLabel ? agentLabel : "";
}

function AppendOrUpdateThinkingChunk(state, agentLabel, text) {
    const key = ThinkingPassKey(agentLabel);
    let pass = state.thinkingPasses[key];

    if (!pass) {
        HideThinking();
        const row = $("<div/>").addClass("askJoobyRow askJoobyRowAssistant");
        const avatar = $("<div/>").addClass("askJoobyAvatar askJoobyAvatarAssistant");
        const wrap = $("<div/>").addClass("askJoobyBubbleWrap");
        const bubble = $("<div/>").addClass("askJoobyBubble askJoobyThinkingBubble");
        if (key !== "") {
            $("<div/>").addClass("askJoobyThinkingWho").text(key).appendTo(wrap);
        }

        wrap.append(bubble);

        AppendMeta(wrap, new Date());
        row.append(avatar).append(wrap);
        AppendToTranscript(row);
        pass = {bubble: bubble, text: ""};
        state.thinkingPasses[key] = pass;
    }

    if (text) {
        pass.text += text;
        pass.bubble.html(AskJoobyRenderMarkdown(pass.text));
    }

    ScrollTranscriptToBottom();
}

function CloseThinkingPass(state, agentLabel) {
    delete state.thinkingPasses[ThinkingPassKey(agentLabel)];
}

function CloseAllThinkingPasses(state) {
    state.thinkingPasses = {};
}

function AskJoobyActivityClass(kind) {
    switch (kind) {
        case "Step":
            return "askJoobyStep";
        case "Thought":
            return "askJoobyThought";
        case "Thinking":
            return "askJoobyThinkingBubble";
        case "Delegation":
            return "askJoobyDelegationBanner";
        case "Handoff":
            return "askJoobyHandoffBanner";
        case "Validation":
            return "askJoobyValidationBanner";
        case "Revision":
            return "askJoobyRevisionBanner";
        default:
            return "askJoobyStep";
    }
}

function RenderTranscript(entries) {
    AskJoobyTranscript.empty();
    TranscriptFollowingEnd = true;
    LastActivityAgent = null;
    HasRealExchange = false;
    PinAuditLinkToEnd();
    ThinkingIndicator = null;

    ActiveSelectionRow = null;
    UpdateSendButtonState();

    let isFirstMessageEntry = true;

    for (let i = 0; i < entries.length; i++) {
        const entry = entries[i];
        if (entry.kind === "Message") {
            const isGreeting = isFirstMessageEntry;
            isFirstMessageEntry = false;
            AppendMessage(entry.role, entry.text, entry.inputTokens, entry.outputTokens, entry.durationMs,
                entry.attachments, entry.role === "Assistant" ? entry.id : null, isGreeting,
                entry.createdDate, entry.cachedInputTokens, entry.joobrikCount, entry.distinctJoobrikCount,
                entry.totalCost);
        } else {
            AppendActivity(AskJoobyActivityClass(entry.kind), entry.text, entry.createdDate, entry.detail,
                entry.iconId);
        }
    }

    if (entries.length > 0) {
        PinTranscriptStart(entries[0].createdDate);
        PinAuditLinkToEnd(entries[entries.length - 1].createdDate);
    }
}

const AskJoobyAllowedExtensions = [".txt", ".md", ".csv", ".json", ".log"];

function RenderPendingChips() {
    const container = $("#askJoobyPendingChips");
    container.empty();

    PendingAttachments.forEach(function (attachment) {
        const chip = $("<span/>").addClass("askJoobyPendingChip");
        if (attachment.uploading) {
            chip.addClass("askJoobyPendingChipUploading");
        }

        $("<span/>").addClass("askJoobyPendingChipName")
            .text(attachment.uploading ? attachment.name + " (uploading...)" : attachment.name)
            .appendTo(chip);

        if (!attachment.uploading) {
            $("<button/>").attr("type", "button").addClass("askJoobyPendingChipRemove")
                .attr("aria-label", "Remove " + attachment.name).text("×")
                .on("click", function () {
                    RemovePendingAttachment(attachment.id);
                })
                .appendTo(chip);
        }

        container.append(chip);
    });
}

function ResetUploadWidget() {
    PendingAttachments = PendingAttachments.filter(function (attachment) {
        return attachment.uploading;
    });
    RenderPendingChips();
    AskJoobyUpload.val("");
    UpdateSendButtonState();
}

function RemovePendingAttachment(fileId) {
    PendingAttachments = PendingAttachments.filter(function (attachment) {
        return attachment.id !== fileId;
    });
    RenderPendingChips();
    UpdateSendButtonState();
    $.ajax({url: "/api/AskJoobyConversation/Files/" + fileId, type: "DELETE"});
}

function UploadSelectedFile(file) {
    const extension = "." + file.name.split(".").pop().toLowerCase();
    if (AskJoobyAllowedExtensions.indexOf(extension) === -1) {
        AppendActivity("askJoobyError", "Only .txt, .md, .csv, .json or .log files can be attached.");
        return;
    }

    const tempId = "temp-" + Date.now() + "-" + Math.random();
    PendingAttachments.push({id: tempId, name: file.name, uploading: true});
    RenderPendingChips();

    EnsureConversationAsync().then(function (conversationId) {
        const formData = new FormData();
        formData.append("files", file);

        $.ajax({
            url: "/api/AskJoobyConversation/" + conversationId + "/Files",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false
        }).done(function (response) {
            const entry = PendingAttachments.find(function (attachment) {
                return attachment.id === tempId;
            });

            if (entry) {
                entry.id = response.id;
                entry.name = response.name || file.name;
                entry.uploading = false;
            }

            RenderPendingChips();
            UpdateSendButtonState();
        }).fail(function () {
            PendingAttachments = PendingAttachments.filter(function (attachment) {
                return attachment.id !== tempId;
            });
            RenderPendingChips();
            AppendActivity("askJoobyError", "\"" + file.name + "\" could not be attached.");
        });
    });
}

function SelectConversation(id, allowGreeting) {
    if (allowGreeting === undefined) {
        allowGreeting = true;
    }

    CurrentConversationId = id;
    ResetUploadWidget();
    $(".askJoobyConversationItem").removeClass("askJoobyConversationSelected");
    $(".askJoobyConversationItem[data-id='" + id + "']").addClass("askJoobyConversationSelected");

    RefreshComposerButtonForCurrentConversation();

    $.get("/api/AskJoobyConversation/" + id + "/Messages", function (entries) {
        RenderTranscript(entries);
        if (allowGreeting && entries.length === 0) {
            StartConversation(id);
        }

        const state = ConversationStreamState.get(id);
        if (state && state.handle !== null) {
            ShowThinking();
            ScrollTranscriptToBottom(true);
        }
    });
}

function CreateConversationAsync(allowGreeting) {
    if (allowGreeting === undefined) {
        allowGreeting = true;
    }

    return $.post("/api/AskJoobyConversation", function (conversation) {
        LoadConversations();
        SelectConversation(conversation.id, allowGreeting);
    });
}

let PendingConversationCreation = null;

function EnsureConversationAsync() {
    if (CurrentConversationId !== null) {
        return $.Deferred().resolve(CurrentConversationId).promise();
    }

    if (PendingConversationCreation === null) {
        PendingConversationCreation = CreateConversationAsync(false).then(function () {
            return CurrentConversationId;
        }).always(function () {
            PendingConversationCreation = null;
        });
    }

    return PendingConversationCreation;
}

function DeleteConversation(id) {
    if (!confirm("Delete this conversation? This cannot be undone.")) {
        return;
    }

    $.ajax({
        url: "/api/AskJoobyConversation/" + id,
        type: "DELETE",
        success: function () {
            LoadConversations();

            const state = ConversationStreamState.get(id);
            if (state && state.handle !== null) {
                state.handle.dispose();
            }

            ConversationStreamState.delete(id);

            if (CurrentConversationId === id) {
                CurrentConversationId = null;
                AskJoobyTranscript.empty();
                LastActivityAgent = null;
                PinAuditLinkToEnd();
            }
        }
    });
}

function GetConversationStreamState(conversationId) {
    let state = ConversationStreamState.get(conversationId);
    if (!state) {
        state = {handle: null, busy: false, allowStop: true};
        ConversationStreamState.set(conversationId, state);
    }

    return state;
}

function RefreshComposerButtonForCurrentConversation() {
    const state = CurrentConversationId === null ? null : ConversationStreamState.get(CurrentConversationId);
    const busy = state !== null && state !== undefined && state.busy;
    const allowStop = state === null || state === undefined || state.allowStop;

    AskJoobyStopButton.prop("hidden", !busy);

    const stopButton = AskJoobyStopButton.data("kendoButton");
    if (stopButton) {
        stopButton.enable(allowStop);
    }

    const sendButton = AskJoobySendButton.data("kendoButton");
    if (sendButton) {
        sendButton.enable(CanSendCurrentMessage());
    }
}

function SetComposerBusy(conversationId, busy, allowStop) {
    if (allowStop === undefined) {
        allowStop = true;
    }

    const state = GetConversationStreamState(conversationId);
    state.busy = busy;
    state.allowStop = allowStop;

    if (conversationId === CurrentConversationId) {
        RefreshComposerButtonForCurrentConversation();
    }
}

function CheckedSelectionOptions() {
    if (ActiveSelectionRow === null) {
        return [];
    }

    return ActiveSelectionRow.find(".askJoobySelectableInput:checked")
        .map(function () {
            return $(this).val();
        }).get();
}

function CanSendCurrentMessage() {
    if (AskJoobyOffline) {
        return false;
    }

    return AskJoobyInput.val().trim() !== "" || PendingAttachments.length > 0 ||
        CheckedSelectionOptions().length > 0;
}

function UpdateSendButtonState() {
    const canSend = CanSendCurrentMessage();
    const sendButton = AskJoobySendButton.data("kendoButton");
    if (sendButton) {
        sendButton.enable(canSend);
    }

    if (ActiveSelectionRow !== null) {
        ActiveSelectionRow.find(".askJoobyGoButton").prop("disabled", !canSend);
    }
}

function AppendBannerActivity(state, item, cssClass) {
    state.assistantBubble = null;
    state.assistantText = "";
    CloseThinkingPass(state, item.agentLabel);
    AppendActivity(cssClass, item.text, null, item.detail);
    ShowThinking();
}

function FinishStream(conversationId, handleRef, isCurrent) {
    const streamState = ConversationStreamState.get(conversationId);
    if (streamState && streamState.handle === handleRef.handle) {
        streamState.handle = null;
    }

    SetComposerBusy(conversationId, false);

    return isCurrent();
}

function BuildStreamSubscriber(conversationId, handleRef, isGreeting) {
    const state = {
        assistantBubble: null, assistantText: "", assistantTimestamp: null, thinkingPasses: {}
    };

    function isCurrent() {
        return conversationId === CurrentConversationId;
    }

    return {
        next: function (item) {
            if (!isCurrent()) {
                return;
            }

            switch (item.kind) {
                case "Text":
                    CloseAllThinkingPasses(state);
                    if (state.assistantBubble === null) {
                        HideThinking();
                        state.assistantTimestamp = new Date();
                        state.assistantBubble = AppendMessage("Assistant", "", null, null, null, null, null,
                            isGreeting, state.assistantTimestamp);
                    }

                    if (item.text) {
                        state.assistantText += item.text;
                        state.assistantBubble.html(AskJoobyRenderMarkdown(state.assistantText));
                    }

                    if (item.messageId) {
                        if (!isGreeting) {
                            AppendFeedbackControls(state.assistantBubble.parent(), item.messageId);
                        }

                        PrependAttachments(state.assistantBubble, item.attachments);
                    }

                    ScrollTranscriptToBottom();
                    break;
                case "Thinking":
                    state.assistantBubble = null;
                    state.assistantText = "";
                    AppendOrUpdateThinkingChunk(state, item.agentLabel, item.text);
                    break;
                case "Step":
                    state.assistantBubble = null;
                    state.assistantText = "";
                    CloseThinkingPass(state, item.agentLabel);
                    AppendActivity("askJoobyStep", item.text, null, item.detail, item.iconId);
                    ShowThinking();
                    break;
                case "Thought":
                    state.assistantBubble = null;
                    state.assistantText = "";
                    CloseThinkingPass(state, item.agentLabel);
                    AppendActivity("askJoobyThought", item.text);
                    ShowThinking();
                    break;
                case "Guard":
                    state.assistantBubble = null;
                    state.assistantText = "";
                    AppendActivity("askJoobyGuardBanner", item.text, null, item.detail);
                    break;
                case "Delegation":
                    AppendBannerActivity(state, item, "askJoobyDelegationBanner");
                    break;
                case "Handoff":
                    AppendBannerActivity(state, item, "askJoobyHandoffBanner");
                    break;
                case "Validation":
                    AppendBannerActivity(state, item, "askJoobyValidationBanner");
                    break;
                case "Revision":
                    AppendBannerActivity(state, item, "askJoobyRevisionBanner");
                    break;
                case "Title":
                    LoadConversations();
                    break;
                case "Stats":
                    if (state.assistantBubble !== null) {
                        AppendMeta(state.assistantBubble.parent(), state.assistantTimestamp, item.inputTokens,
                            item.outputTokens, item.durationMs, item.cachedInputTokens, item.joobrikCount,
                            item.distinctJoobrikCount, item.totalCost);
                    }

                    break;
                case "Suggestions":
                    AppendSuggestions(item.suggestions);
                    break;
                case "Clarification":
                    AppendClarification(item.options, item.allowFreeText);
                    break;
                case "Error":
                    state.assistantBubble = null;
                    state.assistantText = "";
                    CloseAllThinkingPasses(state);
                    AppendActivity("askJoobyError", item.text);
                    break;
                default:
                    break;
            }
        },
        error: function (error) {
            if (!FinishStream(conversationId, handleRef, isCurrent)) {
                return;
            }

            HideThinking();
            AppendActivity("askJoobyError", "Something went wrong: " + error);
        },
        complete: function () {
            if (!FinishStream(conversationId, handleRef, isCurrent)) {
                return;
            }

            HideThinking();
        }
    };
}

function StreamMessage(conversationId, text) {
    ShowThinking();
    SetComposerBusy(conversationId, true);

    const attachmentIds = PendingAttachments.filter(function (attachment) {
        return !attachment.uploading;
    }).map(function (attachment) {
        return attachment.id;
    });
    ResetUploadWidget();

    const handleRef = {handle: null};
    WaitForConnectedAsync().then(function () {
        handleRef.handle = Connection.stream("SendMessageAsync", conversationId, text, null, attachmentIds)
            .subscribe(BuildStreamSubscriber(conversationId, handleRef));
        GetConversationStreamState(conversationId).handle = handleRef.handle;
    }, function (error) {
        HideThinking();
        SetComposerBusy(conversationId, false);
        AppendActivity("askJoobyError", "Could not connect: " + error);
    });
}

function StartConversation(conversationId) {
    if (AskJoobyOffline) {
        FetchOfflineWelcomeAsync().then(function (welcome) {
            AppendMessage("Assistant", welcome, null, null, null, null, null, true, new Date());
        });
        return;
    }

    ShowThinking();
    SetComposerBusy(conversationId, true, false);

    const handleRef = {handle: null};
    WaitForConnectedAsync().then(function () {
        handleRef.handle = Connection.stream("StartConversationAsync", conversationId, null)
            .subscribe(BuildStreamSubscriber(conversationId, handleRef, true));
        GetConversationStreamState(conversationId).handle = handleRef.handle;
    }, function (error) {
        HideThinking();
        SetComposerBusy(conversationId, false);
        AppendActivity("askJoobyError", "Could not connect: " + error);
    });
}

function StopCurrentReply() {
    if (CurrentConversationId === null) {
        return;
    }

    const state = ConversationStreamState.get(CurrentConversationId);
    if (!state || state.handle === null) {
        return;
    }

    state.handle.dispose();
    state.handle = null;
    HideThinking();
    SetComposerBusy(CurrentConversationId, false);
    AppendActivity("askJoobyStep", "Stopped.");
}

function SendMessageWhileBusy(conversationId, text) {
    $.ajax({
        url: "/api/AskJoobyConversation/" + conversationId + "/Guidance",
        type: "POST",
        contentType: "application/json",
        data: JSON.stringify({guidance: text})
    }).fail(function () {
        AppendActivity("askJoobyError", "That didn't reach Jooby -- try sending it again in a moment.");
    });
}

function JoinWithAnd(items) {
    if (items.length <= 1) {
        return items.join("");
    }

    return items.slice(0, -1).join(", ") + " and " + items[items.length - 1];
}

function BuildSelectionAwareMessage(typedText) {
    const checked = CheckedSelectionOptions();
    if (checked.length === 0) {
        return typedText;
    }

    const selectedPart = JoinWithAnd(checked);
    return typedText === "" ? selectedPart : selectedPart + " -- " + typedText;
}

function BuildDisplayAttachments() {
    return PendingAttachments.filter(function (attachment) {
        return !attachment.uploading;
    }).map(function (attachment) {
        return {id: attachment.id, name: attachment.name};
    });
}

function SendCurrentMessage() {
    const input = AskJoobyInput;
    const typedText = input.val().trim();

    if (!CanSendCurrentMessage()) {
        return;
    }

    const text = BuildSelectionAwareMessage(typedText);

    DisableSelectionRow(ActiveSelectionRow);
    ActiveSelectionRow = null;

    input.val("");

    AppendMessage("User", text, null, null, null, BuildDisplayAttachments(), null, false, new Date());

    EnsureConversationAsync().then(function (conversationId) {
        const streamState = ConversationStreamState.get(conversationId);
        if (streamState && streamState.busy) {
            SendMessageWhileBusy(conversationId, text);
        } else {
            StreamMessage(conversationId, text);
        }
    });
}

function FixAskJoobyPageHeight() {
    document.body.style.marginBottom = "0";
}

function FixSplitterPaneWidths() {
    const splitterElement = document.getElementById("askJoobySplitter");
    const pane1 = splitterElement.children[0];
    const splitbar = splitterElement.children[1];
    const pane2 = splitterElement.children[2];

    if (!pane1 || !splitbar || !pane2) {
        return;
    }

    const correctPane2Width = splitterElement.clientWidth - pane1.offsetWidth - splitbar.offsetWidth;
    if (correctPane2Width > 0) {
        pane2.style.width = correctPane2Width + "px";
    }
}

$(function () {
    AskJoobyTranscript = $("#askJoobyTranscript");
    AskJoobyInput = $("#askJoobyInput");
    AskJoobyConversations = $("#askJoobyConversations");
    AskJoobyStopButton = $("#askJoobyStop");
    AskJoobySendButton = $("#askJoobySend");
    AskJoobyConversationSearch = $("#askJoobyConversationSearch");
    AskJoobyUpload = $("#askJoobyUpload");

    FixAskJoobyPageHeight();

    AskJoobyTranscript.on("scroll", function () {
        TranscriptFollowingEnd = TranscriptIsAtEnd(this);
    });

    const splitter = $("#askJoobySplitter").kendoSplitter({
        panes: [
            {
                collapsible: true,
                size: "280px"
            },
            {
                collapsible: false
            }
        ],
        resize: FixSplitterPaneWidths
    }).data("kendoSplitter");

    splitter.resize(true);
    $(window).on("load resize", function () {
        FixAskJoobyPageHeight();
        splitter.resize(true);
    });

    setTimeout(function () {
        FixAskJoobyPageHeight();
        splitter.resize(true);
    }, 500);

    AskJoobyConversations.kendoListView({
        dataSource: {
            transport: {
                read: {
                    url: "/api/AskJoobyConversation",

                    data: function () {
                        return {search: AskJoobyConversationSearch.val() || ""};
                    }
                }
            }
        },
        template: kendo.template($("#askJoobyConversationTemplate").html())
    });

    var conversationSearchWidget = AskJoobyConversationSearch.kendoTextBox().data("kendoTextBox");

    if (conversationSearchWidget && conversationSearchWidget.wrapper) {
        conversationSearchWidget.wrapper.css("width", "100%");
    }
    let conversationSearchDebounce = null;
    AskJoobyConversationSearch.on("input", function () {
        clearTimeout(conversationSearchDebounce);
        conversationSearchDebounce = setTimeout(LoadConversations, 250);
    });

    AskJoobyConversations.on("click", ".askJoobyConversationItem", function () {
        SelectConversation(Number($(this).data("id")));
    });

    AskJoobyConversations.on("click", ".askJoobyConversationDelete", function (e) {
        e.stopPropagation();
        DeleteConversation(Number($(this).data("id")));
    });

    AskJoobySendButton.kendoButton({
        click: SendCurrentMessage
    });

    AskJoobyStopButton.kendoButton({
        click: StopCurrentReply
    });

    AskJoobyInput.kendoTextArea();

    AskJoobyInput.on("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            SendCurrentMessage();
        }
    });

    AskJoobyInput.on("input", UpdateSendButtonState);
    UpdateSendButtonState();

    $("#askJoobyNew").kendoButton({
        click: function () {
            CreateConversationAsync(true);
        }
    });

    $("#askJoobyAttachButton").kendoButton({
        click: function () {
            document.getElementById("askJoobyUpload").click();
        }
    });

    AskJoobyUpload.on("change", function (e) {
        const files = Array.from(e.target.files);
        e.target.value = "";
        files.forEach(UploadSelectedFile);
    });
});
