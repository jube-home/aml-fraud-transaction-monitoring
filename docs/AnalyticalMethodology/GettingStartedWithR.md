---
layout: default
title: Getting Started with R
nav_order: 3
parent: Analytical Methodology
---

# Getting Started with R

This module is almost entirely R environment mechanics rather than analytics: launching the R command line, setting a
working directory, navigating RStudio, managing packages, and using
`help()`. Most of the eighteen original procedures describe clicking through Windows dialogs (the original guide
targeted R 3.3.2 for Windows and a matching RStudio release) and have no script to run. Where a procedure does contain
real R code, it was re-run in the shared validation container (`rocker/r-ver:4.4.1`) and the console output below is the
genuine captured stdout, not a transcription of the old screenshots. The container has no `d:\R` drive, so `/work`
stands in for the original example working directory — the `getwd()`/`setwd()` semantics are identical.

Package and environment management (Procedures 6, 7, 9, 10, 11 and 13) is the one place in this module where a real
`AdvancedAnalytics` equivalent exists, and it applies the same way to all of them: the `AdvancedAnalyticsTool` layer and
the decoupled R script runner tool are both already-running services you call, not something you `library()`, search the
path for, or
`detach()` yourself. There is no package to install, load, list or unload in the product because there is nothing to
manage — the tool surface is simply there. That point is not repeated under every one of those procedures below; each
just notes "none" and refers back here.

### Procedure 1: Navigate to and launch the R command line.

The original procedure is Windows Explorer and Command Prompt navigation: open a command prompt, switch to the `C:`
drive, `cd` into the R installation's `bin` directory, and type `R` to launch the interactive console. There is no
script here to validate — it is OS navigation to an executable, shown only as a precursor to RStudio.

**Tool equivalent:** none — this is OS-level tooling, not analytics.

### Procedure 2: Issue commands to the R Console.

The console reads one line at a time, executes it immediately, and prints the result. The mathematical operators (`+`,
`-`, `/`, `*`) work as they do in Excel. Exiting the console with Ctrl+D offers to save (`y`), discard (`n`) or cancel
(`c`) the session's history.

```R
1+1
```

Console output:

```
[1] 2
```

`[1]` is the position within the returned vector, not part of the value.

**Tool equivalent:** none — R/script-runner only.

### Procedure 3: Set a Working Directory.

`getwd()` reports the directory R will read and write files against; `setwd()` changes it. Saving a session's history to
the same directory as the R executable itself fails, which is what makes a working directory necessary before doing
anything else.

```R
getwd()
setwd("/work")
getwd()
```

Console output:

```
[1] "/"
[1] "/work"
```

**Tool equivalent:** none as a literal function — but the underlying concept doesn't carry over either. The
product has no session working directory to set: data lives in
`AdvancedAnalyticsCache` as a packed `double[]` with an explicit `Rows`/`Columns` shape (see
`docs/AnalyticalMethodology/Reference/FoundationData.md`), not as files a session points at on disk.

### Procedure 4: Run a script from the R command line.

Rather than typing commands into the console one line at a time, a script file can be handed to
`Rscript` (the original guide's `RScript.exe`) for unattended execution. The file below is the same one-line calculation
as Procedure 2, saved with a trailing comment (text after `#` is ignored by R):

```R
1+1 # Adds one and one together
```

Invoked as `Rscript OnePlusOne.R`. Console output:

```
[1] 2
```

**Tool equivalent:** none — R/script-runner only.

### Procedure 5: Launching R Studio.

RStudio is a separate installation that overlays Core R with an IDE: a script pane, a console pane, an environment pane
and a packages pane. The original procedure is Start-menu navigation to the RStudio application. All procedures from
here on use a "script active, console passive" style — code is written and run from the script pane, never typed
directly into the console — except for the two procedures that call this out explicitly.

**Tool equivalent:** none — IDE navigation, not analytics.

### Procedure 6: Identify Packages Installed.

`searchpaths()` lists the search path's physical package directories; `search()` gives the same list as short,
human-readable names.

```R
searchpaths()
search()
```

Console output:

```
[1] ".GlobalEnv"                         "/usr/local/lib/R/library/stats"    
[3] "/usr/local/lib/R/library/graphics"  "/usr/local/lib/R/library/grDevices"
[5] "/usr/local/lib/R/library/utils"     "/usr/local/lib/R/library/datasets" 
[7] "/usr/local/lib/R/library/methods"   "Autoloads"                         
[9] "/usr/local/lib/R/library/base"     
[1] ".GlobalEnv"        "package:stats"     "package:graphics" 
[4] "package:grDevices" "package:utils"     "package:datasets" 
[7] "package:methods"   "Autoloads"         "package:base"
```

(Paths differ from the original Windows output only because the container installs R under
`/usr/local/lib/R` rather than `C:\Program Files\R\R-3.3.2`; the mechanism is identical.)

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 7: Browsing and Installing Packages.

Installing a package (the original guide installs `ggplot2` from CRAN) is done through RStudio's Packages pane: click
Install, type the package name, keep "Install Dependencies" checked, and RStudio sends the underlying
`install.packages()` call to the console for you. Nothing is typed by the user in this procedure, so there is no script
to validate.

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 8: Review Help and Documentation.

The Packages pane links each installed package's name to its documentation index, and each function listed there to its
own help page — for example, clicking through to `ggplot2` and then to
`autoplot`. This is pane navigation with no console interaction; Procedure 12 below covers the equivalent through code
with `help()`.

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 9: Load and Unload Packages in RStudio.

Ticking the checkbox next to a package's name in the Packages pane sends `library(packageName)`
to the console; unticking it sends `detach()`. No script is typed directly by the user — the checkbox is the only
interaction — so Procedures 10 and 13 below cover the same two functions as typed code.

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 10: Load Packages using Script.

Keeping to the "script active" convention, `library()` calls belong at the head of the script rather than being
triggered from the Packages pane checkbox.

```R
library(ggplot2)
```

Console output: (none — a successful `library()` call is silent)

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 11: List all Functions in a Package.

`ls("package:ggplot2")` lists every object a loaded package exports. This is one of the few procedures the original
guide targets at the console directly rather than the script, to avoid cluttering it.

```R
ls("package:ggplot2")
```

Console output (649 exported objects; first and last few shown):

```
  [1] "%+%"                        "%+replace%"                
  [3] "add_gg"                     "aes"                       
  [5] "aes_"                       "aes_all"                   
  [7] "aes_auto"                   "aes_q"                     
  [9] "aes_string"                 "after_scale"               
 [11] "after_stat"                 "alpha"                     
 [13] "annotate"                   "annotation_borders"        
   ...
[643] "waiver"                     "wrap_dims"                 
[645] "xlab"                       "xlim"                      
[647] "ylab"                       "ylim"                      
[649] "zeroGrob"                  
```

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 12: Use the help () function to explain a function.

`help()` looks up a function's documentation page. In RStudio it renders in the Help pane; run at a plain console (as in
this validation) it prints the same page as text.

