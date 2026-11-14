---
layout: default
title: Logging in Visualisation Datasource
nav_order: 8
parent: Visualisations
grand_parent: Configuration
---

# Logging in Visualisation Datasource

On each recall of a Visualisation datasource there is extensive logging in the database for the purpose of capturing
user inputs, the time taken to process the SQL and an errors returned by the database.

Given SQL is validated on the creation of a Visualisation Datasource and all parameters are passed as named variables,
the likelihood of errors at recall are reduced.

To inspect the datasource recall:

``` sql
select * from "VisualisationRegistryDatasourceExecutionLog"
```

Returning the following data:

<!-- TODO(screenshot): file=visualisation-executions.png route=/administration/visualisations state="Replace the Razor-era image Executions.png with the Vue Administration > Visualisations page showing the same state" -->
![Image](Executions.png)

There exists the possibility to join onto the VisualisationRegistryDatasource to obtain the SQL:

``` sql
select d."Command",l."ResponseTime" from "VisualisationRegistryDatasourceExecutionLog" l
inner join "VisualisationRegistryDatasource"d on d."Id" = l."VisualisationRegistryDatasourceId"
```

Returning the following data:

<!-- TODO(screenshot): file=visualisation-data-returned-from-join-on-datasource.png route=/administration/visualisations state="Replace the Razor-era image DataReturnedFromJoinOnDatasource.png with the Vue Administration > Visualisations page showing the same state" -->
![Image](DataReturnedFromJoinOnDatasource.png)

The table should be inspected for excessive response times and Errors. In the case of long response times, it is
advisable to manually test the explain plan of the SQL composed and seek optimisation.

The parameters that have been passed in this execution and parameterization of the execution are available:

``` sql
select * from "VisualisationRegistryDatasourceExecutionLogParameter"
```

Returning the following data:

<!-- TODO(screenshot): file=visualisation-paramaters.png route=/administration/visualisations state="Replace the Razor-era image Paramaters.png with the Vue Administration > Visualisations page showing the same state" -->
![Image](Paramaters.png)

To obtain the name of the parameter a join:

``` sql
select p."Name",l."Value" from "VisualisationRegistryDatasourceExecutionLogParameter" l
inner join "VisualisationRegistryParameter" p on p."Id" = l."VisualisationRegistryParameterId"
```

Returning the following data:

<!-- TODO(screenshot): file=visualisation-data-returned-from-join-on-parameters.png route=/administration/visualisations state="Replace the Razor-era image DataReturnedFromJoinOnParameters.png with the Vue Administration > Visualisations page showing the same state" -->
![Image](DataReturnedFromJoinOnParameters.png)

Keep in mind that the SQL cannot be injected as it is validated then stored alongside the datasource and the parameters
are passed on a secure basis. Notwithstanding the secure development of the SQL invocation, the parameters log should be
monitored for potentially malicious statements.

<!-- TODO(docs): Verify the execution log shows the card path (success and SQL-rejected failures) and that Preview and Introspect are not logged. -->
<!-- TODO(docs): Check that the log Error column holds the full exception text for the agent-tool execution path and the SQL-rejected message for the card path; owner to decide whether stack traces should be stored. -->
