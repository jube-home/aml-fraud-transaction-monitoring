

library(testthat)
result <- test_dir("tests/testthat", stop_on_failure = FALSE)
if (any(as.data.frame(result)$failed > 0)) quit(status = 1)
