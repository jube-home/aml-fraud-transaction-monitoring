source(file.path("..", "..", "R", "sandbox.R"))

test_that("a successful script returns status ok with its console output", {
  result <- run_sandboxed('cat("hello\\n"); print(1 + 1)', timeout_seconds = 5)
  expect_equal(result$status, "ok")
  expect_match(result$consoleOutput, "hello")
  expect_match(result$consoleOutput, "2")
  expect_null(result$error)
  expect_false(result$timedOut)
})

test_that("a script that errors returns status error with the message, not a crash", {
  result <- run_sandboxed('stop("deliberate failure")', timeout_seconds = 5)
  expect_equal(result$status, "error")
  expect_match(result$error, "deliberate failure")
})

test_that("warnings are captured separately from console output and do not fail the run", {
  result <- run_sandboxed('warning("just a warning"); cat("still ran\\n")', timeout_seconds = 5)
  expect_equal(result$status, "ok")
  expect_match(result$consoleOutput, "still ran")
  expect_true(length(result$warnings) >= 1)
  expect_match(result$warnings[[1]], "just a warning")
})

test_that("a script that fails validation never runs and is reported as rejected", {
  result <- run_sandboxed('system("id")', timeout_seconds = 5)
  expect_equal(result$status, "rejected")
  expect_match(result$error, "system")
  expect_equal(result$durationMs, 0)
})

test_that("an infinite loop is stopped by the timeout, not left to hang", {
  start <- Sys.time()
  result <- run_sandboxed("repeat {}", timeout_seconds = 1)
  elapsed <- as.numeric(Sys.time() - start, units = "secs")

  expect_equal(result$status, "error")
  expect_true(result$timedOut)

  expect_true(elapsed < 10)
})

test_that("every response carries the metadata fields the API contract promises", {
  result <- run_sandboxed('1 + 1', timeout_seconds = 5)
  expect_true(all(c("status", "consoleOutput", "error", "warnings", "durationMs",
                     "timedOut", "rVersion", "timestamp") %in% names(result)))
  expect_match(result$rVersion, "^R version")
})

test_that("two calls do not share state — nothing persists between runs", {
  run_sandboxed("leaked_variable <- 42", timeout_seconds = 5)
  result <- run_sandboxed("exists('leaked_variable', inherits = FALSE)", timeout_seconds = 5)

  expect_equal(result$status, "ok")
  expect_match(result$consoleOutput, "FALSE")
})

test_that("a syntax error is reported as a validation rejection, not an execution error", {
  result <- run_sandboxed("x <- (1 +", timeout_seconds = 5)
  expect_equal(result$status, "rejected")
  expect_match(result$error, "Parse error")
})
