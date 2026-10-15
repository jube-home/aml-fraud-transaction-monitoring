const AskJoobyConnectionState = {Connected: "Connected", Disconnected: "Disconnected"};

function AskJoobyStreamEndpointFor(name, args) {
    switch (name) {
        case "SendMessageAsync":
            return {
                url: "/api/AskJoobyConversation/" + args[0] + "/SendMessage",
                body: {text: args[1] || "", pageContext: args[2] || "", attachmentFileIds: args[3] || []}
            };
        case "StartConversationAsync":
            return {
                url: "/api/AskJoobyConversation/" + args[0] + "/Start",
                body: {pageContext: args[1] || ""}
            };
        case "SubmitFeedbackAsync":
            return {
                url: "/api/AskJoobyConversation/" + args[0] + "/Messages/" + args[1] + "/Feedback",
                body: {isPositive: args[2]}
            };
        default:
            return null;
    }
}

function AskJoobyStreamPost(target, signal) {
    return fetch(target.url, {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify(target.body),
        signal: signal
    }).then(function (response) {
        const refreshed = response.headers.get("X-Session-Expiry");

        if (refreshed && window.JubeSessionExpiry) {
            window.JubeSessionExpiry.set(refreshed);
        }

        return response;
    });
}

function AskJoobyStreamDispatchFrame(frame, subscriber, state) {
    let event = "item";
    const data = [];

    frame.split("\n").forEach(function (line) {
        if (line.startsWith("event:")) {
            event = line.slice(6).trim();
        } else if (line.startsWith("data:")) {
            data.push(line.slice(5).replace(/^ /, ""));
        }
    });

    if (data.length === 0) {
        return;
    }

    const payload = data.join("\n");
    if (event === "complete") {
        state.completed = true;
        subscriber.complete();
        return;
    }

    if (event === "error") {
        state.completed = true;
        let message;
        try {
            message = JSON.parse(payload).message || payload;
        } catch (ignored) {
            message = payload;
        }

        subscriber.error(new Error(message));
        return;
    }

    subscriber.next(JSON.parse(payload));
}

function AskJoobyStreamPump(response, subscriber, state) {
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";

    function read() {
        return reader.read().then(function (result) {
            if (result.done) {
                if (!state.completed) {
                    state.completed = true;
                    subscriber.complete();
                }

                return;
            }

            buffer += decoder.decode(result.value, {stream: true});

            let boundary = buffer.indexOf("\n\n");
            while (boundary !== -1 && !state.completed) {
                AskJoobyStreamDispatchFrame(buffer.slice(0, boundary), subscriber, state);
                buffer = buffer.slice(boundary + 2);
                boundary = buffer.indexOf("\n\n");
            }

            if (state.completed) {
                return;
            }

            return read();
        });
    }

    return read();
}

function AskJoobyCreateConnection() {
    return {
        state: AskJoobyConnectionState.Connected,
        start: function () {
            return Promise.resolve();
        },
        onreconnected: function () {
        },
        onclose: function () {
        },
        invoke: function () {
            const target = AskJoobyStreamEndpointFor(arguments[0], Array.prototype.slice.call(arguments, 1));
            if (target === null) {
                return Promise.reject(new Error("Unknown AskJooby operation '" + arguments[0] + "'."));
            }

            return AskJoobyStreamPost(target, undefined).then(function (response) {
                if (!response.ok) {
                    return response.json().catch(function () {
                        return null;
                    }).then(function (body) {
                        const detail = body && body.errors && body.errors.length > 0
                            ? body.errors[0].errorMessage
                            : null;

                        throw new Error(detail || ("AskJooby request failed with status " + response.status + "."));
                    });
                }

                return response.json();
            });
        },
        stream: function () {
            const target = AskJoobyStreamEndpointFor(arguments[0], Array.prototype.slice.call(arguments, 1));

            return {
                subscribe: function (subscriber) {
                    const controller = new AbortController();
                    const state = {completed: false};

                    if (target === null) {
                        subscriber.error(new Error("Unknown AskJooby operation."));
                        return {
                            dispose: function () {
                            }
                        };
                    }

                    AskJoobyStreamPost(target, controller.signal).then(function (response) {
                        if (!response.ok) {
                            throw new Error("AskJooby stream failed with status " + response.status + ".");
                        }

                        return AskJoobyStreamPump(response, subscriber, state);
                    }).catch(function (error) {
                        if (state.completed) {
                            return;
                        }

                        state.completed = true;
                        if (error && error.name === "AbortError") {
                            subscriber.complete();
                            return;
                        }

                        subscriber.error(error);
                    });

                    return {
                        dispose: function () {
                            if (state.completed) {
                                return;
                            }

                            state.completed = true;
                            controller.abort();
                        }
                    };
                }
            };
        }
    };
}
