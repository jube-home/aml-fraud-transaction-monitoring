options(
  repos = c(CRAN = "https://packagemanager.posit.co/cran/__linux__/noble/latest"),
  HTTPUserAgent = sprintf("R/%s R (%s)", getRversion(), paste(getRversion(), R.version["platform"], R.version["arch"], R.version["os"])),
  Ncpus = parallel::detectCores(),
  timeout = 600
)

Sys.setenv(MAKEFLAGS = paste0("-j", parallel::detectCores()))

lib <- Sys.getenv("R_LIBRARY")

.libPaths(c(lib, .libPaths()))

install.packages("BiocManager", lib = lib)
library(BiocManager, lib.loc = lib)
BiocManager::install(c("graph", "RBGL"), lib = lib, update = FALSE, ask = FALSE)

packages <- c(

  "rpart", "rpart.plot", "C50", "gbm", "randomForest", "ipred", "ada",

  "e1071", "class", "FNN", "kernlab", "KRLS",

  "cluster", "mclust", "isotree", "meanShiftR",

  "bnlearn", "gRbase", "gRain",

  "neuralnet",

  "triangle",

  "dplyr", "readr", "gmodels", "RODBC",

  "ROCR",

  "moments",

  "ggplot2",

  "plumber", "lintr", "jsonlite", "testthat", "mirai", "promises",

  "httr", "processx"
)

failures <- character(0)
for (pkg in packages) {
  if (requireNamespace(pkg, quietly = TRUE, lib.loc = lib)) next
  cat("== installing:", pkg, "==\n")
  tryCatch(
    install.packages(pkg, lib = lib),
    error = function(e) cat("install.packages() raised an error for", pkg, ":", conditionMessage(e), "\n")
  )
  if (!requireNamespace(pkg, quietly = TRUE, lib.loc = lib)) {
    failures <- c(failures, pkg)
  }
}

if (length(failures) > 0) {
  stop("Failed to install: ", paste(failures, collapse = ", "))
}

cat("All", length(packages), "packages installed to", lib, "\n")
