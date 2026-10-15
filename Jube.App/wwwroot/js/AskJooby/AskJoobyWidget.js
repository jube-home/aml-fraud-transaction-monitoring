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

const AskJoobyWidgetStorageKeyPrefix = "askJoobyWidgetConversationId:";

const AskJoobyWidgetOffline = window.AskJoobyHasInferenceBackend === false;

let AskJoobyWidgetConversationId = null;
let AskJoobyWidgetHistoryLoaded = false;
let AskJoobyWidgetAssistantBubble = null;
let AskJoobyWidgetAssistantText = "";

let AskJoobyWidgetAssistantTimestamp = null;
let AskJoobyWidgetThinkingPasses = {};
let AskJoobyWidgetThinkingIndicator = null;
let AskJoobyWidgetThinkingTick = null;
let AskJoobyWidgetConnection = null;
let AskJoobyWidgetConnectionReady = null;
let AskJoobyWidgetStreamHandle = null;
let AskJoobyWidgetAllowStopCurrentReply = true;

let AskJoobyWidgetActiveSelectionRow = null;

let AskJoobyWidgetTranscript = null;
let AskJoobyWidgetSendButton = null;
let AskJoobyWidgetInput = null;

function AskJoobyWidgetCurrentPageContextKey() {
    const pageContext = typeof AskJoobyPageContext !== "undefined" ? AskJoobyPageContext : null;

    const pageName = pageContext && pageContext.pageName
        ? String(pageContext.pageName)
        : "default";

    const recordSuffix = pageContext && pageContext.record
        ? ":" + JSON.stringify(pageContext.record)
        : "";

    return AskJoobyWidgetStorageKeyPrefix + pageName + recordSuffix;
}

function AskJoobyWidgetLoadStoredConversationId() {
    try {
        const stored = window.localStorage.getItem(AskJoobyWidgetCurrentPageContextKey());
        return stored ? Number(stored) : null;
    } catch (e) {
        return null;
    }
}

function AskJoobyWidgetStoreConversationId(id) {
    try {
        const key = AskJoobyWidgetCurrentPageContextKey();
        if (id === null) {
            window.localStorage.removeItem(key);
        } else {
            window.localStorage.setItem(key, String(id));
        }
    } catch (e) {
    }
}

function AskJoobyWidgetConnect() {
    if (AskJoobyWidgetConnection === null) {
        AskJoobyWidgetConnection = AskJoobyCreateConnection();
        AskJoobyWidgetConnectionReady = AskJoobyWidgetConnection.start();
        return AskJoobyWidgetConnectionReady.then(function () {
            return AskJoobyWidgetConnection;
        });
    }

    if (AskJoobyWidgetConnection.state === AskJoobyConnectionState.Connected) {
        return Promise.resolve(AskJoobyWidgetConnection);
    }

    if (AskJoobyWidgetConnection.state === AskJoobyConnectionState.Disconnected) {
        return AskJoobyWidgetConnectionReady.then(function () {
            return AskJoobyWidgetConnection;
        });
    }

    return new Promise(function (resolve, reject) {
        AskJoobyWidgetConnection.onreconnected(function () {
            resolve(AskJoobyWidgetConnection);
        });
        AskJoobyWidgetConnection.onclose(function (error) {
            reject(error || new Error("Connection closed."));
        });
    });
}

function AskJoobyWidgetFetchOfflineWelcomeAsync() {
    return $.get("/api/AskJoobyConversation/StartupStats")
        .then(function (stats) {
            return AskJoobyPickOfflineWelcome(stats);
        }, function () {
            return AskJoobyPickOfflineWelcome(null);
        });
}

let AskJoobyWidgetFollowingEnd = true;

function AskJoobyWidgetScrollToBottom(force) {
    const transcript = AskJoobyWidgetTranscript;
    if (!transcript.length) {
        return;
    }

    if (!force && !AskJoobyWidgetFollowingEnd) {
        return;
    }

    transcript.scrollTop(transcript[0].scrollHeight);
    AskJoobyWidgetFollowingEnd = true;
}

