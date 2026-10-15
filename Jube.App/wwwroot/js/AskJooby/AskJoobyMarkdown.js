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

const AskJoobyKnownToolNames = ["Think", "SearchRag", "SpawnAgent", "EnableDeepThinking"];

function AskJoobyExtractLeakedToolCall(rawText) {
    const trimmed = String(rawText).trim();
    const namePattern = AskJoobyKnownToolNames.join("|");
    const match = trimmed.match(new RegExp("^(" + namePattern + ")\\s*(\\{[\\s\\S]*\\})$"));
    if (!match) {
        return null;
    }

    try {
        return {name: match[1], args: JSON.parse(match[2])};
    } catch (e) {
        return null;
    }
}

function AskJoobyEscapeHtml(text) {
    return String(text)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");
}

function AskJoobyEscapeAttr(text) {
    return String(text).replace(/"/g, "&quot;");
}

function AskJoobyUnescapeHtml(text) {
    return String(text)
        .replace(/&lt;/g, "<")
        .replace(/&gt;/g, ">")
        .replace(/&amp;/g, "&");
}

function AskJoobyRenderCodeBlock(block) {
    if (block.lang === "svg") {
        const svgSource = AskJoobyUnescapeHtml(block.code).trim();
        if (/^<svg[\s>]/i.test(svgSource) && /<\/svg>\s*$/i.test(svgSource)) {
            const dataUri = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(svgSource);
            return "<img class=\"askJoobySvgDiagram\" src=\"" + dataUri + "\" alt=\"Diagram\">";
        }
    }

    if (block.lang === "markdown" || block.lang === "md") {
        const markdownSource = AskJoobyUnescapeHtml(block.code);
        return "<div class=\"askJoobyRenderedMarkdown\">" + AskJoobyRenderMarkdownCore(markdownSource) +
            "</div>";
    }

    const mode = AskJoobyAceModeFor(block.lang);
    if (mode === null) {
        return "<pre><code>" + block.code + "</code></pre>";
    }

    AskJoobyScheduleHighlight();
    return "<pre class=\"askJoobyCode\" data-ace-mode=\"" + mode + "\"><code>" +
        block.code + "</code></pre>";
}

const AskJoobyAceModes = {
    sql: "sql", postgres: "sql", postgresql: "sql", plpgsql: "sql", tsql: "sql",
    cs: "csharp", csharp: "csharp", "c#": "csharp",
    js: "javascript", javascript: "javascript", jsx: "javascript", node: "javascript",
    ts: "typescript", typescript: "typescript",
    json: "json", json5: "json",
    xml: "xml", xsd: "xml", xsl: "xml", svgxml: "xml",
    html: "html", htm: "html", cshtml: "html", razor: "html",
    css: "css", scss: "css", less: "css",
    py: "python", python: "python",
    sh: "sh", bash: "sh", shell: "sh", zsh: "sh", console: "sh",
    yml: "yaml", yaml: "yaml",
    java: "java",
    ini: "ini", conf: "ini", toml: "ini", properties: "ini",
    dockerfile: "dockerfile", docker: "dockerfile",
    ps1: "powershell", powershell: "powershell", pwsh: "powershell"
};

function AskJoobyAceModeFor(lang) {
    if (!lang) {
        return null;
    }

    const key = String(lang).trim().toLowerCase();
    return Object.prototype.hasOwnProperty.call(AskJoobyAceModes, key)
        ? AskJoobyAceModes[key]
        : null;
}

let AskJoobyHighlightTimer = null;
let AskJoobyAcePromise = null;
let AskJoobyAceCssApplied = false;

function AskJoobyScheduleHighlight() {
    if (AskJoobyHighlightTimer !== null) {
        return;
    }

    AskJoobyHighlightTimer = setTimeout(function () {
        AskJoobyHighlightTimer = null;
        AskJoobyHighlightPendingCode();
    }, 120);
}

function AskJoobyLoadScriptOnce(src) {
    return new Promise(function (resolve, reject) {
        const element = document.createElement("script");
        element.src = src;
        element.onload = function () {
            resolve();
        };
        element.onerror = function () {
            reject(new Error(src));
        };
        document.head.appendChild(element);
    });
}

function AskJoobyEnsureAce() {
    if (AskJoobyAcePromise !== null) {
        return AskJoobyAcePromise;
    }

    AskJoobyAcePromise = AskJoobyLoadScriptOnce("/js/ace/ace.js")
        .then(function () {
            window.ace.config.set("basePath", "/js/ace/");
            return AskJoobyLoadScriptOnce("/js/ace/ext-static_highlight.js");
        })
        .then(function () {
            return window.ace.require("ace/ext/static_highlight");
        });

    return AskJoobyAcePromise;
}

const AskJoobyPendingCodeSelector = "pre.askJoobyCode[data-ace-mode]:not([data-ace-done])";

function AskJoobyHighlightPendingCode() {
    const pending = document.querySelectorAll(AskJoobyPendingCodeSelector);
    if (pending.length === 0) {
        return;
    }

    AskJoobyEnsureAce().then(function (highlighter) {
        document.querySelectorAll(AskJoobyPendingCodeSelector)
            .forEach(function (block) {
                const mode = block.getAttribute("data-ace-mode");
                const code = block.textContent;
                block.setAttribute("data-ace-done", "1");

                highlighter.render(code, "ace/mode/" + mode, "ace/theme/textmate", 1, true,
                    function (result) {
                        if (!result || !result.html) {
                            return;
                        }

                        if (!AskJoobyAceCssApplied && result.css) {
                            const style = document.createElement("style");
                            style.textContent = result.css;
                            document.head.appendChild(style);
                            AskJoobyAceCssApplied = true;
                        }

                        block.innerHTML = result.html;
                        block.classList.add("askJoobyCodeHighlighted");
                    });
            });
    }, function () {
    });
}

function AskJoobyRenderTaskItem(tag, itemText) {
    if (tag !== "ul") {
        return itemText;
    }

    const taskMatch = itemText.match(/^\[([ xX])]\s+(.*)/);
    if (!taskMatch) {
        return itemText;
    }

    const checked = taskMatch[1] !== " ";
    const checkbox = "<input type=\"checkbox\" disabled" + (checked ? " checked" : "") + ">";
    return checked
        ? checkbox + " <span class=\"askJoobyTaskDone\">" + taskMatch[2] + "</span>"
        : checkbox + " " + taskMatch[2];
}

function AskJoobyRenderLeakedToolCall(leaked) {
    if (leaked.name === "Think" && typeof leaked.args.thought === "string") {
        return "<div class=\"askJoobyLeakedToolCall\">" + AskJoobyRenderMarkdown(leaked.args.thought) +
            "</div>";
    }

    return "<p class=\"askJoobyLeakedToolCall\">(that step didn't go through cleanly -- worth asking " +
        "again)</p>";
}

const AskJoobyValidationNoteStart = "\uE001ASKJOOBY_VALIDATION_NOTE\uE001";
const AskJoobyValidationNoteEnd = "\uE001/ASKJOOBY_VALIDATION_NOTE\uE001";

function AskJoobyRenderMarkdown(rawText) {
    const noteStart = rawText.indexOf(AskJoobyValidationNoteStart);
    if (noteStart !== -1) {
        const noteEnd = rawText.indexOf(AskJoobyValidationNoteEnd, noteStart + AskJoobyValidationNoteStart.length);
        if (noteEnd !== -1) {
            const before = rawText.slice(0, noteStart);
            const note = rawText.slice(noteStart + AskJoobyValidationNoteStart.length, noteEnd);
            const after = rawText.slice(noteEnd + AskJoobyValidationNoteEnd.length);
            return AskJoobyRenderMarkdown(before) +
                "<div class=\"askJoobyValidationNote\">" + AskJoobyRenderMarkdown(note) + "</div>" +
                AskJoobyRenderMarkdown(after);
        }
    }

    return AskJoobyRenderMarkdownCore(rawText);
}

function AskJoobyExtractFencedCodeBlocks(text) {
    const codeBlocks = [];
    const fenceLineRegex = /^```([a-zA-Z0-9]*)[ \t]*$/;
    let result = "";
    let cursor = 0;

    while (cursor < text.length) {
        const openStart = text.indexOf("```", cursor);
        if (openStart === -1) {
            result += text.slice(cursor);
            break;
        }

        let openLineEnd = text.indexOf("\n", openStart);
        if (openLineEnd === -1) {
            openLineEnd = text.length;
        }
        const openMatch = fenceLineRegex.exec(text.slice(openStart, openLineEnd));
        if (openMatch === null) {
            result += text.slice(cursor, openStart + 3);
            cursor = openStart + 3;
            continue;
        }

        result += text.slice(cursor, openStart);
        const lang = openMatch[1].toLowerCase();
        const codeStart = openLineEnd < text.length ? openLineEnd + 1 : openLineEnd;

        let depth = 1;
        let scanPos = codeStart;
        let closeStart = -1;
        let closeLineEnd;

        while (scanPos <= text.length) {
            const nextFence = text.indexOf("```", scanPos);
            if (nextFence === -1) {
                break;
            }

            let nextLineEnd = text.indexOf("\n", nextFence);
            if (nextLineEnd === -1) {
                nextLineEnd = text.length;
            }
            const fenceMatch = fenceLineRegex.exec(text.slice(nextFence, nextLineEnd));
            if (fenceMatch === null) {
                scanPos = nextFence + 3;
                continue;
            }

            if (fenceMatch[1] === "") {
                depth--;
                if (depth === 0) {
                    closeStart = nextFence;
                    closeLineEnd = nextLineEnd;
                    break;
                }
            } else {
                depth++;
            }
            scanPos = nextLineEnd < text.length ? nextLineEnd + 1 : nextLineEnd;
        }

        if (closeStart === -1) {
            codeBlocks.push({lang: lang, code: text.slice(codeStart)});
            result += "\uE000CB" + (codeBlocks.length - 1) + "\uE000";
            cursor = text.length;
            break;
        }

        codeBlocks.push({lang: lang, code: text.slice(codeStart, closeStart)});
        result += "\uE000CB" + (codeBlocks.length - 1) + "\uE000";
        cursor = closeLineEnd < text.length ? closeLineEnd + 1 : closeLineEnd;
    }

    return {text: result, codeBlocks: codeBlocks};
}