```R
help("ggplot")
```

Console output (abridged — the full page also lists every argument and three worked examples):

```
ggplot                 package:ggplot2                 R Documentation

_C_r_e_a_t_e _a _n_e_w _g_g_p_l_o_t

_D_e_s_c_r_i_p_t_i_o_n:

     'ggplot()' initializes a ggplot object. It can be used to declare
     the input data frame for a graphic and to specify the set of
     aesthetic mappings for the plot, intended to be common throughout
     all subsequent layers unless specifically overridden.

_U_s_a_g_e:

     ggplot(data = NULL, mapping = aes(), ..., environment = parent.frame())
     
_A_r_g_u_m_e_n_t_s:

    data: Default dataset to use for plot. If not already a data.frame,
          will be converted to one by 'fortify()'. If not specified,
          must be supplied in each layer added to the plot.

 mapping: Default list of aesthetic mappings to use for plot. If not
          specified, must be supplied in each layer added to the plot.
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.AdvancedAnalyticsToolCatalogue`
/ `AdvancedAnalyticsToolDescriptor` — the closest genuine equivalent in this module. Every tool method in the
`AdvancedAnalyticsTools` namespace carries a `[Description]` attribute; `Discover()`
reflects over the assembly and returns one `AdvancedAnalyticsToolDescriptor` per tool (group name, tool name,
description, required permissions). Looking up what a tool does is a catalogue query against already-attached metadata,
not a per-function documentation page you navigate to.

### Procedure 13: Unloading a Package.

`detach()` removes a loaded package from the session, with `unload = TRUE` also unloading its namespace.

```R
detach("package:ggplot2", unload = TRUE)
```

Console output: (none — a successful `detach()` call is silent)

**Tool equivalent:** none — see the package/environment management note at the top of this module.

### Procedure 14: Creating a Numeric Variable by Assignment.

Variables are created by assignment using `<-`. Assigning a bare number creates a numeric variable; `mode()` reports a
variable's type; a variable can also be assigned the result of an expression such as `1 + 1` rather than a literal.

```R
Numeric <- 1
Numeric
mode(Numeric)
NumericResult <- 1 + 1
NumericResult
```

Console output:

```
[1] 1
[1] "numeric"
[1] 2
```

**Tool equivalent:** none — R/script-runner only.

### Procedure 15: Create a String Variable by Assignment.

String literals are wrapped in double quotes; assignment and inspection work the same as for numeric variables.

```R
Char <- "Test"
Char
mode(Char)
```

Console output:

```
[1] "Test"
[1] "character"
```

**Tool equivalent:** none — R/script-runner only.

### Procedure 16: Create a Logical Variable by Assignment.

A logical variable is the `TRUE`/`FALSE` result of a comparison between two other variables.

```R
x <- 1
y <- 2
Logical <- x > y
Logical
mode(Logical)
```

Console output:

```
[1] FALSE
[1] "logical"
```

**Tool equivalent:** none — R/script-runner only.

### Procedure 17: List Variables in R.

`ls()` with no arguments lists every object currently in the session's environment — the same function used in Procedure
11 to list a package's exports, here defaulting to the global environment.

```R
ls()
```

Console output (continuing the session from Procedures 14–16):

```
[1] "Char"          "Logical"       "Numeric"       "NumericResult"
[5] "x"             "y"            
```

**Tool equivalent:** none — R/script-runner only.

### Procedure 18: Remove Variables in R.

`remove()` (aliased to `rm()`) deletes a named object from the session, freeing whatever memory it held — relevant once
a script has accumulated large intermediate objects it no longer needs.

```R
remove("Char")
ls()
```

Console output:

```
[1] "Logical"       "Numeric"       "NumericResult" "x"            
[5] "y"
```

`Char` is gone from the environment; the remaining five objects from Procedures 14–16 are unaffected.

**Tool equivalent:** none — R/script-runner only.
