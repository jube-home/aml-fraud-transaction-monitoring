

library(httr)
library(processx)

port <- 18080 + sample.int(1000, 1)
base_url <- sprintf("http://127.0.0.1:%d", port)

server <- NULL

start_server <- function() {
  jube_r_root <- normalizePath(file.path("..", ".."))
  server <<- processx::process$new(
    "/usr/local/bin/Rscript", "entrypoint.R",
    wd = jube_r_root,
    env = c(Sys.getenv(), JUBE_R_PORT = as.character(port), JUBE_R_HOST = "127.0.0.1"),
    stdout = "|", stderr = "|"
  )

  deadline <- Sys.time() + 30
  repeat {
    ok <- tryCatch({
      resp <- httr::GET(paste0(base_url, "/health"), httr::timeout(1))
      httr::status_code(resp) == 200
    }, error = function(e) FALSE)
    if (ok) return(invisible(TRUE))
    if (!server$is_alive()) {
      stop("Jube.R exited during startup:\n", paste(server$read_all_error_lines(), collapse = "\n"))
    }
    if (Sys.time() > deadline) stop("Jube.R did not become healthy within 30s")
    Sys.sleep(0.2)
  }
}

stop_server <- function() {
  if (!is.null(server) && server$is_alive()) server$kill()
}

start_server()

test_that("GET /health reports the real, current script rules", {
  resp <- httr::GET(paste0(base_url, "/health"))
  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "ok")
  expect_true("system" %in% unlist(body$scriptRules$forbiddenFunctions))
  expect_true(body$scriptRules$maxNestingDepth > 0)
})

test_that("GET /packages lists installed packages including ones this guide depends on", {
  resp <- httr::GET(paste0(base_url, "/packages"))
  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  names_listed <- vapply(body$packages, function(p) p$name, character(1))
  expect_true("bnlearn" %in% names_listed)
  expect_true("randomForest" %in% names_listed)
})

test_that("POST /validate accepts a benign script and flags a hostile one, without running either", {
  ok_resp <- httr::POST(paste0(base_url, "/validate"), body = list(script = "1 + 1"), encode = "json")
  expect_equal(httr::status_code(ok_resp), 200)
  expect_true(httr::content(ok_resp, as = "parsed")$ok)

  bad_resp <- httr::POST(paste0(base_url, "/validate"), body = list(script = 'system("id")'), encode = "json")
  expect_equal(httr::status_code(bad_resp), 200)
  bad_body <- httr::content(bad_resp, as = "parsed")
  expect_false(bad_body$ok)
  expect_true(length(bad_body$errors) > 0)
})

test_that("POST /execute runs a benign script and returns its console output", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = 'cat("hello from the http layer\\n")'), encode = "json")
  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "ok")
  expect_match(body$consoleOutput, "hello from the http layer")
})

test_that("POST /execute rejects a hostile script over HTTP with a non-200 status", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = 'system("id")'), encode = "json")
  expect_equal(httr::status_code(resp), 422)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
})

test_that("POST /execute honours a client-supplied timeout, capped by the server maximum", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "repeat {}", timeoutSeconds = 1), encode = "json")
  expect_equal(httr::status_code(resp), 422)
  body <- httr::content(resp, as = "parsed")
  expect_true(body$timedOut)
})

test_that("POST /execute runs a script whose own text is full of form-urlencoded's syntax characters", {
  script <- 'cat(paste("a&b=c+d", "100% done", "line one\nline two", sep = " | "))'
  resp <- httr::POST(paste0(base_url, "/execute"), body = list(script = script), encode = "json")
  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "ok")
  expect_match(body$consoleOutput, "a&b=c+d | 100% done | line one\nline two", fixed = TRUE)
})

test_that("POST /execute rejects a form-urlencoded body instead of silently misreading it", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "print(1)"), encode = "form")
  expect_equal(httr::status_code(resp), 400)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
})

test_that("POST /validate rejects a form-urlencoded body instead of silently misreading it", {
  resp <- httr::POST(paste0(base_url, "/validate"),
                      body = list(script = "1 + 1"), encode = "form")
  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_false(body$ok)
})