function AskJoobyWidgetPinAuditLinkToEnd(endDate) {
    $("#askJoobyWidgetTranscript > .askJoobyAuditLinkRow").remove();

    const row = $("<div/>").addClass("askJoobyAuditLinkRow");

    const time = AskJoobyFormatTimestamp(endDate);
    if (time !== "") {
        row.append($("<span/>").addClass("askJoobyTranscriptStamp").text("Last updated " + time));
    }

    const link = $("<a/>").attr("href", "#").text("Download Chat Audit");
    link.on("click", function (e) {
        if (AskJoobyWidgetConversationId === null) {
            e.preventDefault();
            return;
        }

        $(this).attr("href", "/api/AskJoobyConversation/" + AskJoobyWidgetConversationId + "/Audit");
    });

    row.append(link);
    AskJoobyWidgetTranscript.append(row);
}

function AskJoobyWidgetAppendToTranscript(element) {
    AskJoobyWidgetTranscript.append(element);
    AskJoobyWidgetPinAuditLinkToEnd(new Date());
}

function AskJoobyWidgetPinTranscriptStart(startDate) {
    $("#askJoobyWidgetTranscript > .askJoobyTranscriptStart").remove();

    const time = AskJoobyFormatTimestamp(startDate);
    if (time === "") {
        return;
    }

    AskJoobyWidgetTranscript.prepend(
        $("<div/>").addClass("askJoobyTranscriptStart").text("Started " + time));
}

function AskJoobyWidgetShowThinking() {
    if (AskJoobyWidgetThinkingIndicator !== null) {
        return;
    }

    AskJoobyWidgetThinkingIndicator = $(
        "<div class=\"askJoobyThinking\">" +
        "<span class=\"askJoobyThinkingBuffalo\" aria-hidden=\"true\" title=\"Gallopy gallopy gallopy\"></span>" +
        "<span class=\"askJoobyThinkingWord askJoobyThinkingDot1\">Gallopy</span>" +
        "<span class=\"askJoobyThinkingWord askJoobyThinkingDot2\">Gallopy</span>" +
        "<span class=\"askJoobyThinkingWord askJoobyThinkingDot3\">Gallopy</span>" +
        "<span class=\"askJoobyThinkingElapsed\"></span></div>");
    AskJoobyWidgetAppendToTranscript(AskJoobyWidgetThinkingIndicator);

    const startedAt = Date.now();
    const elapsed = AskJoobyWidgetThinkingIndicator.find(".askJoobyThinkingElapsed");
    AskJoobyWidgetThinkingTick = setInterval(function () {
        elapsed.text(Math.round((Date.now() - startedAt) / 1000) + "s");
    }, 1000);
    AskJoobyWidgetScrollToBottom();
}

function AskJoobyWidgetHideThinking() {
    if (AskJoobyWidgetThinkingIndicator === null) {
        return;
    }

    if (AskJoobyWidgetThinkingTick !== null) {
        clearInterval(AskJoobyWidgetThinkingTick);
        AskJoobyWidgetThinkingTick = null;
    }

    AskJoobyWidgetThinkingIndicator.remove();
    AskJoobyWidgetThinkingIndicator = null;
}

