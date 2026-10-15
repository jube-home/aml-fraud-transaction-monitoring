

FORBIDDEN_SYMBOLS <- c(

  "system", "system2", "shell", "shell.exec", "pipe",

  "get", "get0", "mget", "do.call", "Recall", "eval", "evalq", "parse",
  "sys.call", "sys.function", "match.call", "match.fun", "as.function",

  "environment", "body", "formals",

  ".Internal", ".Primitive", ".C", ".Call", ".Fortran", ".External",
  "dyn.load", "dyn.unload", "library.dynam", "getNativeSymbolInfo",

  "unlockBinding", "lockBinding", "lockEnvironment", "assign", "delayedAssign",
  "globalenv", "baseenv", "topenv", "as.environment", "sys.frame", "parent.frame",

  "Sys.setenv", "Sys.unsetenv", "Sys.setlocale", "Sys.getenv",
  "file.remove", "file.rename", "unlink", "file.create", "file.append",
  "file.symlink", "file.link", "Sys.chmod", "Sys.umask", "setwd",

  "download.file", "url", "socketConnection", "make.socket", "curlGetHeaders",

  "install.packages", "remove.packages",
  "quit", "q", "sink", "trace", "untrace", "traceback", "source",

  "<<-"
)

SCRIPT_MAX_CHARS <- 200000L
SCRIPT_MAX_NESTING_DEPTH <- 64L

rejected_result <- function(error_message,
                             timestamp = format(Sys.time(), "%Y-%m-%dT%H:%M:%OS3Z", tz = "UTC")) {
  list(
    status = "rejected",
    consoleOutput = "",
    error = error_message,
    warnings = list(),
    durationMs = 0,
    timedOut = FALSE,
    rVersion = R.version.string,
    timestamp = timestamp
  )
}

check_script_shape <- function(script_text,
                                max_chars = SCRIPT_MAX_CHARS,
                                max_nesting_depth = SCRIPT_MAX_NESTING_DEPTH) {
  if (nchar(script_text, type = "bytes") > max_chars) {
    return(sprintf("Script exceeds the maximum length of %d characters.", max_chars))
  }

  depth <- 0L
  max_depth <- 0L
  in_string <- NA_character_
  in_comment <- FALSE
  escaped <- FALSE
  chars <- strsplit(script_text, "", fixed = TRUE)[[1]]

  for (ch in chars) {
    if (in_comment) {
      if (ch == "\n") in_comment <- FALSE
      next
    }
    if (!is.na(in_string)) {
      if (escaped) {
        escaped <- FALSE
      } else if (ch == "\\") {
        escaped <- TRUE
      } else if (ch == in_string) {
        in_string <- NA_character_
      }
      next
    }
    if (ch == "#") {
      in_comment <- TRUE
      next
    }
    if (ch == "\"" || ch == "'") {
      in_string <- ch
      next
    }
    if (ch %in% c("(", "[", "{")) {
      depth <- depth + 1L
      if (depth > max_depth) max_depth <- depth
    } else if (ch %in% c(")", "]", "}")) {
      depth <- max(0L, depth - 1L)
    }
  }

  if (max_depth > max_nesting_depth) {
    return(sprintf(
      "Script nests %d levels deep, exceeding the maximum of %d.",
      max_depth, max_nesting_depth
    ))
  }

  NULL
}

find_forbidden_symbols <- function(expr) {
  found <- character(0)

  walk <- function(e) {
    if (is.symbol(e)) {
      name <- as.character(e)
      if (name %in% FORBIDDEN_SYMBOLS) {
        found[[length(found) + 1L]] <<- name
      }
      return(invisible(NULL))
    }
    if (is.call(e)) {
      head <- e[[1]]
      if (is.call(head) && length(head) == 3L &&
          (identical(head[[1]], as.symbol("::")) || identical(head[[1]], as.symbol(":::")))) {
        qualified_name <- as.character(head[[3]])
        if (qualified_name %in% FORBIDDEN_SYMBOLS) {
          found[[length(found) + 1L]] <<- qualified_name
        }
      }
      for (part in as.list(e)) {
        if (!missing(part)) walk(part)
      }
      return(invisible(NULL))
    }
    if (is.pairlist(e) || is.list(e)) {
      for (part in as.list(e)) {
        if (!missing(part)) walk(part)
      }
    }
    invisible(NULL)
  }

  for (top in as.list(expr)) walk(top)
  unique(found)
}

