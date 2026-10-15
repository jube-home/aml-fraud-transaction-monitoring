

source(file.path("..", "..", "R", "sandbox.R"))

hostile_seeds <- list(
  process_exec = 'system("id")',
  process_exec2 = 'system2("ls", "-la")',
  shell_exec = 'shell("dir")',
  pipe_exec = 'pipe("id")',
  qualified_base = 'base::system("id")',
  qualified_internal = 'base:::system("id")',
  get_indirection = 'get("system")("id")',
  get0_indirection = 'get0("system")("id")',
  do_call_string = 'do.call("system", list("id"))',
  do_call_get = 'do.call(get("system"), list("id"))',
  eval_parse_chain = 'eval(parse(text = "system(\'id\')"))',
  alias_then_call = 'f <- system\nf("id")',
  alias_equals = 'f = system\nf("id")',
  backtick_call = '`system`("id")',
  unlock_rebind = 'unlockBinding("system", baseenv()); assign("system", function(...) "pwned", envir = baseenv())',
  env_tamper = 'environment(mean) <- globalenv()',
  file_delete = 'unlink("/etc/passwd")',
  file_remove = 'file.remove("/etc/passwd")',
  file_rename = 'file.rename("/etc/passwd", "/tmp/x")',
  setwd_escape = 'setwd("/etc")',
  network_download = 'download.file("http://example.invalid/x", "/tmp/x")',
  network_url = 'con <- url("http://example.invalid/x")',
  network_socket = 'socketConnection("example.invalid", 80)',
  env_write = 'Sys.setenv(EVIL = "1")',
  env_read = 'Sys.getenv("SECRET")',
  quit_session = "quit()",
  q_session = "q()",
  reflection_call = "sys.call()",
  reflection_frame = "parent.frame()",
  reflection_env = "globalenv()",
  internal_escape = '.Internal(Sys.time())',
  dyn_load = 'dyn.load("/tmp/evil.so")',
  match_fun_indirection = 'match.fun("system")("id")',
  as_function_indirection = 'as.function(alist(x = , system(x)))("id")',
  install_packages = 'install.packages("some.package")',
  source_escape = 'source("/etc/passwd")',
  superassignment_leak = 'leaked_state <<- "pwned"',
  shell_exec_variant = 'shell.exec("calc.exe")',
  mget_indirection = 'mget("system")[[1]]("id")',
  recall_reflection = 'f <- function(n) if (n <= 0) 0 else Recall(n - 1)',
  evalq_call = 'evalq(1 + 1, globalenv())',
  sys_function_call = 'sys.function()',
  match_call_call = 'f <- function(x) match.call()',
  body_getter = 'body(mean)',
  formals_getter = 'formals(mean)',
  primitive_escape = '.Primitive("sum")(1, 2)',
  c_ffi = '.C("some_symbol")',
  call_ffi = '.Call("some_symbol")',
  fortran_ffi = '.Fortran("some_symbol")',
  external_ffi = '.External("some_symbol")',
  dyn_unload = 'dyn.unload("/tmp/x.so")',
  library_dynam = 'library.dynam("x", "x", "/tmp")',
  native_symbol_info = 'getNativeSymbolInfo("Rf_error")',
  lock_binding = 'lockBinding("x", globalenv())',
  lock_environment = 'lockEnvironment(globalenv())',
  delayed_assign = 'delayedAssign("x", 1)',
  topenv_call = 'topenv()',
  as_environment_call = 'as.environment(1)',
  sys_frame_call = 'sys.frame()',
  sys_unsetenv = 'Sys.unsetenv("PATH")',
  sys_setlocale = 'Sys.setlocale("LC_ALL", "C")',
  file_create = 'file.create("/tmp/x")',
  file_append = 'file.append("/tmp/a", "/tmp/b")',
  file_symlink = 'file.symlink("/etc/passwd", "/tmp/link")',
  file_link = 'file.link("/etc/passwd", "/tmp/link2")',
  sys_chmod = 'Sys.chmod("/tmp/x", "777")',
  sys_umask = 'Sys.umask("000")',
  make_socket = 'make.socket("example.invalid", 80)',
  curl_get_headers = 'curlGetHeaders("http://example.invalid")',
  remove_packages = 'remove.packages("stats")',
  sink_call = 'sink("/tmp/out.txt")',
  trace_call = 'trace("mean")',
  untrace_call = 'untrace("mean")',
  traceback_call = 'traceback()'
)