test_that("a realistic RunScript-shaped call runs against an already-resolved dataframe and computes something no pure-computation tool already covers", {
  script <- paste(
    "rules$Share <- rules$Hits / sum(rules$Hits)",
    "rules$BlockedRate <- rules$Blocked / rules$Hits",
    "ordered <- rules[order(-rules$Share), ]",
    "cat(sprintf('top_rule=%s share=%.4f blocked_rate=%.4f\\n', ordered$RuleName[1], ordered$Share[1], ordered$BlockedRate[1]))",
    "cat(sprintf('herfindahl=%.4f\\n', sum(rules$Share^2)))",
    sep = "\n"
  )

  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = script, userName = "alice",
                                  dataframes = list(list(
                                    name = "rules",
                                    columns = list(list(Name = "RuleName", Type = "string"),
                                                   list(Name = "Hits", Type = "double"),
                                                   list(Name = "Blocked", Type = "double")),
                                    data = list(
                                      list(RuleName = "velocity-check", Hits = 420, Blocked = 61),
                                      list(RuleName = "geo-mismatch", Hits = 95, Blocked = 40),
                                      list(RuleName = "device-reuse", Hits = 310, Blocked = 12),
                                      list(RuleName = "amount-outlier", Hits = 18, Blocked = 9)
                                    )
                                  ))),
                      encode = "json")

  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "ok")
  expect_match(body$consoleOutput, "top_rule=velocity-check")
  expect_match(body$consoleOutput, "herfindahl=0\\.")
})

test_that("POST /execute hydrates every base JSON value shape into the right R type", {
  script <- paste(
    "cat(sprintf('number:%s(%s) string:%s(%s) bool:%s(%s) array:%s(%s,len=%d)\\n',",
    "  class(numberVar), numberVar, class(stringVar), stringVar, class(boolVar), boolVar,",
    "  class(arrayVar), paste(arrayVar, collapse = ','), length(arrayVar)))",
    sep = "\n"
  )

  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = script,
                                  variables = list(
                                    list(name = "numberVar", value = 42.5),
                                    list(name = "stringVar", value = "high_risk"),
                                    list(name = "boolVar", value = TRUE),
                                    list(name = "arrayVar", value = list(1, 2, 3))
                                  )),
                      encode = "json")

  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "ok")
  expect_match(body$consoleOutput, "number:numeric(42.5)", fixed = TRUE)
  expect_match(body$consoleOutput, "string:character(high_risk)", fixed = TRUE)
  expect_match(body$consoleOutput, "bool:logical(TRUE)", fixed = TRUE)

  expect_match(body$consoleOutput, "array:(numeric|integer)\\(1,2,3,len=3\\)")
})

test_that("a dataframe entry with no usable columns fails the call with one generic message over real HTTP", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "1 + 1",
                                  dataframes = list(list(name = "x", columns = list(), data = list()))),
                      encode = "json")
  expect_equal(httr::status_code(resp), 400)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
  expect_equal(body$error, "could not build dataframe: x")
})

test_that("one malformed dataframe among several fails the whole call over real HTTP, rather than running with fewer than asked for", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "1 + 1",
                                  dataframes = list(
                                    list(name = "good", columns = list(list(Name = "X", Type = "double")),
                                         data = list(list(X = 1))),
                                    list(name = "bad", columns = list(), data = list())
                                  )),
                      encode = "json")
  expect_equal(httr::status_code(resp), 400)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
  expect_equal(body$error, "could not build dataframe: bad")
})

test_that("POST /execute refuses more than 10 dataframes or 50 variables in one call", {
  too_many_dataframes <- lapply(1:11, function(i) {
    list(name = sprintf("d%d", i), columns = list(), data = list())
  })
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "1", dataframes = too_many_dataframes), encode = "json")
  expect_equal(httr::status_code(resp), 400)
  expect_match(httr::content(resp, as = "parsed")$error, "At most 10 dataframes")

  too_many_variables <- lapply(1:51, function(i) list(name = sprintf("v%d", i), value = i))
  resp2 <- httr::POST(paste0(base_url, "/execute"),
                       body = list(script = "1", variables = too_many_variables), encode = "json")
  expect_equal(httr::status_code(resp2), 400)
  expect_match(httr::content(resp2, as = "parsed")$error, "At most 50 variables")
})