validate_script <- function(script_text) {
  shape_error <- check_script_shape(script_text)
  if (!is.null(shape_error)) {
    return(list(ok = FALSE, errors = shape_error))
  }

  parsed <- tryCatch(parse(text = script_text), error = function(e) e)
  if (inherits(parsed, "error")) {
    return(list(ok = FALSE, errors = paste("Parse error:", conditionMessage(parsed))))
  }

  forbidden <- find_forbidden_symbols(parsed)
  if (length(forbidden) > 0) {
    return(list(
      ok = FALSE,
      errors = sprintf(
        "Script references disallowed function(s): %s.",
        paste(forbidden, collapse = ", ")
      )
    ))
  }

  list(ok = TRUE, errors = character(0))
}

lint_script <- function(script_text) {
  tmp <- tempfile(fileext = ".R")
  on.exit(unlink(tmp), add = TRUE)
  writeLines(script_text, tmp)
  results <- tryCatch(
    as.data.frame(lintr::lint(tmp)),
    error = function(e) data.frame()
  )
  if (nrow(results) == 0) return(list())
  lapply(seq_len(nrow(results)), function(i) {
    list(
      line = results$line_number[i],
      column = results$column_number[i],
      type = as.character(results$type[i]),
      message = as.character(results$message[i])
    )
  })
}

is_valid_r_identifier <- function(name) {
  is.character(name) && length(name) == 1L &&
    grepl("^(?:[a-zA-Z]|\\.(?![0-9]))[a-zA-Z0-9._]*$", name, perl = TRUE)
}

run_sandboxed <- function(script_text, timeout_seconds, dataframes = list(), variables = list()) {
  timestamp <- format(Sys.time(), "%Y-%m-%dT%H:%M:%OS3Z", tz = "UTC")
  validation <- validate_script(script_text)
  if (!validation$ok) {
    return(rejected_result(paste(validation$errors, collapse = " "), timestamp))
  }

  for (dataframe_name in names(dataframes)) {
    if (!is_valid_r_identifier(dataframe_name)) {
      return(rejected_result(
        sprintf("'%s' is not a valid R identifier for a dataframe name.", dataframe_name), timestamp))
    }
  }

  for (variable in variables) {
    if (!is_valid_r_identifier(variable$name)) {
      return(rejected_result(
        sprintf("'%s' is not a valid R identifier for a variable name.", variable$name), timestamp))
    }
  }

  sandbox_env <- new.env(parent = globalenv())

  for (dataframe_name in names(dataframes)) {
    assign(dataframe_name, dataframes[[dataframe_name]], envir = sandbox_env)
  }

  for (variable in variables) {
    assign(variable$name, variable$value, envir = sandbox_env)
  }

  warnings_seen <- character(0)
  start <- Sys.time()
  timed_out <- FALSE

  setTimeLimit(cpu = timeout_seconds, elapsed = timeout_seconds, transient = TRUE)
  on.exit(setTimeLimit(cpu = Inf, elapsed = Inf, transient = FALSE), add = TRUE)

  console_output <- character(0)
  error_message <- NULL

  result <- withCallingHandlers(
    tryCatch(
      {
        console_output <- utils::capture.output(
          eval(parse(text = script_text), envir = sandbox_env)
        )
        NULL
      },
      error = function(e) {
        msg <- conditionMessage(e)
        if (grepl("reached elapsed time limit|reached CPU time limit", msg)) {
          timed_out <<- TRUE
        }
        error_message <<- msg
        NULL
      }
    ),
    warning = function(w) {
      warnings_seen[[length(warnings_seen) + 1L]] <<- conditionMessage(w)
      invokeRestart("muffleWarning")
    }
  )

  duration_ms <- as.numeric(Sys.time() - start, units = "secs") * 1000

  list(
    status = if (is.null(error_message)) "ok" else "error",
    consoleOutput = paste(console_output, collapse = "\n"),
    error = error_message,
    warnings = as.list(warnings_seen),
    durationMs = round(duration_ms, 1),
    timedOut = timed_out,
    rVersion = R.version.string,
    timestamp = timestamp
  )
}
