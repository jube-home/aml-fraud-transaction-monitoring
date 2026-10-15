

library(plumber)

host <- Sys.getenv("JUBE_R_HOST", "0.0.0.0")
port <- as.integer(Sys.getenv("JUBE_R_PORT", "8080"))

pr("plumber.R") |> pr_run(host = host, port = port)
