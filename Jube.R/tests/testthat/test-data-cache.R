source(file.path("..", "..", "R", "sandbox.R"))
source(file.path("..", "..", "R", "data_cache.R"))

test_that("simplify_column keeps a numeric column numeric rather than coercing it to character", {
  values <- list(1, 2.5, 3)
  result <- simplify_column(values)
  expect_true(is.numeric(result))
  expect_equal(result, c(1, 2.5, 3))
})

test_that("simplify_column keeps a logical column logical", {
  result <- simplify_column(list(TRUE, FALSE, TRUE))
  expect_true(is.logical(result))
  expect_equal(result, c(TRUE, FALSE, TRUE))
})

test_that("simplify_column keeps a character column character", {
  result <- simplify_column(list("a", "b", "c"))
  expect_true(is.character(result))
  expect_equal(result, c("a", "b", "c"))
})

test_that("simplify_column places a type-correct NA where a row's value was JSON null", {
  numeric_result <- simplify_column(list(1, NULL, 3))
  expect_true(is.numeric(numeric_result))
  expect_equal(numeric_result, c(1, NA_real_, 3))

  character_result <- simplify_column(list("a", NULL, "c"))
  expect_true(is.character(character_result))
  expect_equal(character_result, c("a", NA_character_, "c"))
})

test_that("simplify_column handles an entirely-null column without erroring", {
  result <- simplify_column(list(NULL, NULL))
  expect_equal(length(result), 2)
  expect_true(all(is.na(result)))
})

test_that("simplify_column treats a value that is not uniformly numeric or logical as character", {
  result <- simplify_column(list(1, "two", 3))
  expect_true(is.character(result))
  expect_equal(result, c("1", "two", "3"))
})

test_that("rows_of_object_array normalises a plain list of named-list rows unchanged", {
  rows <- list(list(Name = "Id", Type = "long"), list(Name = "Amount", Type = "double"))
  result <- rows_of_object_array(rows)
  expect_equal(length(result), 2)
  expect_equal(result[[1]]$Name, "Id")
  expect_equal(result[[2]]$Name, "Amount")
})

test_that("rows_of_object_array normalises a jsonlite-simplified data.frame back into rows by column name", {
  frame <- data.frame(Name = c("Id", "Amount"), Type = c("long", "double"), stringsAsFactors = FALSE)
  result <- rows_of_object_array(frame)
  expect_equal(length(result), 2)
  expect_equal(result[[1]]$Name, "Id")
  expect_equal(result[[1]]$Type, "long")
  expect_equal(result[[2]]$Name, "Amount")
})

test_that("rows_of_object_array returns an empty list for NULL or an empty array", {
  expect_equal(rows_of_object_array(NULL), list())
  expect_equal(rows_of_object_array(list()), list())
  expect_equal(rows_of_object_array(data.frame()), list())
})

test_that("build_dataframe_from_json builds a realistic multi-row, multi-column dataframe with every column kept in its real type", {
  columns <- list(
    list(Name = "Id", Type = "long"), list(Name = "Amount", Type = "double"),
    list(Name = "Status", Type = "string"), list(Name = "IsFlagged", Type = "bool")
  )
  data <- lapply(1:5, function(i) {
    list(Id = i, Amount = i * 1.5, Status = if (i %% 2 == 0) "flagged" else "ok", IsFlagged = i %% 2 == 0)
  })

  frame <- build_dataframe_from_json(columns, data)

  expect_false(is.null(frame))
  expect_equal(nrow(frame), 5)
  expect_equal(colnames(frame), c("Id", "Amount", "Status", "IsFlagged"))
  expect_true(is.numeric(frame$Id))
  expect_true(is.numeric(frame$Amount))
  expect_true(is.character(frame$Status))
  expect_true(is.logical(frame$IsFlagged))
  expect_equal(frame$Status, c("ok", "flagged", "ok", "flagged", "ok"))
})

test_that("build_dataframe_from_json places a type-correct NA where a row omits a column's value", {
  columns <- list(list(Name = "Label", Type = "string"), list(Name = "Value", Type = "double"))
  data <- list(list(Label = "a", Value = 1), list(Label = NULL, Value = NULL), list(Label = "c", Value = 3))

  frame <- build_dataframe_from_json(columns, data)

  expect_equal(frame$Label, c("a", NA_character_, "c"))
  expect_equal(frame$Value, c(1, NA_real_, 3))
})

test_that("build_dataframe_from_json picks columns by name rather than by the row's own key order", {
  columns <- list(list(Name = "A", Type = "double"), list(Name = "B", Type = "string"))
  data <- list(list(B = "first", A = 1), list(A = 2, B = "second"))

  frame <- build_dataframe_from_json(columns, data)

  expect_equal(frame$A, c(1, 2))
  expect_equal(frame$B, c("first", "second"))
})

test_that("build_dataframe_from_json returns NULL for an empty columns array", {
  expect_null(build_dataframe_from_json(list(), list()))
})

test_that("dataframe_build_error never varies its wording by name content", {
  expect_equal(dataframe_build_error("transactions"), "could not build dataframe: transactions")
  expect_equal(dataframe_build_error("x"), "could not build dataframe: x")
})

test_that("is_valid_r_identifier accepts ordinary names and rejects hostile ones", {
  expect_true(is_valid_r_identifier("transactions"))
  expect_true(is_valid_r_identifier("my_var1"))
  expect_true(is_valid_r_identifier(".hidden"))
  expect_false(is_valid_r_identifier("1leading"))
  expect_false(is_valid_r_identifier("has space"))
  expect_false(is_valid_r_identifier("has;semicolon"))
  expect_false(is_valid_r_identifier(".1notvalid"))
})

test_that("build_request_dataframes with an empty request builds nothing and raises no error", {
  result <- build_request_dataframes(list())
  expect_equal(result$frames, list())
  expect_null(result$error)
})

test_that("build_request_dataframes builds two independent valid entries correctly in one call", {
  result <- build_request_dataframes(list(
    list(name = "a", columns = list(list(Name = "Value", Type = "double")),
         data = list(list(Value = 1), list(Value = 2))),
    list(name = "b", columns = list(list(Name = "Label", Type = "string")),
         data = list(list(Label = "z")))
  ))

  expect_null(result$error)
  expect_equal(names(result$frames), c("a", "b"))
  expect_equal(nrow(result$frames$a), 2)
  expect_equal(result$frames$a$Value, c(1, 2))
  expect_equal(nrow(result$frames$b), 1)
  expect_equal(result$frames$b$Label, "z")
})

test_that("one malformed entry among several fails the whole call rather than silently building the rest", {
  result <- build_request_dataframes(list(
    list(name = "good", columns = list(list(Name = "Value", Type = "double")), data = list(list(Value = 1))),
    list(name = "bad", columns = list(), data = list())
  ))

  expect_null(result$frames)
  expect_equal(result$error, dataframe_build_error("bad"))
})

test_that("the first malformed entry short-circuits before a later good one is even attempted", {
  result <- build_request_dataframes(list(
    list(name = "bad", columns = list(), data = list()),
    list(name = "good", columns = list(list(Name = "Value", Type = "double")), data = list(list(Value = 1)))
  ))

  expect_null(result$frames)
  expect_equal(result$error, dataframe_build_error("bad"))
})