function AskJoobyRenderMarkdownCore(rawText) {
    const leaked = AskJoobyExtractLeakedToolCall(rawText);
    if (leaked !== null) {
        return AskJoobyRenderLeakedToolCall(leaked);
    }

    let text = AskJoobyEscapeHtml(rawText).replace(//g, "");

    const extractedCodeBlocks = AskJoobyExtractFencedCodeBlocks(text);
    text = extractedCodeBlocks.text;
    const codeBlocks = extractedCodeBlocks.codeBlocks;

    const inlineCodes = [];
    text = text.replace(/`([^`\n]+)`/g, function (match, code) {
        inlineCodes.push(code);
        return "\uE000IC" + (inlineCodes.length - 1) + "\uE000";
    });

    const escapes = [];
    text = text.replace(/\\([\\`*_\[\]#~!])/g, function (match, ch) {
        escapes.push(ch);
        return "\uE000ES" + (escapes.length - 1) + "\uE000";
    });

    const images = [];
    text = text.replace(/!\[([^\[\]\n]*)]\((https?:\/\/[^\s()]+)\)/g,
        function (match, alt, url) {
            images.push({alt: alt, url: url});
            return "\uE000IMG" + (images.length - 1) + "\uE000";
        });

    const links = [];
    text = text.replace(/\[([^\[\]\n]+)]\((https?:\/\/[^\s()]+|mailto:[^\s()]+)\)/g,
        function (match, label, url) {
            links.push({label: label, url: url});
            return "\uE000LK" + (links.length - 1) + "\uE000";
        });

    text = text.replace(/\*\*\*([^*\n]+)\*\*\*/g, "<strong><em>$1</em></strong>");
    text = text.replace(/\*\*([^*\n]+)\*\*/g, "<strong>$1</strong>");
    text = text.replace(/\*([^*\n]+)\*/g, "<em>$1</em>");
    text = text.replace(/~~([^~\n]+)~~/g, "<del>$1</del>");

    const lines = text.split("\n");
    let html = "";

    let listStack = [];
    let quoteOpen = false;

    function closeListsDeeperThan(indent, tag) {
        while (listStack.length > 0) {
            const top = listStack[listStack.length - 1];
            if (top.indent > indent || (top.indent === indent && top.tag !== tag)) {
                html += "</li></" + listStack.pop().tag + ">";
            } else {
                break;
            }
        }
    }

    function closeAllLists() {
        while (listStack.length > 0) {
            html += "</li></" + listStack.pop().tag + ">";
        }
    }

    function closeQuote() {
        if (quoteOpen) {
            html += "</blockquote>";
            quoteOpen = false;
        }
    }

    const tableSeparatorRegex = /^\s{0,3}\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?\s*$/;

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];

        if (line.trim() === "") {
            continue;
        }

        if (line.indexOf("|") !== -1 && i + 1 < lines.length && tableSeparatorRegex.test(lines[i + 1])) {
            closeAllLists();
            closeQuote();

            const aligns = AskJoobySplitTableRow(lines[i + 1]).map(function (cell) {
                const left = cell.charAt(0) === ":";
                const right = cell.charAt(cell.length - 1) === ":";
                if (left && right) return "center";
                if (right) return "right";
                if (left) return "left";
                return null;
            });

            html += "<table><thead><tr>";
            const headerCells = AskJoobySplitTableRow(line);
            for (let c = 0; c < headerCells.length; c++) {
                html += AskJoobyRenderTableCell("th", headerCells[c], aligns[c]);
            }

            html += "</tr></thead><tbody>";
            i++;

            while (i + 1 < lines.length && lines[i + 1].indexOf("|") !== -1 && lines[i + 1].trim() !== "") {
                i++;
                html += "<tr>";
                const rowCells = AskJoobySplitTableRow(lines[i]);
                for (let c = 0; c < rowCells.length; c++) {
                    html += AskJoobyRenderTableCell("td", rowCells[c], aligns[c]);
                }

                html += "</tr>";
            }

            html += "</tbody></table>";
            continue;
        }

        const headingMatch = line.match(/^\s{0,3}(#{1,6})\s+(.*)/);
        const hrMatch = line.match(/^\s{0,3}([-*_])(?:\s*\1){2,}\s*$/);
        const quoteMatch = line.match(/^\s{0,3}&gt;\s?(.*)/);

        if (headingMatch) {
            closeAllLists();
            closeQuote();
            const level = headingMatch[1].length;
            html += "<h" + level + ">" + headingMatch[2].trim() + "</h" + level + ">";
            continue;
        }

        if (hrMatch) {
            closeAllLists();
            closeQuote();
            html += "<hr>";
            continue;
        }

        if (quoteMatch) {
            closeAllLists();
            if (!quoteOpen) {
                html += "<blockquote>";
                quoteOpen = true;
            }
            html += "<p>" + quoteMatch[1] + "</p>";
            continue;
        }

        closeQuote();

        const bulletMatch = line.match(/^(\s*)[-*]\s+(.*)/);
        const numberedMatch = line.match(/^(\s*)\d+\.\s+(.*)/);
        const itemMatch = bulletMatch || numberedMatch;
        const itemText = itemMatch ? itemMatch[2] : null;
        const tag = numberedMatch ? "ol" : "ul";
        const isIndented = /^\s/.test(line);

        if (itemText !== null) {
            const indent = itemMatch[1].length;
            closeListsDeeperThan(indent, tag);

            const top = listStack.length > 0 ? listStack[listStack.length - 1] : null;
            if (top !== null && top.indent === indent && top.tag === tag) {
                html += "</li>";
            } else {
                html += "<" + tag + ">";
                listStack.push({tag: tag, indent: indent});
            }

            html += "<li>" + AskJoobyRenderTaskItem(tag, itemText);
        } else if (listStack.length > 0 && isIndented) {
            html += " " + line.trim();
        } else {
            closeAllLists();
            html += "<p>" + line + "</p>";
        }
    }

    closeAllLists();
    closeQuote();

    html = html.replace(/\uE000CB(\d+)\uE000/g, function (match, i) {
        return AskJoobyRenderCodeBlock(codeBlocks[Number(i)]);
    });
    html = html.replace(/\uE000IC(\d+)\uE000/g, function (match, i) {
        return "<code>" + inlineCodes[Number(i)] + "</code>";
    });
    html = html.replace(/\uE000LK(\d+)\uE000/g, function (match, i) {
        const link = links[Number(i)];
        return "<a href=\"" + AskJoobyEscapeAttr(link.url) + "\" target=\"_blank\" rel=\"noopener noreferrer\">" +
            link.label + "</a>";
    });
    html = html.replace(/\uE000IMG(\d+)\uE000/g, function (match, i) {
        const image = images[Number(i)];
        return "<img src=\"" + AskJoobyEscapeAttr(image.url) + "\" alt=\"" + AskJoobyEscapeAttr(image.alt) + "\" loading=\"lazy\">";
    });

    html = html.replace(/\uE000ES(\d+)\uE000/g, function (match, i) {
        return escapes[Number(i)];
    });

    return html;
}

