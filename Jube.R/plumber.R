# Jube.R HTTP router. Unauthenticated by design (see README.md — the
# boundary is the deployment network, not this layer) — deliberately does
# no auth/session handling itself, only script-execution safety.

source("R/sandbox.R")
source("R/data_cache.R")

library(mirai)
library(promises)

STARTED_AT <- Sys.time()
DEFAULT_TIMEOUT_SECONDS <- as.numeric(Sys.getenv("JUBE_R_SCRIPT_TIMEOUT_SECONDS", "30"))
MAX_TIMEOUT_SECONDS <- as.numeric(Sys.getenv("JUBE_R_SCRIPT_TIMEOUT_SECONDS_MAX", "300"))
MAX_DATAFRAMES_PER_CALL <- 10L
MAX_VARIABLES_PER_CALL <- 50L
MAX_VARIABLE_STRING_CHARS <- 200000L
MAX_VARIABLE_VECTOR_ELEMENTS <- 100000L

# Request-body ceiling, independent of the script-text ceiling above: a
# legitimate /execute call can carry up to 10 dataframes and 50 variables
# (each individually bounded, see MAX_VARIABLE_STRING_CHARS /
# MAX_VARIABLE_VECTOR_ELEMENTS and build_request_dataframes()), so the whole
# JSON body can legitimately run into several MB. This bounds it anyway, well
# above any real call, so a hostile body cannot force unbounded memory use
# just to be parsed and rejected.
MAX_REQUEST_BODY_BYTES <- as.numeric(Sys.getenv("JUBE_R_MAX_REQUEST_BODY_BYTES", "52428800"))

# --- Background execution daemons -------------------------------------------
# Plumber is a single R process on a single OS thread: without this, eval()
# for one script blocks that process from answering ANY other request --
# including /health -- for as long as the script runs, up to
# MAX_TIMEOUT_SECONDS. mirai starts DAEMON_COUNT separate R processes
# ("daemons") that scripts actually run in; the main process hands a script
# to a daemon and comes straight back to Plumber's event loop via
# `promises`, so /health, /validate and other /execute calls keep being
# served while one is still running.
#
# DAEMON_COUNT defaults to 2, matching this image's own documented `--cpus 2`
# deployment flag (see README.md) -- one daemon per CPU the container's
# cgroup actually grants it, so the pool can't be sized past what the
# container is allowed to use anyway. Override with JUBE_R_DAEMON_COUNT for a
# deployment with a different CPU allocation.
DAEMON_COUNT <- max(1L, as.integer(Sys.getenv("JUBE_R_DAEMON_COUNT", "2")))

# Per-task hard backstop, in addition to (not instead of) the setTimeLimit()
# wall-clock/CPU timeout already enforced inside run_sandboxed() itself. That
# timeout is the normal, graceful path -- it returns a clean "timedOut: true"
# result. This backstop exists for the case it doesn't fire (e.g. a script
# stuck in uninterruptible native code that setTimeLimit() cannot interrupt):
# mirai's own `.timeout` on the task kills it from outside R entirely. Set a
# little *after* the script's own timeout (grace, below) so the graceful path
# always gets the first chance to resolve cleanly.
MIRAI_TIMEOUT_GRACE_MS <- as.numeric(Sys.getenv("JUBE_R_MIRAI_GRACE_MS", "5000"))

# Bounded concurrency for /execute specifically: with only DAEMON_COUNT
# daemons, an unbounded flood of concurrent calls would otherwise queue
# without limit inside mirai's own dispatcher, growing memory and making
# every caller's wait longer rather than failing fast. Reject outright once
# this many calls are already running or queued. Defaults to three times the
# daemon count -- enough slack to smooth a short burst without turning into
# an unbounded queue.
MAX_INFLIGHT_EXECUTE <- as.numeric(Sys.getenv("JUBE_R_MAX_INFLIGHT_EXECUTE", as.character(DAEMON_COUNT * 3L)))

mirai::daemons(DAEMON_COUNT)

