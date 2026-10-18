

simplify_column <- function(values) {
  if (length(values) == 0) {
    return(character(0))
  }

  present <- values[!vapply(values, is.null, logical(1))]

  if (length(present) == 0) {
    return(rep(NA, length(values)))
  }

  is_logical <- all(vapply(present, is.logical, logical(1)))
  is_numeric <- !is_logical && all(vapply(present, is.numeric, logical(1)))

  vapply(values, function(value) {
    if (is.null(value)) {
      if (is_logical) return(NA)
      if (is_numeric) return(NA_real_)
      return(NA_character_)
    }
    if (is_logical) return(as.logical(value))
    if (is_numeric) return(as.numeric(value))
    as.character(value)
  }, if (is_logical) logical(1) else if (is_numeric) numeric(1) else character(1))
}

rows_of_object_array <- function(x) {
  if (is.null(x)) {
    return(list())
  }
  if (is.data.frame(x)) {
    if (nrow(x) == 0) {
      return(list())
    }
    column_names <- names(x)
    return(lapply(seq_len(nrow(x)), function(i) {
      setNames(lapply(column_names, function(column_name) x[[column_name]][[i]]), column_names)
    }))
  }
  if (is.list(x) && length(x) > 0) {
    return(x)
  }
  list()
}

dataframe_build_error <- function(name) {
  sprintf("could not build dataframe: %s", name)
}

build_dataframe_from_json <- function(columns, data) {
  column_entries <- rows_of_object_array(columns)

  if (length(column_entries) == 0) {
    return(NULL)
  }

  column_names <- vapply(column_entries, function(column) column$Name, character(1))
  row_entries <- rows_of_object_array(data)

  as.data.frame(
    setNames(
      lapply(column_names, function(column_name) {
        simplify_column(lapply(row_entries, function(one_row) one_row[[column_name]]))
      }),
      column_names
    ),
    stringsAsFactors = FALSE, check.names = FALSE
  )
}

build_request_dataframes <- function(dataframe_requests) {
  if (length(dataframe_requests) == 0) {
    return(list(frames = list(), error = NULL))
  }

  frames <- list()

  for (entry in dataframe_requests) {
    frame <- tryCatch(build_dataframe_from_json(entry$columns, entry$data), error = function(e) NULL)

    if (is.null(frame)) {
      return(list(frames = NULL, error = dataframe_build_error(entry$name)))
    }

    frames[[entry$name]] <- frame
  }

  list(frames = frames, error = NULL)
}