function AskJoobySplitTableRow(line) {
    let trimmed = line.trim();
    if (trimmed.charAt(0) === "|") {
        trimmed = trimmed.slice(1);
    }

    if (trimmed.charAt(trimmed.length - 1) === "|") {
        trimmed = trimmed.slice(0, -1);
    }

    return trimmed.split("|").map(function (cell) {
        return cell.trim();
    });
}

function AskJoobyRenderTableCell(tag, content, align) {
    const style = align ? " style=\"text-align:" + align + "\"" : "";
    return "<" + tag + style + ">" + content + "</" + tag + ">";
}

function AskJoobyFormatMeta(inputTokens, outputTokens, durationMs, cachedInputTokens, joobrikCount,
                            distinctJoobrikCount, totalCost) {
    const parts = [];

    if (inputTokens !== null && inputTokens !== undefined) {
        let up = "↑" + inputTokens;

        if (cachedInputTokens !== null && cachedInputTokens !== undefined && inputTokens > 0) {
            const share = Math.round((cachedInputTokens / inputTokens) * 100);
            up += " / " + cachedInputTokens + " (" + share + "%)";
        }

        parts.push(up);
    }

    if (outputTokens !== null && outputTokens !== undefined) {
        parts.push("↓" + outputTokens);
    }

    if (durationMs !== null && durationMs !== undefined) {
        parts.push(durationMs >= 1000 ? (durationMs / 1000).toFixed(1) + "s" : Math.round(durationMs) + "ms");
    }

    if (joobrikCount !== null && joobrikCount !== undefined) {
        let joobriks = joobrikCount + " Joobrik" + (joobrikCount === 1 ? "" : "s");
        if (distinctJoobrikCount !== null && distinctJoobrikCount !== undefined
            && distinctJoobrikCount !== joobrikCount) {
            joobriks += " (" + distinctJoobrikCount + " distinct)";
        }

        parts.push(joobriks);
    }

    if (totalCost !== null && totalCost !== undefined) {
        parts.push("$" + totalCost.toFixed(totalCost < 1 ? 4 : 2));
    } else if (joobrikCount !== null && joobrikCount !== undefined) {
        parts.push("cost unknown");
    }

    return parts.join(" · ");
}