# Daemons start as brand new R sessions with nothing sourced into them.
# `everywhere()` runs the same source() calls on every daemon (and persists
# them for the daemon's lifetime) so run_sandboxed() and the validation /
# forbidden-symbol / shape-check machinery it depends on are available
# there too -- the sandboxing rules apply inside the daemon exactly as they
# do in the main process. Absolute paths are resolved here, in the main
# process, so this does not depend on a daemon's own working directory.
mirai::everywhere(
  {
    source(sandbox_path)
    source(data_cache_path)
  },
  sandbox_path = normalizePath("R/sandbox.R"),
  data_cache_path = normalizePath("R/data_cache.R")
)

.execute_inflight <- new.env()
.execute_inflight$count <- 0L

increment_inflight <- function() {
  .execute_inflight$count <- .execute_inflight$count + 1L
  invisible(.execute_inflight$count)
}

decrement_inflight <- function() {
  .execute_inflight$count <- max(0L, .execute_inflight$count - 1L)
  invisible(.execute_inflight$count)
}

backstop_timeout_result <- function(timestamp, detail = NULL) {
  message <- paste(
    "The script was stopped by the background worker's hard backstop",
    "timeout before it returned a result.",
    if (!is.null(detail)) sprintf("(%s)", detail) else NULL
  )
  list(
    status = "error",
    consoleOutput = "",
    error = message,
    warnings = list(),
    durationMs = 0,
    timedOut = TRUE,
    rVersion = R.version.string,
    timestamp = timestamp
  )
}

reject_unknown_fields <- function(parsed, allowed) {
  extra <- setdiff(names(parsed), allowed)
  if (length(extra) > 0) {
    return(sprintf("Unexpected field(s) in request body: %s.", paste(extra, collapse = ", ")))
  }
  NULL
}

# Best-effort defence at the httpuv layer itself, in case the installed
# plumber version honours this option: reject an oversized body before it is
# even buffered. Deliberately set well above MAX_REQUEST_BODY_BYTES, not
# equal to it -- the installed plumber/httpuv version here does honour this
# option and enforces it by aborting the connection rather than returning a
# JSON body, so setting it to the same threshold as the checks below makes it
# race them for a normally Content-Length-labelled request and produces an
# inconsistent, non-JSON rejection. With the margin below, a request whose
# own declared Content-Length already exceeds MAX_REQUEST_BODY_BYTES is
# always caught cleanly by the @filter first; this option is then a true
# last-resort backstop for a body that lies about or omits Content-Length.
options(plumber.maxRequestSize = MAX_REQUEST_BODY_BYTES * 2)

#* @apiTitle Jube.R
#* @apiDescription Sandboxed R script execution. Every response is JSON;
#*   nothing here returns a raw R console transcript without the metadata
#*   wrapper (status, timing, timeout, error) described in README.md.

#* Plumber's default JSON serializer wraps every scalar value in a length-1
#* array (`"status": ["ok"]`, matching jsonlite::toJSON()'s own default),
#* which is exactly wrong for a metadata contract every caller here parses
#* by field — confirmed by a real client (httr) getting lists back instead
#* of plain strings/numbers/booleans. `serializer_unboxed_json()` fixes
#* that but still serialises a NULL field (e.g. `error` on a successful
#* run) as `{}` rather than `null` by jsonlite's own default — explicit
#* `null = "null"` here fixes that too, applied once for every route below.
#* @plumber
function(pr) {
  pr |> plumber::pr_set_serializer(plumber::serializer_unboxed_json(null = "null"))
}

#* Rejects a request whose declared Content-Length already exceeds
#* MAX_REQUEST_BODY_BYTES before anything touches the body itself --
#* jsonlite::fromJSON and the request handlers below are both O(body size),
#* so this is the cheapest point at which an oversized, hostile payload can
#* be turned away. Applies to every route, not only /execute, since /health
#* and /packages taking a body at all is itself already unexpected.
#* @filter requestSize
function(req, res) {
  contentLength <- suppressWarnings(as.numeric(req$HTTP_CONTENT_LENGTH))
  if (length(contentLength) == 1 && !is.na(contentLength) && contentLength > MAX_REQUEST_BODY_BYTES) {
    res$status <- 413
    return(list(status = "rejected",
                error = sprintf("Request body exceeds the maximum of %d bytes.", MAX_REQUEST_BODY_BYTES)))
  }
  forward()
}