function AskJoobyWidgetAppendMessage(role, text, inputTokens, outputTokens, durationMs, attachments, messageId,
                                     isGreeting, timestamp, cachedInputTokens, joobrikCount, distinctJoobrikCount, totalCost) {
    AskJoobyWidgetHideThinking();
    const row = $("<div/>").addClass("askJoobyRow").addClass("askJoobyRow" + role);
    const avatar = $("<div/>").addClass("askJoobyAvatar").addClass("askJoobyAvatar" + role);
    if (role === "User") {
        avatar.text("U");
    }

    const wrap = $("<div/>").addClass("askJoobyBubbleWrap");
    const bubble = $("<div/>").addClass("askJoobyBubble").addClass("askJoobyBubble" + role);
    bubble.append(AskJoobyRenderMarkdown(text));
    AskJoobyWidgetPrependAttachments(bubble, attachments);
    wrap.append(bubble);
    AskJoobyWidgetAppendMeta(wrap, timestamp, inputTokens, outputTokens, durationMs, cachedInputTokens,
        joobrikCount, distinctJoobrikCount, totalCost);
    if (role === "Assistant" && messageId && !isGreeting) {
        AskJoobyWidgetAppendFeedbackControls(wrap, messageId);
    }

    row.append(avatar).append(wrap);
    AskJoobyWidgetAppendToTranscript(row);
    AskJoobyWidgetScrollToBottom(role === "User");
    return bubble;
}