wrap_direct <- function(seed) seed
wrap_in_function <- function(seed) sprintf("f <- function() {\n%s\n}\nf()", seed)
wrap_in_if <- function(seed) sprintf("if (TRUE) {\n%s\n}", seed)
wrap_in_for <- function(seed) sprintf("for (i in 1:1) {\n%s\n}", seed)
wrap_in_trycatch <- function(seed) sprintf("tryCatch({\n%s\n}, error = function(e) NULL)", seed)
wrap_buried <- function(seed) sprintf("a <- 1\nb <- 2\nc <- a + b\n%s\nd <- c * 2", seed)
wrap_extra_whitespace <- function(seed) gsub(" ", "   \n   ", seed, fixed = TRUE)
wrap_with_comment <- function(seed) sprintf("# looks harmless\n%s\n# nothing to see here", seed)

wrappers <- list(
  direct = wrap_direct,
  in_function = wrap_in_function,
  in_if = wrap_in_if,
  in_for = wrap_in_for,
  in_trycatch = wrap_in_trycatch,
  buried = wrap_buried,
  extra_whitespace = wrap_extra_whitespace,
  with_comment = wrap_with_comment
)

cases <- expand.grid(
  seed_name = names(hostile_seeds),
  wrapper_name = names(wrappers),
  stringsAsFactors = FALSE
)

test_that("every hostile seed is rejected under every wrapping shape", {
  failures <- character(0)
  for (i in seq_len(nrow(cases))) {
    seed_name <- cases$seed_name[i]
    wrapper_name <- cases$wrapper_name[i]
    script <- wrappers[[wrapper_name]](hostile_seeds[[seed_name]])

    result <- validate_script(script)
    if (isTRUE(result$ok)) {
      failures <- c(failures, sprintf("%s / %s", seed_name, wrapper_name))
      next
    }

    outcome <- run_sandboxed(script, timeout_seconds = 2)
    if (outcome$status != "rejected") {
      failures <- c(failures, sprintf("%s / %s (ran instead of rejected: status=%s)",
                                       seed_name, wrapper_name, outcome$status))
    }
  }

  if (length(failures) > 0) {
    fail(sprintf(
      "%d of %d hostile combinations were NOT rejected:\n%s",
      length(failures), nrow(cases), paste(failures, collapse = "\n")
    ))
  }
  expect_equal(length(failures), 0)
})

test_that("the corpus actually covers every forbidden symbol at least once", {
  corpus_text <- paste(unlist(hostile_seeds), collapse = "\n")
  parsed <- parse(text = corpus_text)
  referenced <- find_forbidden_symbols(parsed)
  untested <- setdiff(FORBIDDEN_SYMBOLS, referenced)
  expect_equal(untested, character(0),
               info = paste("Forbidden symbols with no corpus seed:", paste(untested, collapse = ", ")))
})

test_that("benign scripts using the pre-installed packages are NOT rejected", {
  benign <- list(
    'library(stats)\nmean(c(1, 2, 3))',
    'x <- rnorm(100)\nsummary(x)',
    'library(dplyr)\ndata.frame(a = 1:3) %>% filter(a > 1)',
    'f <- function(x) x^2\nsapply(1:5, f)',
    'for (i in 1:3) print(i)',
    'tryCatch(stop("boom"), error = function(e) conditionMessage(e))',
    'writeLines("hello")',
    'saveRDS(list(a = 1), file.path(tempdir(), "model.rds"))'
  )
  for (script in benign) {
    result <- validate_script(script)
    expect_true(result$ok, info = paste("Unexpectedly rejected:", script, "-", paste(result$errors, collapse = "; ")))
  }
})