#* Liveness, version information, and the actual, current script-safety
#* rules (not documentation that can drift — read straight from R/sandbox.R
#* — so a caller iterating against /validate can learn the boundaries
#* directly rather than only by tripping them).
#* @get /health
function() {
  list(
    status = "ok",
    rVersion = R.version.string,
    plumberVersion = as.character(utils::packageVersion("plumber")),
    upSinceUtc = format(STARTED_AT, "%Y-%m-%dT%H:%M:%OS3Z", tz = "UTC"),
    uptimeSeconds = round(as.numeric(Sys.time() - STARTED_AT, units = "secs"), 1),
    defaultTimeoutSeconds = DEFAULT_TIMEOUT_SECONDS,
    maxTimeoutSeconds = MAX_TIMEOUT_SECONDS,
    scriptRules = list(
      maxScriptChars = SCRIPT_MAX_CHARS,
      maxNestingDepth = SCRIPT_MAX_NESTING_DEPTH,
      forbiddenFunctions = as.list(sort(FORBIDDEN_SYMBOLS))
    )
  )
}

#* Every package installed in this image, so a caller can check what's safe
#* to `library()` before writing a script around it instead of discovering
#* a missing package at execution time.
#* @get /packages
function() {
  lib <- Sys.getenv("R_LIBRARY")
  search_paths <- if (nzchar(lib)) c(lib, .libPaths()) else .libPaths()
  installed <- as.data.frame(utils::installed.packages(lib.loc = search_paths))
  packages <- installed[order(installed$Package), c("Package", "Version")]
  list(
    count = nrow(packages),
    packages = lapply(seq_len(nrow(packages)), function(i) {
      list(name = packages$Package[i], version = packages$Version[i])
    }),
    note = paste(
      "Every package listed here is already installed and may be loaded with",
      "library(<name>). install.packages() is disabled (see /health and",
      "README.md) — there is no package manager in this image at runtime,",
      "and no network to fetch from even if there were."
    )
  )
}

# A script can be up to maxScriptChars long (see /health) and can contain
# quotes, newlines, and the characters application/x-www-form-urlencoded
# treats as syntax (&, =, +, %) — none of which a caller can be trusted to
# percent-encode correctly, and Plumber's own automatic argument binding
# does not reliably distinguish a JSON body from a form-urlencoded one once
# the function signature declares plain scalar parameters. Both routes
# below read and parse the raw JSON body themselves instead, so the only
# accepted shape is the one README.md documents:
# {"script": "...", "timeoutSeconds": 30} — and a script's own content can
# never be misread as part of the request's own framing.
parse_json_body <- function(req, bad_body_message) {
  raw <- req$postBody
  if (is.null(raw) || !nzchar(raw)) {
    return(list(parsed = NULL, error = bad_body_message, status = NULL))
  }
  # Defence in depth alongside the @filter requestSize above and the
  # plumber.maxRequestSize option: this fires regardless of whether the
  # caller sent (or lied about) a Content-Length header, e.g. a
  # chunked-encoded body -- req$postBody is only ever examined once it is
  # already fully buffered, so this still bounds what jsonlite is asked to
  # parse even when the earlier, cheaper checks could not apply.
  if (nchar(raw, type = "bytes") > MAX_REQUEST_BODY_BYTES) {
    return(list(parsed = NULL,
                error = sprintf("Request body exceeds the maximum of %d bytes.", MAX_REQUEST_BODY_BYTES),
                status = 413L))
  }
  parsed <- tryCatch(jsonlite::fromJSON(raw, simplifyVector = TRUE), error = function(e) NULL)
  if (is.null(parsed) || !is.list(parsed)) {
    return(list(parsed = NULL, error = bad_body_message, status = NULL))
  }
  list(parsed = parsed, error = NULL, status = NULL)
}

# jsonlite simplifies a uniform-shape JSON array of objects (our "dataframes"/"variables" arrays) into a
# data.frame whose columns are either atomic vectors or, when a column's values are not all the same
# type (our "variables" array's own "value" field, deliberately -- see the wire-format docs), a list
# column; it simplifies an empty array to list() rather than a data.frame at all. `[[i]]` works
# correctly against either an atomic vector or a list column, so this one helper reads both shapes the
# same way and always hands back a plain list of {name, value} pairs regardless of which one jsonlite
# chose.
rows_of_name_value <- function(x, value_field) {
  if (is.null(x)) {
    return(list())
  }
  if (is.data.frame(x)) {
    if (nrow(x) == 0) {
      return(list())
    }
    return(lapply(seq_len(nrow(x)), function(i) {
      list(name = x$name[[i]], value = x[[value_field]][[i]])
    }))
  }
  if (is.list(x) && length(x) > 0) {
    return(x)
  }
  list()
}