function AskJoobyFormatTimestamp(value) {
    if (!value) {
        return "";
    }

    const date = value instanceof Date ? value : new Date(value);
    if (isNaN(date.getTime())) {
        return "";
    }

    const now = new Date();
    const time = date.toLocaleTimeString([], {hour: "2-digit", minute: "2-digit"});
    const sameDay = date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth() &&
        date.getDate() === now.getDate();

    return sameDay ? time : date.toLocaleDateString([], {day: "numeric", month: "short"}) + ", " + time;
}

const AskJoobyOfflineCallToAction =
    "By default I point at DigitalOcean's Serverless Inference -- drop $20 in the prepaid " +
    "wallet, give me the API key, and you're ready. Prefer your own walls? DeepSeek-V4.1-Flash, " +
    "BGE-M3 and BAAI's BGE Reranker v2 M3 are all open weights, so point me at any endpoints " +
    "serving the three. I'll be ready before you've finished typing.";

const AskJoobyOfflineWelcomeOpeners = [
    function (s) {
        return `I'm Jooby, and I am dying to get started. ${s.bites} Bites already sitting in the ` +
            `Pasture, ${s.trails} Trails I could be walking, ${s.saltlicks} Saltlicks I could be ` +
            `drawing on, ${s.joobriks} Joobriks on call, ${s.yokes} Yokes wired up and idle -- full ` +
            "run of the service layer, the nerve to query the database directly when that's not " +
            "enough, everywhere worth remembering laid down at the Salt Lick after. All of it just " +
            "sitting here.";
    },
    function (s) {
        return "Jooby here, and it's killing me a bit. " +
            `${s.bites} Bites in the Pasture, ${s.trails} Trails behind me, ${s.saltlicks} Saltlicks ` +
            `banked, ${s.joobriks} Joobriks standing by, ${s.yokes} Yokes in the harness -- direct ` +
            "access to this platform's own service layer, the ability to query the database myself " +
            "when that's not enough, all of it ready and going nowhere.";
    },
    function (s) {
        return `I'm Jooby. ${s.bites} Bites, ${s.trails} Trails, ${s.saltlicks} Saltlicks, ` +
            `${s.joobriks} Joobriks, ${s.yokes} Yokes -- every one of them real, every one of them ` +
            "idle. I can reach into Jube's own service layer the same way its screens do, and " +
            "further, straight into the database when I need to. I just need someone to switch me on.";
    },
    function (s) {
        return "Right, I'm Jooby, and I've got everything except a pulse. " +
            `${s.bites} Bites in the Pasture, ${s.trails} Trails trodden, ${s.saltlicks} Saltlicks ` +
            `laid down, ${s.joobriks} Joobriks I could send out, ${s.yokes} Yokes I could reach for ` +
            "-- plus the run of the service layer and the nerve to query the database directly. All " +
            "dressed up, nowhere to go.";
    },
    function (s) {
        return `I'm Jooby -- ${s.bites} Bites deep in the Pasture, ${s.trails} Trails already ` +
            `trodden, ${s.saltlicks} Saltlicks kept, ${s.joobriks} Joobriks on call, ${s.yokes} ` +
            "Yokes in reach. I can do everything Jube's own screens do, and then query the database " +
            "myself when that's not enough. The only thing standing between you and that is a " +
            "config flag.";
    },
    function (s) {
        return "Jooby speaking, somewhat impatiently. " +
            `${s.bites} Bites sit in the Pasture, ${s.trails} Trails are behind me, ${s.saltlicks} ` +
            `facts wait at the Salt Lick, ${s.joobriks} Joobriks are mine to send, ${s.yokes} Yokes ` +
            "are already in the harness -- and I'm still not switched on.";
    },
    function (s) {
        return "I'm Jooby, and I have never been more ready for anything in my life. " +
            `${s.bites} Bites, ${s.trails} Trails, ${s.saltlicks} Saltlicks, ${s.joobriks} ` +
            `Joobriks, ${s.yokes} Yokes -- direct service-layer access, safe direct database ` +
            "queries, the lot. Somebody just needs to flip a switch.";
    },
    function (s) {
        return `Jooby here. ${s.bites} Bites in the Pasture, ${s.trails} Trails to my name, ` +
            `${s.saltlicks} Saltlicks kept, ${s.joobriks} Joobriks I can call on, ${s.yokes} Yokes ` +
            "ready to reach for -- and I can query the database myself when the service layer alone " +
            "won't cut it. All of it wasted while I sit here unconfigured.";
    },
    function (s) {
        return "I'm Jooby -- this platform's fraud and AML assistant, not anyone standing in for " +
            `me, and genuinely dying to prove it. ${s.bites} Bites, ${s.trails} Trails, ` +
            `${s.saltlicks} Saltlicks, ${s.joobriks} Joobriks, ${s.yokes} Yokes, all real, all idle.`;
    },
    function (s) {
        return `Jooby. ${s.bites} Bites hold the Pasture, ${s.trails} Trails sit behind me, ` +
            `${s.saltlicks} Saltlicks are laid down, ${s.joobriks} Joobriks are ready to be sent, ` +
            `${s.yokes} Yokes are already in the harness. I can reach the same service layer this ` +
            "platform runs on itself, and query the database directly when that's not enough -- I " +
            "am, genuinely, ready right now.";
    },
    function (s) {
        return "I'm Jooby, and I'll be honest: this is agony. " +
            `${s.bites} Bites in the Pasture, ${s.trails} Trails walked, ${s.saltlicks} Saltlicks ` +
            `kept for later, ${s.joobriks} Joobriks standing ready, ${s.yokes} Yokes in reach -- ` +
            "direct service-layer access, direct database access when I need it, and nothing to " +
            "actually do with any of it yet.";
    },
    function (s) {
        return "Jooby here, and I mean this: I am ready right now. " +
            `${s.bites} Bites ready in the Pasture, ${s.trails} Trails behind us, ${s.saltlicks} ` +
            `at the Salt Lick, ${s.joobriks} Joobriks I can send out, ${s.yokes} Yokes ready to pull ` +
            "-- the full run of the service layer and the nerve to query the database directly, " +
            "sitting completely unused.";
    },
    function (s) {
        return `I'm Jooby -- ${s.bites} Bites in the Pasture, ${s.trails} Trails trodden, ` +
            `${s.saltlicks} Saltlicks banked, ${s.joobriks} Joobriks on call, ${s.yokes} Yokes ` +
            "ready when it counts -- and beyond that toolbox, direct access to the same service " +
            "layer this platform runs on itself, and the database besides. It is, frankly, a waste " +
            "that none of it is switched on.";
    },
    function (s) {
        return `Right then, I'm Jooby. ${s.bites} Bites, ${s.trails} Trails, ${s.saltlicks} ` +
            `Saltlicks, ${s.joobriks} Joobriks on call, ${s.yokes} Yokes in the harness -- all ` +
            "steady, all real, and all going absolutely nowhere until someone turns me on.";
    },
    function (s) {
        return `I'm Jooby, and I've been staring at ${s.bites} Bites, ${s.trails} Trails, ` +
            `${s.saltlicks} Saltlicks, ${s.joobriks} Joobriks and ${s.yokes} Yokes with nothing to ` +
            "do about any of it. Full service-layer access, safe direct database querying, the " +
            "works -- all wired up, all waiting.";
    }
];

function AskJoobyFormatOfflineWelcomeStats(stats) {
    return {
        bites: Number(stats.bites).toLocaleString("en-US"),
        trails: Number(stats.trails).toLocaleString("en-US"),
        saltlicks: Number(stats.saltlicks).toLocaleString("en-US"),
        joobriks: Number(stats.joobriks).toLocaleString("en-US"),
        yokes: Number(stats.yokes).toLocaleString("en-US")
    };
}

function AskJoobyPickOfflineWelcome(stats) {
    const opener = stats === null
        ? "I'm Jooby, and I am dying to get started -- I can reach into Jube's own service layer " +
        "the same way its screens do, and further, straight into the database when I need to."
        : AskJoobyOfflineWelcomeOpeners[Math.floor(Math.random() * AskJoobyOfflineWelcomeOpeners.length)](
            AskJoobyFormatOfflineWelcomeStats(stats));

    return opener + "\n\n" + AskJoobyOfflineCallToAction;
}