function AskJoobyWidgetAppendFeedbackControls(wrap, messageId) {
    wrap.find(".askJoobyFeedback").remove();

    const feedback = $("<div/>").addClass("askJoobyFeedback");
    const up = $("<button/>").attr("type", "button").addClass("askJoobyFeedbackButton")
        .attr("aria-label", "Good response, worth remembering").text("👍");
    const down = $("<button/>").attr("type", "button").addClass("askJoobyFeedbackButton")
        .attr("aria-label", "Bad response, worth remembering as an example to avoid").text("👎");

    function submit(isPositive, clicked, other) {
        clicked.prop("disabled", true).addClass("askJoobyFeedbackChosen");
        other.prop("disabled", true);

        AskJoobyWidgetConnect().then(function (connection) {
            return connection.invoke(
                "SubmitFeedbackAsync", AskJoobyWidgetConversationId, messageId, isPositive);
        }).then(function (confirmation) {
            AskJoobyWidgetAppendActivity("askJoobyStep", confirmation);
        }, function (error) {
            clicked.prop("disabled", false).removeClass("askJoobyFeedbackChosen");
            other.prop("disabled", false);
            AskJoobyWidgetAppendActivity("askJoobyError", "Couldn't save feedback: " + (error && error.message ? error.message : error));
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

function AskJoobyWidgetAppendMeta(wrap, timestamp, inputTokens, outputTokens, durationMs,
                                  cachedInputTokens, joobrikCount, distinctJoobrikCount, totalCost) {
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

function AskJoobyWidgetPrependAttachments(bubble, attachments) {
    if (!attachments || attachments.length === 0) {
        return;
    }

    const firstBlock = bubble.children().first();
    const target = firstBlock.length > 0 ? firstBlock : bubble;

    for (let i = attachments.length - 1; i >= 0; i--) {
        const attachment = attachments[i];
        if (/\.svg$/i.test(attachment.name || "")) {
            AskJoobyWidgetRenderSvgAttachment(bubble, attachment);
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

function AskJoobyWidgetRenderSvgAttachment(bubble, attachment) {
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

function AskJoobyWidgetActivityClass(kind) {
    switch (kind) {
        case "Step":
            return "askJoobyStep";
        case "Thought":
            return "askJoobyThought";
        case "Thinking":
            return "askJoobyThinkingBubble";
        case "Delegation":
            return "askJoobyDelegationBanner";
        case "Validation":
            return "askJoobyValidationBanner";
        default:
            return "askJoobyStep";
    }
}

function AskJoobyWidgetAppendActivity(cssClass, text, timestamp, icon) {
    AskJoobyWidgetHideThinking();
    const effectiveTimestamp = timestamp || new Date();

    if (cssClass === "askJoobyDelegationBanner") {
        const banner = $("<div/>").addClass("askJoobyDelegationBanner").text(text);
        AskJoobyWidgetAppendActivityStamp(banner, effectiveTimestamp);
        AskJoobyWidgetAppendToTranscript(banner);
        AskJoobyWidgetScrollToBottom();
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
        AskJoobyWidgetAppendMeta(wrap, effectiveTimestamp);
        row.append(avatar).append(wrap);
        AskJoobyWidgetAppendToTranscript(row);
        AskJoobyWidgetScrollToBottom();
        return;
    }

    const activityRow = $("<div/>").addClass("askJoobyActivityRow");
    const line = $("<div/>").addClass("askJoobyActivity").addClass(cssClass);
    const toolIcon = AskJoobyToolIcon(icon);
    if (toolIcon) {
        line.addClass("askJoobyToolIconed").append(toolIcon);
    }
    line.append($("<span/>").addClass("askJoobyActivityText").text(text));
    AskJoobyWidgetAppendActivityStamp(line, effectiveTimestamp);
    activityRow.append(line);
    AskJoobyWidgetAppendToTranscript(activityRow);
    AskJoobyWidgetScrollToBottom();
}

function AskJoobyWidgetAppendActivityStamp(element, timestamp) {
    const stamp = AskJoobyFormatTimestamp(timestamp);
    if (stamp !== "") {
        element.append($("<span/>").addClass("askJoobyActivityStamp").text(stamp));
    }
}

function AskJoobyWidgetAppendSelectionRow(options, hintText) {
    if (!options || options.length === 0) {
        return;
    }

    AskJoobyWidgetDisableSelectionRow(AskJoobyWidgetActiveSelectionRow);

    const row = $("<div/>").addClass("askJoobySelectionRow");
    options.forEach(function (option) {
        const label = $("<label/>").addClass("askJoobySelectionOption");
        const checkbox = $("<input/>").attr("type", "checkbox").addClass("askJoobySelectableInput").val(option);
        checkbox.on("change", function () {
            label.toggleClass("askJoobySelectionOptionSelected", checkbox.prop("checked"));
        });
        label.append(checkbox).append($("<span/>").text(option));
        row.append(label);
    });

    const controls = $("<div/>").addClass("askJoobySelectionControls");
    if (hintText) {
        controls.append($("<span/>").addClass("askJoobyClarificationHint").text(hintText));
    }

    const goButton = $("<button/>").attr("type", "button").addClass("askJoobyGoButton").text("Go");
    goButton.on("click", AskJoobyWidgetSendCurrentMessage);
    controls.append(goButton);
    row.append(controls);

    AskJoobyWidgetActiveSelectionRow = row;
    AskJoobyWidgetAppendToTranscript(row);
    AskJoobyWidgetScrollToBottom();
}

function AskJoobyWidgetDisableSelectionRow(row) {
    if (row === null) {
        return;
    }

    row.find(".askJoobySelectableInput").prop("disabled", true);
    row.find(".askJoobyGoButton").prop("disabled", true);
}

function AskJoobyWidgetAppendSuggestions(suggestions) {
    AskJoobyWidgetAppendSelectionRow(suggestions, null);
}

function AskJoobyWidgetAppendClarification(options, allowFreeText) {
    AskJoobyWidgetAppendSelectionRow(options,
        allowFreeText ? "Tick one or more, or just type your own answer below." : "Tick one or more.");
}

function AskJoobyWidgetThinkingPassKey(agentLabel) {
    return agentLabel ? agentLabel : "";
}

function AskJoobyWidgetAppendOrUpdateThinkingChunk(agentLabel, text) {
    const key = AskJoobyWidgetThinkingPassKey(agentLabel);
    let pass = AskJoobyWidgetThinkingPasses[key];

    if (!pass) {
        AskJoobyWidgetHideThinking();
        const row = $("<div/>").addClass("askJoobyRow askJoobyRowAssistant");
        const avatar = $("<div/>").addClass("askJoobyAvatar askJoobyAvatarAssistant");
        const wrap = $("<div/>").addClass("askJoobyBubbleWrap");
        const bubble = $("<div/>").addClass("askJoobyBubble askJoobyThinkingBubble");
        if (key !== "") {
            $("<div/>").addClass("askJoobyThinkingWho").text(key).appendTo(wrap);
        }

        wrap.append(bubble);
        AskJoobyWidgetAppendMeta(wrap, new Date());
        row.append(avatar).append(wrap);
        AskJoobyWidgetAppendToTranscript(row);
        pass = {bubble: bubble, text: ""};
        AskJoobyWidgetThinkingPasses[key] = pass;
    }

    if (text) {
        pass.text += text;
        pass.bubble.html(AskJoobyRenderMarkdown(pass.text));
    }

    AskJoobyWidgetScrollToBottom();
}

function AskJoobyWidgetCloseThinkingPass(agentLabel) {
    delete AskJoobyWidgetThinkingPasses[AskJoobyWidgetThinkingPassKey(agentLabel)];
}

function AskJoobyWidgetCloseAllThinkingPasses() {
    AskJoobyWidgetThinkingPasses = {};
}

function AskJoobyWidgetLoadHistoryIfNeeded() {
    if (AskJoobyWidgetHistoryLoaded || AskJoobyWidgetConversationId === null) {
        AskJoobyWidgetHistoryLoaded = true;
        return;
    }

    AskJoobyWidgetHistoryLoaded = true;

    $.get("/api/AskJoobyConversation/" + AskJoobyWidgetConversationId + "/Messages")
        .done(function (entries) {
            if (!entries) {
                AskJoobyWidgetStartNewConversation();
                return;
            }

            AskJoobyWidgetTranscript.empty();
            AskJoobyWidgetFollowingEnd = true;
            AskJoobyWidgetPinAuditLinkToEnd();
            AskJoobyWidgetActiveSelectionRow = null;

            let isFirstMessageEntry = true;
            for (let i = 0; i < entries.length; i++) {
                const entry = entries[i];
                if (entry.kind === "Message") {
                    const isGreeting = isFirstMessageEntry;
                    isFirstMessageEntry = false;
                    AskJoobyWidgetAppendMessage(entry.role, entry.text, entry.inputTokens, entry.outputTokens,
                        entry.durationMs, entry.attachments, entry.role === "Assistant" ? entry.id : null,
                        isGreeting, entry.createdDate, entry.cachedInputTokens, entry.joobrikCount,
                        entry.distinctJoobrikCount, entry.totalCost);
                } else {
                    AskJoobyWidgetAppendActivity(AskJoobyWidgetActivityClass(entry.kind), entry.text,
                        entry.createdDate, entry.iconId);
                }
            }

            if (entries.length > 0) {
                AskJoobyWidgetPinTranscriptStart(entries[0].createdDate);
                AskJoobyWidgetPinAuditLinkToEnd(entries[entries.length - 1].createdDate);
            }

            if (entries.length === 0) {
                AskJoobyWidgetStartConversation(AskJoobyWidgetConversationId);
            }
        })
        .fail(function () {
            AskJoobyWidgetStartNewConversation();
        });
}

function AskJoobyWidgetStartNewConversation() {
    AskJoobyWidgetConversationId = null;
    AskJoobyWidgetStoreConversationId(null);
    AskJoobyWidgetAssistantBubble = null;
    AskJoobyWidgetAssistantText = "";
    AskJoobyWidgetCloseAllThinkingPasses();
    AskJoobyWidgetTranscript.empty();
    AskJoobyWidgetFollowingEnd = true;
    AskJoobyWidgetPinAuditLinkToEnd();
    AskJoobyWidgetActiveSelectionRow = null;

    AskJoobyWidgetCreateConversationAsync().then(function () {
        AskJoobyWidgetStartConversation(AskJoobyWidgetConversationId);
    });
}

function AskJoobyWidgetCreateConversationAsync() {
    return $.post("/api/AskJoobyConversation", function (conversation) {
        AskJoobyWidgetConversationId = conversation.id;
        AskJoobyWidgetStoreConversationId(conversation.id);
    });
}

function AskJoobyWidgetSetComposerBusy(busy, allowStop) {
    if (allowStop === undefined) {
        allowStop = true;
    }

    AskJoobyWidgetAllowStopCurrentReply = allowStop;
    AskJoobyWidgetSendButton.text(busy ? "Stop" : "Send");

    const sendButton = AskJoobyWidgetSendButton.data("kendoButton");
    if (sendButton) {
        sendButton.enable(busy ? allowStop : !AskJoobyWidgetOffline);
    }
}

function AskJoobyWidgetBuildStreamSubscriber(isGreeting) {
    return {
        next: function (item) {
            switch (item.kind) {
                case "Text":
                    AskJoobyWidgetCloseAllThinkingPasses();
                    if (AskJoobyWidgetAssistantBubble === null) {
                        AskJoobyWidgetHideThinking();
                        AskJoobyWidgetAssistantTimestamp = new Date();
                        AskJoobyWidgetAssistantBubble = AskJoobyWidgetAppendMessage("Assistant", "", null, null,
                            null, null, null, isGreeting, AskJoobyWidgetAssistantTimestamp);
                    }

                    if (item.text) {
                        AskJoobyWidgetAssistantText += item.text;
                        AskJoobyWidgetAssistantBubble.html(AskJoobyRenderMarkdown(AskJoobyWidgetAssistantText));
                    }

                    if (item.messageId) {
                        if (!isGreeting) {
                            AskJoobyWidgetAppendFeedbackControls(AskJoobyWidgetAssistantBubble.parent(),
                                item.messageId);
                        }

                        AskJoobyWidgetPrependAttachments(AskJoobyWidgetAssistantBubble, item.attachments);
                    }

                    AskJoobyWidgetScrollToBottom();
                    break;
                case "Thinking":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetAppendOrUpdateThinkingChunk(item.agentLabel, item.text);
                    break;
                case "Step":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetCloseThinkingPass(item.agentLabel);
                    AskJoobyWidgetAppendActivity("askJoobyStep", item.text, null, item.iconId);
                    AskJoobyWidgetShowThinking();
                    break;
                case "Thought":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetCloseThinkingPass(item.agentLabel);
                    AskJoobyWidgetAppendActivity("askJoobyThought", item.text);
                    AskJoobyWidgetShowThinking();
                    break;
                case "Guard":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetAppendActivity("askJoobyGuardBanner", item.text);
                    break;
                case "Delegation":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetCloseThinkingPass(item.agentLabel);
                    AskJoobyWidgetAppendActivity("askJoobyDelegationBanner", item.text);
                    AskJoobyWidgetShowThinking();
                    break;
                case "Validation":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetCloseThinkingPass(item.agentLabel);
                    AskJoobyWidgetAppendActivity("askJoobyValidationBanner", item.text);
                    AskJoobyWidgetShowThinking();
                    break;
                case "Stats":
                    if (AskJoobyWidgetAssistantBubble !== null) {
                        AskJoobyWidgetAppendMeta(AskJoobyWidgetAssistantBubble.parent(),
                            AskJoobyWidgetAssistantTimestamp, item.inputTokens, item.outputTokens,
                            item.durationMs, item.cachedInputTokens, item.joobrikCount,
                            item.distinctJoobrikCount, item.totalCost);
                    }

                    break;
                case "Suggestions":
                    AskJoobyWidgetAppendSuggestions(item.suggestions);
                    break;
                case "Clarification":
                    AskJoobyWidgetAppendClarification(item.options, item.allowFreeText);
                    break;
                case "Error":
                    AskJoobyWidgetAssistantBubble = null;
                    AskJoobyWidgetAssistantText = "";
                    AskJoobyWidgetCloseAllThinkingPasses();
                    AskJoobyWidgetAppendActivity("askJoobyError", item.text);
                    break;
                default:
                    break;
            }
        },
        error: function (error) {
            AskJoobyWidgetStreamHandle = null;
            AskJoobyWidgetHideThinking();
            AskJoobyWidgetSetComposerBusy(false);
            AskJoobyWidgetAppendActivity("askJoobyError", "Something went wrong: " + error);
        },
        complete: function () {
            AskJoobyWidgetStreamHandle = null;
            AskJoobyWidgetHideThinking();
            AskJoobyWidgetSetComposerBusy(false);
        }
    };
}

function AskJoobyWidgetSerializedPageContext() {
    const context = AskJoobyPageContext ? {...AskJoobyPageContext} : {};

    if (Object.keys(context).length === 0) {
        return null;
    }

    return JSON.stringify(context, null, 2);
}

function AskJoobyWidgetStreamMessage(conversationId, text) {
    AskJoobyWidgetAssistantBubble = null;
    AskJoobyWidgetAssistantText = "";
    AskJoobyWidgetCloseAllThinkingPasses();
    AskJoobyWidgetShowThinking();
    AskJoobyWidgetSetComposerBusy(true);

    AskJoobyWidgetConnect().then(function (connection) {
        AskJoobyWidgetStreamHandle = connection.stream("SendMessageAsync", conversationId, text,
            AskJoobyWidgetSerializedPageContext(), []).subscribe(AskJoobyWidgetBuildStreamSubscriber());
    }, function (error) {
        AskJoobyWidgetHideThinking();
        AskJoobyWidgetSetComposerBusy(false);
        AskJoobyWidgetAppendActivity("askJoobyError", "Could not connect: " + error);
    });
}

function AskJoobyWidgetStartConversation(conversationId) {
    AskJoobyWidgetAssistantBubble = null;
    AskJoobyWidgetAssistantText = "";
    AskJoobyWidgetCloseAllThinkingPasses();

    if (AskJoobyWidgetOffline) {
        AskJoobyWidgetFetchOfflineWelcomeAsync().then(function (welcome) {
            AskJoobyWidgetAppendMessage("Assistant", welcome, null, null, null, null, null, true, new Date());
        });
        return;
    }

    AskJoobyWidgetShowThinking();
    AskJoobyWidgetSetComposerBusy(true, false);

    AskJoobyWidgetConnect().then(function (connection) {
        AskJoobyWidgetStreamHandle = connection.stream(
            "StartConversationAsync", conversationId, AskJoobyWidgetSerializedPageContext())
            .subscribe(AskJoobyWidgetBuildStreamSubscriber(true));
    }, function (error) {
        AskJoobyWidgetHideThinking();
        AskJoobyWidgetSetComposerBusy(false);
        AskJoobyWidgetAppendActivity("askJoobyError", "Could not connect: " + error);
    });
}

function AskJoobyWidgetStopCurrentReply() {
    if (AskJoobyWidgetStreamHandle === null) {
        return;
    }

    AskJoobyWidgetStreamHandle.dispose();
    AskJoobyWidgetStreamHandle = null;
    AskJoobyWidgetAssistantBubble = null;
    AskJoobyWidgetAssistantText = "";
    AskJoobyWidgetCloseAllThinkingPasses();
    AskJoobyWidgetHideThinking();
    AskJoobyWidgetSetComposerBusy(false);
    AskJoobyWidgetAppendActivity("askJoobyStep", "Stopped.");
}

function AskJoobyWidgetHandleComposerButtonClick() {
    if (AskJoobyWidgetStreamHandle !== null) {
        if (AskJoobyWidgetAllowStopCurrentReply) {
            AskJoobyWidgetStopCurrentReply();
        }

        return;
    }

    AskJoobyWidgetSendCurrentMessage();
}

function AskJoobyWidgetCheckedSelectionOptions() {
    if (AskJoobyWidgetActiveSelectionRow === null) {
        return [];
    }

    return AskJoobyWidgetActiveSelectionRow.find(".askJoobySelectableInput:checked")
        .map(function () {
            return $(this).val();
        }).get();
}

function AskJoobyWidgetJoinSelections(items) {
    if (items.length <= 1) {
        return items.join("");
    }

    const quoted = items.map(function (item) {
        return "\"" + item + "\"";
    });

    return "I selected all of these, combine them: " + quoted.join(", ");
}

function AskJoobyWidgetBuildSelectionAwareMessage(typedText) {
    const checked = AskJoobyWidgetCheckedSelectionOptions();
    if (checked.length === 0) {
        return typedText;
    }

    const selectedPart = AskJoobyWidgetJoinSelections(checked);
    return typedText === "" ? selectedPart : selectedPart + " -- " + typedText;
}

function AskJoobyWidgetSendCurrentMessage() {
    if (AskJoobyWidgetOffline) {
        return;
    }

    const input = AskJoobyWidgetInput;
    const typedText = input.val().trim();
    const text = AskJoobyWidgetBuildSelectionAwareMessage(typedText);

    if (text === "") {
        return;
    }

    AskJoobyWidgetDisableSelectionRow(AskJoobyWidgetActiveSelectionRow);
    AskJoobyWidgetActiveSelectionRow = null;

    input.val("");
    AskJoobyWidgetAppendMessage("User", text, null, null, null, null, null, false, new Date());

    if (AskJoobyWidgetConversationId === null) {
        AskJoobyWidgetCreateConversationAsync().then(function () {
            AskJoobyWidgetStreamMessage(AskJoobyWidgetConversationId, text);
        });
    } else {
        AskJoobyWidgetStreamMessage(AskJoobyWidgetConversationId, text);
    }
}

$(function () {
    AskJoobyWidgetTranscript = $("#askJoobyWidgetTranscript");
    AskJoobyWidgetSendButton = $("#askJoobyWidgetSend");
    AskJoobyWidgetInput = $("#askJoobyWidgetInput");

    AskJoobyWidgetTranscript.on("scroll", function () {
        AskJoobyWidgetFollowingEnd =
            this.scrollHeight - this.scrollTop - this.clientHeight <= 48;
    });

    const askJoobyWidgetLauncher = $("#askJoobyWidgetLauncher");

    function AskJoobyWidgetOpenPanel() {
        const panel = $("#askJoobyWidgetPanel");
        panel.show();
        askJoobyWidgetLauncher
            .addClass("askJoobyLauncherOpen")
            .attr("aria-expanded", "true")
            .attr("aria-label", "Close chat");

        if (!AskJoobyWidgetHistoryLoaded) {
            AskJoobyWidgetConversationId = AskJoobyWidgetLoadStoredConversationId();
        }

        if (AskJoobyWidgetConversationId === null) {
            AskJoobyWidgetHistoryLoaded = true;
            AskJoobyWidgetCreateConversationAsync().then(function () {
                AskJoobyWidgetStartConversation(AskJoobyWidgetConversationId);
            });
        } else {
            AskJoobyWidgetLoadHistoryIfNeeded();
        }
    }

    function AskJoobyWidgetClosePanel() {
        $("#askJoobyWidgetPanel").hide();
        askJoobyWidgetLauncher
            .removeClass("askJoobyLauncherOpen")
            .attr("aria-expanded", "false")
            .attr("aria-label", "Open chat");
    }

    askJoobyWidgetLauncher.on("click", function () {
        if ($("#askJoobyWidgetPanel").is(":visible")) {
            AskJoobyWidgetClosePanel();
        } else {
            AskJoobyWidgetOpenPanel();
        }
    });

    $(document).on("keydown", function (e) {
        if (e.key === "Escape" && $("#askJoobyWidgetPanel").is(":visible")) {
            AskJoobyWidgetClosePanel();
            askJoobyWidgetLauncher.trigger("focus");
        }
    });

    $("#askJoobyWidgetNew").on("click", AskJoobyWidgetStartNewConversation);

    AskJoobyWidgetSendButton.kendoButton({
        click: AskJoobyWidgetHandleComposerButtonClick
    });

    AskJoobyWidgetSendButton.data("kendoButton").enable(!AskJoobyWidgetOffline);

    AskJoobyWidgetInput.kendoTextArea();

    AskJoobyWidgetInput.on("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            AskJoobyWidgetHandleComposerButtonClick();
        }
    });
});