# Same normalisation as rows_of_name_value, for "dataframes" entries specifically -- each one now
# carries {name, columns, data} (already-resolved table data, see data_cache.R) rather than a bare
# {name, pointer}, so it needs its own two value fields instead of one.
rows_of_dataframe_requests <- function(x) {
  if (is.null(x)) {
    return(list())
  }
  if (is.data.frame(x)) {
    if (nrow(x) == 0) {
      return(list())
    }
    return(lapply(seq_len(nrow(x)), function(i) {
      list(name = x$name[[i]], columns = x$columns[[i]], data = x$data[[i]])
    }))
  }
  if (is.list(x) && length(x) > 0) {
    return(x)
  }
  list()
}

# Mirrors ScriptExecutionService's own per-variable size limits on the .NET side -- enforced here too,
# not only there, because this unauthenticated-by-design server can be reached directly (see
# Http/RServerExecute.http) and should not rely on a well-behaved caller to have applied its own cap
# first.
oversized_variable_name <- function(variable_requests) {
  for (variable in variable_requests) {
    value <- variable$value
    if (length(value) > MAX_VARIABLE_VECTOR_ELEMENTS) {
      return(variable$name)
    }
    if (is.character(value) && any(nchar(value, type = "bytes") > MAX_VARIABLE_STRING_CHARS, na.rm = TRUE)) {
      return(variable$name)
    }
  }
  NULL
}

#* Check a script for shape, syntax and disallowed function references
#* WITHOUT running it, plus non-blocking lint feedback. Use this first —
#* it's cheap, and catches the errors /execute would reject anyway before
#* spending any execution time.
#* @param script:string The R source to check, sent as a JSON body:
#*   {"script": "..."}.
#* @post /validate
function(req, res) {
  body <- parse_json_body(req, "The request body must be JSON: {\"script\": \"...\"}.")
  if (!is.null(body$error)) {
    if (!is.null(body$status)) res$status <- body$status
    return(list(ok = FALSE, errors = list(body$error)))
  }
  parsed <- body$parsed

  unknown <- reject_unknown_fields(parsed, c("script"))
  if (!is.null(unknown)) {
    res$status <- 400
    return(list(ok = FALSE, errors = list(unknown)))
  }

  if (!is.character(parsed$script) || length(parsed$script) != 1L || !nzchar(parsed$script)) {
    return(list(ok = FALSE, errors = list("script must be a non-empty string.")))
  }
  script <- parsed$script
  validation <- validate_script(script)
  list(
    ok = validation$ok,
    errors = as.list(validation$errors),
    lint = if (validation$ok) lint_script(script) else list()
  )
}

