

source(file.path("..", "..", "R", "sandbox.R"))

test_that("script under the length limit passes the shape check", {
  script <- paste(rep("x", SCRIPT_MAX_CHARS - 1), collapse = "")
  expect_null(check_script_shape(script))
})

test_that("script over the length limit is rejected", {
  script <- paste(rep("x", SCRIPT_MAX_CHARS + 1), collapse = "")
  reason <- check_script_shape(script)
  expect_true(is.character(reason))
  expect_match(reason, "length")
})

test_that("nesting depth boundary is exact", {
  at_limit <- paste0(strrep("(", SCRIPT_MAX_NESTING_DEPTH), "1", strrep(")", SCRIPT_MAX_NESTING_DEPTH))
  over_limit <- paste0(strrep("(", SCRIPT_MAX_NESTING_DEPTH + 1), "1", strrep(")", SCRIPT_MAX_NESTING_DEPTH + 1))
  expect_null(check_script_shape(at_limit))
  expect_match(check_script_shape(over_limit), "nests")
})

test_that("nesting depth is counted across (), [] and {} together", {
  mixed <- paste0(
    strrep("(", 30), strrep("{", 30), strrep("[", 30), "1",
    strrep("]", 30), strrep("}", 30), strrep(")", 30)
  )
  expect_match(check_script_shape(mixed), "nests")
})

test_that("brackets inside a string literal do not count toward depth", {
  script <- paste0('x <- "', strrep("(", 100), '"')
  expect_null(check_script_shape(script))
})

test_that("brackets inside a comment do not count toward depth", {
  script <- paste0("x <- 1 # ", strrep("(", 100))
  expect_null(check_script_shape(script))
})

test_that("an escaped quote inside a string does not end it early", {
  script <- paste0('x <- "a\\"', strrep("(", 100), '"')
  expect_null(check_script_shape(script))
})

test_that("a syntactically invalid script is rejected by parse(), not silently accepted", {
  result <- validate_script("x <- (1 +")
  expect_false(result$ok)
  expect_match(result$errors, "Parse error")
})