test_that("POST /execute refuses a single variable value that is itself too large", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "1",
                                  variables = list(list(name = "huge", value = strrep("x", 200001)))),
                      encode = "json")
  expect_equal(httr::status_code(resp), 400)
  expect_match(httr::content(resp, as = "parsed")$error, "huge")

  resp2 <- httr::POST(paste0(base_url, "/execute"),
                       body = list(script = "1",
                                   variables = list(list(name = "longVector", value = as.list(seq_len(100001))))),
                       encode = "json")
  expect_equal(httr::status_code(resp2), 400)
  expect_match(httr::content(resp2, as = "parsed")$error, "longVector")
})

test_that("a genuinely large variable within the limits still binds and runs correctly", {
  big_string <- strrep("y", 150000)
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "cat(nchar(bigString), '\\n')",
                                  variables = list(list(name = "bigString", value = big_string))),
                      encode = "json")
  expect_equal(httr::status_code(resp), 200)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "ok")
  expect_match(body$consoleOutput, "150000")
})

test_that("POST /execute rejects a body over the request-size ceiling before running anything", {
  oversized_script <- paste0('cat("', strrep("x", 51 * 1024 * 1024), '")')
  resp <- httr::POST(paste0(base_url, "/execute"), body = list(script = oversized_script), encode = "json")
  expect_equal(httr::status_code(resp), 413)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
  expect_match(body$error, "exceeds the maximum")
})

test_that("POST /validate rejects a body over the request-size ceiling before running anything", {
  oversized_script <- paste0('cat("', strrep("x", 51 * 1024 * 1024), '")')
  resp <- httr::POST(paste0(base_url, "/validate"), body = list(script = oversized_script), encode = "json")
  expect_equal(httr::status_code(resp), 413)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
  expect_match(body$error, "exceeds the maximum")
})

test_that("POST /execute rejects an unexpected extra field in the request body", {
  resp <- httr::POST(paste0(base_url, "/execute"),
                      body = list(script = "1 + 1", notARealField = "whatever"), encode = "json")
  expect_equal(httr::status_code(resp), 400)
  body <- httr::content(resp, as = "parsed")
  expect_equal(body$status, "rejected")
  expect_match(body$error, "notARealField")
})

test_that("POST /validate rejects an unexpected extra field in the request body", {
  resp <- httr::POST(paste0(base_url, "/validate"),
                      body = list(script = "1 + 1", notARealField = "whatever"), encode = "json")
  expect_equal(httr::status_code(resp), 400)
  body <- httr::content(resp, as = "parsed")
  expect_false(body$ok)
  expect_match(body$errors[[1]], "notARealField")
})

test_that("the main process keeps answering /health while a slow script is still running on a daemon", {
  # A second, independent R process makes the slow call so this test's own
  # httr::POST (synchronous) does not block waiting for it -- that lets the
  # /health check below genuinely race a script that is still executing,
  # rather than running strictly after it.
  slow_call_expr <- sprintf(
    'httr::POST("%s", body = list(script = "Sys.sleep(4)", timeoutSeconds = 10), encode = "json")',
    paste0(base_url, "/execute")
  )
  slow_call <- processx::process$new(
    "/usr/local/bin/Rscript", c("-e", slow_call_expr),
    stdout = "|", stderr = "|"
  )
  on.exit(if (slow_call$is_alive()) slow_call$kill(), add = TRUE)

  Sys.sleep(0.5)
  expect_true(slow_call$is_alive())

  health_start <- Sys.time()
  resp <- httr::GET(paste0(base_url, "/health"), httr::timeout(2))
  health_elapsed <- as.numeric(Sys.time() - health_start, units = "secs")

  expect_equal(httr::status_code(resp), 200)
  expect_true(health_elapsed < 2)

  slow_call$wait(timeout = 8000)
  expect_false(slow_call$is_alive())
})

stop_server()