#* Run a script. Always validates first (see /validate) — a script that
#* fails validation is rejected without ever being evaluated, reported the
#* same way as any other execution outcome (status "rejected"). The actual
#* run happens on a background mirai daemon (see "Background execution
#* daemons" above), so this process stays able to answer /health, /validate
#* and other /execute calls while this one is still running.
#* @param script:string The R source to run, sent as a JSON body:
#*   {"script": "...", "timeoutSeconds": 30, "userName": "alice",
#*   "dataframes": [{"name": "transactions", "columns": [...], "data": [...]}],
#*   "variables": [{"name": "threshold", "value": 100}]}.
#* @param timeoutSeconds:number Optional, in the same JSON body. CPU and
#*   wall-clock limit for this call, capped at maxTimeoutSeconds from
#*   /health. Defaults to defaultTimeoutSeconds from /health
#*   (JUBE_R_SCRIPT_TIMEOUT_SECONDS).
#* @post /execute
function(req, res) {
  timestamp <- format(Sys.time(), "%Y-%m-%dT%H:%M:%OS3Z", tz = "UTC")

  body <- parse_json_body(req, "The request body must be JSON: {\"script\": \"...\"}.")
  if (!is.null(body$error)) {
    res$status <- if (!is.null(body$status)) body$status else 400
    return(list(status = "rejected", error = body$error))
  }
  parsed <- body$parsed

  unknown <- reject_unknown_fields(parsed,
    c("script", "timeoutSeconds", "userName", "dataframes", "variables"))
  if (!is.null(unknown)) {
    res$status <- 400
    return(list(status = "rejected", error = unknown))
  }

  if (!is.character(parsed$script) || length(parsed$script) != 1L || !nzchar(parsed$script)) {
    res$status <- 400
    return(list(status = "rejected", error = "script must be a non-empty string."))
  }
  script <- parsed$script

  if (is.character(parsed$userName) && length(parsed$userName) == 1L && nzchar(parsed$userName)) {
    message(sprintf("execute: userName=%s", parsed$userName))
  }

  timeoutSeconds <- parsed$timeoutSeconds
  timeout <- if (is.null(timeoutSeconds)) {
    DEFAULT_TIMEOUT_SECONDS
  } else {
    requested <- suppressWarnings(as.numeric(timeoutSeconds))
    if (is.na(requested) || requested <= 0) {
      res$status <- 400
      return(list(status = "rejected", error = "timeoutSeconds must be a positive number."))
    }
    min(requested, MAX_TIMEOUT_SECONDS)
  }

  dataframe_requests <- rows_of_dataframe_requests(parsed$dataframes)
  variable_requests <- rows_of_name_value(parsed$variables, "value")

  if (length(dataframe_requests) > MAX_DATAFRAMES_PER_CALL) {
    res$status <- 400
    return(list(status = "rejected",
                error = sprintf("At most %d dataframes may be hydrated in one call.",
                                 MAX_DATAFRAMES_PER_CALL)))
  }

  if (length(variable_requests) > MAX_VARIABLES_PER_CALL) {
    res$status <- 400
    return(list(status = "rejected",
                error = sprintf("At most %d variables may be bound in one call.",
                                 MAX_VARIABLES_PER_CALL)))
  }

  oversized <- oversized_variable_name(variable_requests)
  if (!is.null(oversized)) {
    res$status <- 400
    return(list(status = "rejected",
                error = sprintf("The variable '%s' is too large.", oversized)))
  }

  built <- build_request_dataframes(dataframe_requests)
  if (!is.null(built$error)) {
    res$status <- 400
    return(list(status = "rejected", error = built$error))
  }

  # Validated synchronously, before this request is allowed to occupy a
  # daemon/queue slot at all: a script this rejects costs no more than
  # /validate already costs today, and never eats into the bounded
  # concurrency budget below -- otherwise a flood of hostile scripts could
  # exhaust the daemon queue just by being rejected slowly, one at a time.
  # run_sandboxed() (inside the daemon, below) repeats this exact check on
  # its own regardless, so a direct call to this server that somehow
  # bypassed this point is still covered.
  preValidation <- validate_script(script)
  if (!preValidation$ok) {
    res$status <- 422
    return(rejected_result(paste(preValidation$errors, collapse = " "), timestamp))
  }

  if (.execute_inflight$count >= MAX_INFLIGHT_EXECUTE) {
    res$status <- 503
    res$setHeader("Retry-After", "1")
    return(list(status = "rejected",
                error = sprintf(
                  "Too many scripts are already running or queued (limit %d). Retry shortly.",
                  MAX_INFLIGHT_EXECUTE)))
  }
  increment_inflight()

  task <- mirai::mirai(
    run_sandboxed(scriptText, timeoutSecondsValue, dataframes = dataframeValues, variables = variableValues),
    scriptText = script,
    timeoutSecondsValue = timeout,
    dataframeValues = built$frames,
    variableValues = variable_requests,
    .timeout = round(timeout * 1000) + MIRAI_TIMEOUT_GRACE_MS
  )

  promises::then(
    task,
    onFulfilled = function(outcome) {
      if (mirai::is_error_value(outcome)) {
        res$status <- 422
        return(backstop_timeout_result(timestamp))
      }
      if (outcome$status != "ok") res$status <- 422
      outcome
    },
    onRejected = function(err) {
      res$status <- 422
      backstop_timeout_result(timestamp, conditionMessage(err))
    }
  ) |> promises::finally(decrement_inflight)
}
