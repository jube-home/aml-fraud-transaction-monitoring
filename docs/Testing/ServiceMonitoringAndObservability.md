---
layout: default
title: Service Monitoring and Observability
nav_order: 18
parent: Testing
---

# Service Monitoring and Observability

Tests of the monitoring services that surface engine counters, Redis, Postgres, Patroni, etcd, Docker, HAProxy and
OpenTelemetry health to the interface.

## Application log entry

### ApplicationLogEntryServiceTests

`Jube.Test.Service.ApplicationLogEntry.ApplicationLogEntryServiceTests` — 18 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsAllFieldsAsync`          | Fact   | List returns most recent first and projects all fields.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByLevelAscendingAndDescendingAsync`                  | Fact   | List sorts by level ascending and descending.                      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Archiver stage performance counter

### ArchiverStagePerformanceCounterServiceTests

`Jube.Test.Service.ArchiverStagePerformanceCounter.ArchiverStagePerformanceCounterServiceTests` — 21 tests. Category:
Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`          | Fact   | List returns most recent first and projects model name.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`           | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByStageIdAsync`                                    | Fact   | List filters by stage id.                                          |
| `ListIncludesEveryTenantForLandlordUserAsync`                  | Fact   | List includes every tenant for landlord user.                      |
| `ListSortsByTotalMicrosecondsAscendingAndDescendingAsync`      | Fact   | List sorts by total microseconds ascending and descending.         |
| `ListSortsByStageNameAscendingAndDescendingAsync`              | Fact   | List sorts by stage name ascending and descending.                 |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForTotalMicrosecondsAsync`   | Fact   | List statistics computes exact values for total microseconds.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Archiver warning

### ArchiverWarningServiceTests

`Jube.Test.Service.ArchiverWarning.ArchiverWarningServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                            | Kind   | Verifies                                                           |
|-----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`           | Fact   | List returns most recent first and projects model name.            |
| `ListClampsTakeToOneHundredThousandAsync`                       | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                     | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`          | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`             | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`            | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`            | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListIncludesEveryTenantForLandlordUserAsync`                   | Fact   | List includes every tenant for landlord user.                      |
| `ListFiltersByDateRangeAsync`                                   | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`               | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByStageIdAsync`                                     | Fact   | List filters by stage id.                                          |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`     | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`  | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListSortsByDurationMicrosecondsAscendingAndDescendingAsync`    | Fact   | List sorts by duration microseconds ascending and descending.      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`           | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`        | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`  | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`  | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForDurationMicrosecondsAsync` | Fact   | List statistics computes exact values for duration microseconds.   |

## Capture queue health

### CaptureQueueHealthServiceTests

`Jube.Test.Service.CaptureQueueHealth.CaptureQueueHealthServiceTests` — 17 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByQueueIdAsync`                                    | Fact   | List filters by queue id.                                          |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListSortsByQueueDepthAscendingAndDescendingAsync`             | Fact   | List sorts by queue depth ascending and descending.                |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Case creation stage performance counter

### CaseCreationStagePerformanceCounterServiceTests

`Jube.Test.Service.CaseCreationStagePerformanceCounter.CaseCreationStagePerformanceCounterServiceTests` — 17 tests.
Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsStageNameAsync`          | Fact   | List returns most recent first and projects stage name.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByStageIdAsync`                                    | Fact   | List filters by stage id.                                          |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListSortsByTotalMicrosecondsAscendingAndDescendingAsync`      | Fact   | List sorts by total microseconds ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Case creation warning

### CaseCreationWarningServiceTests

`Jube.Test.Service.CaseCreationWarning.CaseCreationWarningServiceTests` — 20 tests. Category: Service. Collection:
Database.

| Test                                                            | Kind   | Verifies                                                           |
|-----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`              | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                       | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                     | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`          | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`             | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`            | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`            | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListIncludesEveryTenantForLandlordUserAsync`                   | Fact   | List includes every tenant for landlord user.                      |
| `ListFiltersByDateRangeAsync`                                   | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`               | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByStageIdAsync`                                     | Fact   | List filters by stage id.                                          |
| `ListFiltersBySearchAgainstCaseKeyValueCaseInsensitivelyAsync`  | Fact   | List filters by search against case key value case insensitively.  |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`     | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`  | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListSortsByDurationMicrosecondsAscendingAndDescendingAsync`    | Fact   | List sorts by duration microseconds ascending and descending.      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`           | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`        | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`  | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`  | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForDurationMicrosecondsAsync` | Fact   | List statistics computes exact values for duration microseconds.   |

## Container log entry

### ContainerLogEntryServiceTests

`Jube.Test.Service.ContainerLogEntry.ContainerLogEntryServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsAllFieldsAsync`          | Fact   | List returns most recent first and projects all fields.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListFiltersBySearchAgainstContainerNameAsync`                 | Fact   | List filters by search against container name.                     |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByContainerNameAscendingAndDescendingAsync`          | Fact   | List sorts by container name ascending and descending.             |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Docker container metric

### DockerContainerMetricServiceTests

`Jube.Test.Service.DockerContainerMetric.DockerContainerMetricServiceTests` — 20 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListFiltersBySearchAgainstImageStateAndStatusAsync`           | Fact   | List filters by search against image state and status.             |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListSortsByMemoryUsageBytesAscendingAndDescendingAsync`       | Fact   | List sorts by memory usage bytes ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForMemoryUsageBytesAsync`    | Fact   | List statistics computes exact values for memory usage bytes.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Docker event

### DockerEventServiceTests

`Jube.Test.Service.DockerEvent.DockerEventServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsAllFieldsAsync`          | Fact   | List returns most recent first and projects all fields.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersBySearchAgainstActionAsync`                        | Fact   | List filters by search against action.                             |
| `ListFiltersBySearchAgainstActorNameAsync`                     | Fact   | List filters by search against actor name.                         |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListSortsByActionAscendingAndDescendingAsync`                 | Fact   | List sorts by action ascending and descending.                     |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Docker host metric

### DockerHostMetricServiceTests

`Jube.Test.Service.DockerHostMetric.DockerHostMetricServiceTests` — 20 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAgainstInstanceAsync`                      | Fact   | List filters by search against instance.                           |
| `ListFiltersBySearchAgainstOperatingSystemAsync`               | Fact   | List filters by search against operating system.                   |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByMemTotalBytesAscendingAndDescendingAsync`          | Fact   | List sorts by mem total bytes ascending and descending.            |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForMemTotalBytesAsync`       | Fact   | List statistics computes exact values for mem total bytes.         |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Dot net runtime metric

### DotNetRuntimeMetricServiceTests

`Jube.Test.Service.DotNetRuntimeMetric.DotNetRuntimeMetricServiceTests` — 19 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByHeapSizeBytesAscendingAndDescendingAsync`          | Fact   | List sorts by heap size bytes ascending and descending.            |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForHeapSizeBytesAsync`       | Fact   | List statistics computes exact values for heap size bytes.         |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Entity analysis asynchronous queue balance

### EntityAnalysisAsynchronousQueueBalanceServiceTests

`Jube.Test.Service.EntityAnalysisAsynchronousQueueBalance.EntityAnalysisAsynchronousQueueBalanceServiceTests` — 20
tests. Category: Service. Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                           |
|------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAsync`                                            | Fact   | List returns most recent first.                                                    |
| `ListClampsTakeToOneHundredThousandAsync`                                    | Fact   | List clamps take to one hundred thousand.                                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                                  | Fact   | List without permission throws forbidden.                                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`                       | Fact   | Non landlord holding every specification is forbidden.                             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                          | Fact   | Create with null user name throws not authenticated.                               |
| `CreateWithBlankUserNameThrowsNotAuthenticatedAsync`                         | Fact   | Create with blank user name throws not authenticated.                              |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                         | Fact   | Create with unknown tenant throws not authenticated.                               |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`                  | Fact   | List with zero sample percentage excludes every matching row.                      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`               | Fact   | List with hundred sample percentage includes every matching row.                   |
| `ListFiltersByDateRangeAsync`                                                | Fact   | List filters by date range.                                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`                            | Fact   | List defaults to last hour when from and to omitted.                               |
| `ListSortsByCaseCreationAscendingAndDescendingAsync`                         | Fact   | List sorts by case creation ascending and descending.                              |
| `ListSortsByInstanceAscendingAndDescendingAsync`                             | Fact   | List sorts by instance ascending and descending.                                   |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`                        | Theory | List sort direction is case insensitive for ascending.                             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`                     | Fact   | List with garbage sort direction falls back to descending.                         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`               | Fact   | List with unrecognised sort field falls back to most recent first.                 |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`               | Fact   | List total reflects full filtered count even when take is smaller.                 |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`                   | Fact   | List statistics includes every continuous measured column.                         |
| `ListNameIsAlwaysNullAsync`                                                  | Fact   | List name is always null.                                                          |
| `ListAsynchronousEntityInvokeIsAlwaysZeroEvenWhenUnderlyingColumnIsSetAsync` | Fact   | List asynchronous entity invoke is always zero even when underlying column is set. |

## Entity analysis model asynchronous queue balance

### EntityAnalysisModelAsynchronousQueueBalanceServiceTests

`Jube.Test.Service.EntityAnalysisModelAsynchronousQueueBalance.EntityAnalysisModelAsynchronousQueueBalanceServiceTests` — 20 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`          | Fact   | List returns most recent first and projects model name.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`           | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListIncludesEveryTenantForLandlordUserAsync`                  | Fact   | List includes every tenant for landlord user.                      |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersBySearchAgainstModelNameCaseInsensitivelyAsync`    | Fact   | List filters by search against model name case insensitively.      |
| `ListSortsByArchiveAscendingAndDescendingAsync`                | Fact   | List sorts by archive ascending and descending.                    |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForArchiveAsync`             | Fact   | List statistics computes exact values for archive.                 |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Entity analysis model processing counter

### EntityAnalysisModelProcessingCounterServiceTests

`Jube.Test.Service.EntityAnalysisModelProcessingCounter.EntityAnalysisModelProcessingCounterServiceTests` — 24 tests.
Category: Service. Collection: Database.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`                         | Fact   | List returns most recent first and projects model name.                             |
| `ListClampsTakeToOneHundredThousandAsync`                                     | Fact   | List clamps take to one hundred thousand.                                           |
| `ListWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | List without permission throws forbidden.                                           |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`                        | Fact   | Non landlord holding every specification is forbidden.                              |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                           | Fact   | Create with null user name throws not authenticated.                                |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                          | Fact   | Create with unknown tenant throws not authenticated.                                |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`                   | Fact   | List with zero sample percentage excludes every matching row.                       |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`                | Fact   | List with hundred sample percentage includes every matching row.                    |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`                          | Fact   | List is forbidden for non landlord users of any tenant.                             |
| `ListIncludesEveryTenantForLandlordUserAsync`                                 | Fact   | List includes every tenant for landlord user.                                       |
| `ListFiltersByDateRangeAsync`                                                 | Fact   | List filters by date range.                                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`                             | Fact   | List defaults to last hour when from and to omitted.                                |
| `ListFiltersBySearchAgainstModelNameCaseInsensitivelyAsync`                   | Fact   | List filters by search against model name case insensitively.                       |
| `ListSortsByModelInvokeAscendingAndDescendingAsync`                           | Fact   | List sorts by model invoke ascending and descending.                                |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`                         | Theory | List sort direction is case insensitive for ascending.                              |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`                      | Fact   | List with garbage sort direction falls back to descending.                          |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`                | Fact   | List with unrecognised sort field falls back to most recent first.                  |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`                | Fact   | List total reflects full filtered count even when take is smaller.                  |
| `ListProjectsMinAndMaxResponseTimeMicrosecondsAndArchiveWalPendingCountAsync` | Fact   | List projects min and max response time microseconds and archive wal pending count. |
| `ListIdIsAlwaysZeroAsync`                                                     | Fact   | List id is always zero.                                                             |
| `ListStatisticsComputesExactValuesForResponseElevationSumAsync`               | Fact   | List statistics computes exact values for response elevation sum.                   |
| `ListStatisticsWithSingleValueHasZeroStandardDeviationAsync`                  | Fact   | List statistics with single value has zero standard deviation.                      |
| `ListStatisticsOverZeroMatchingRowsDoesNotThrowAsync`                         | Fact   | List statistics over zero matching rows does not throw.                             |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`                    | Fact   | List statistics includes every continuous measured column.                          |

## Entity analysis model response time pipeline counter

### EntityAnalysisModelResponseTimePipelineCounterServiceTests

`Jube.Test.Service.EntityAnalysisModelResponseTimePipelineCounter.EntityAnalysisModelResponseTimePipelineCounterServiceTests` — 20 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`          | Fact   | List returns most recent first and projects model name.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByStageIdAsync`                                    | Fact   | List filters by stage id.                                          |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`           | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListIncludesEveryTenantForLandlordUserAsync`                  | Fact   | List includes every tenant for landlord user.                      |
| `ListSortsByTotalMicrosecondsAscendingAndDescendingAsync`      | Fact   | List sorts by total microseconds ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForTotalMicrosecondsAsync`   | Fact   | List statistics computes exact values for total microseconds.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Entity analysis model stage performance counter

### EntityAnalysisModelStagePerformanceCounterServiceTests

`Jube.Test.Service.EntityAnalysisModelStagePerformanceCounter.EntityAnalysisModelStagePerformanceCounterServiceTests` — 20 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`          | Fact   | List returns most recent first and projects model name.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`           | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByStageIdAsync`                                    | Fact   | List filters by stage id.                                          |
| `ListIncludesEveryTenantForLandlordUserAsync`                  | Fact   | List includes every tenant for landlord user.                      |
| `ListSortsByTotalMicrosecondsAscendingAndDescendingAsync`      | Fact   | List sorts by total microseconds ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForTotalMicrosecondsAsync`   | Fact   | List statistics computes exact values for total microseconds.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Entity analysis model task performance counter

### EntityAnalysisModelTaskPerformanceCounterServiceTests

`Jube.Test.Service.EntityAnalysisModelTaskPerformanceCounter.EntityAnalysisModelTaskPerformanceCounterServiceTests` — 20
tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsModelNameAsync`          | Fact   | List returns most recent first and projects model name.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByDirectionAndTaskTypeIdAsync`                     | Fact   | List filters by direction and task type id.                        |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync`           | Fact   | List is forbidden for non landlord users of any tenant.            |
| `ListIncludesEveryTenantForLandlordUserAsync`                  | Fact   | List includes every tenant for landlord user.                      |
| `ListSortsByTotalMicrosecondsAscendingAndDescendingAsync`      | Fact   | List sorts by total microseconds ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForTotalMicrosecondsAsync`   | Fact   | List statistics computes exact values for total microseconds.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Entity analysis model ttl counter

### EntityAnalysisModelTtlCounterServiceTests

`Jube.Test.Service.EntityAnalysisModelTtlCounter.EntityAnalysisModelTtlCounterServiceTests` — 52 tests. Category:
Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `GetByEntityAnalysisModelGuidReturnsOnlyRowsForThatModelAsync`                        | Fact   | Get by entity analysis model guid returns only rows for that model.                         |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `TtlCounterDataNameRequiredRejectsEmptyAsync`                                         | Fact   | Ttl counter data name required rejects empty.                                               |
| `InvalidTtlCounterIntervalIsRejectedAsync`                                            | Fact   | Invalid ttl counter interval is rejected.                                                   |
| `TtlCounterValueNegativeIsRejectedAsync`                                              | Fact   | Ttl counter value negative is rejected.                                                     |
| `InvalidResolutionIntervalIsRejectedAsync`                                            | Fact   | Invalid resolution interval is rejected.                                                    |
| `TtlCounterDataValueRequiredWhenSumEnabledAsync`                                      | Fact   | Ttl counter data value required when sum enabled.                                           |
| `TtlCounterDataValueNotRequiredWhenSumDisabledAsync`                                  | Fact   | Ttl counter data value not required when sum disabled.                                      |
| `SumEnabledWithDataValueSucceedsAsync`                                                | Fact   | Sum enabled with data value succeeds.                                                       |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on update have no effect.                                |
| `UpdatedUserAndUpdatedDateAreAlwaysNullNoBackingColumnAsync`                          | Fact   | Updated user and updated date are always null no backing column.                            |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                | Fact   | Update of locked row throws not found.                                                      |
| `UpdateOfSoftDeletedRowRevivesItAsync`                                                | Fact   | Update of soft deleted row revives it.                                                      |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                           | Fact   | Update of never existed id throws not found.                                                |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                               | Fact   | Delete of missing or already deleted row throws not found.                                  |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                | Fact   | Resubmit of non rejected version throws approval refused and writes no row.                 |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                | Fact   | Resubmit of rejected version bumps version archives old and keeps content.                  |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                     | Fact   | Resubmit of rejected delete keeps deleted true and reopens approval.                        |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                               | Fact   | Retract of non rejected version throws approval refused.                                    |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                             | Fact   | Retract with no approved baseline throws approval refused.                                  |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync` | Fact   | Retract of rejected edit restores last approved content and stamps the rejection retracted. |
| `RetractOfRejectedDeleteUndeletesAsync`                                               | Fact   | Retract of rejected delete undeletes.                                                       |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                      | Fact   | Active and locked round trip bool to byte and back.                                         |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                     | Fact   | Get by id for missing id returns null not exception.                                        |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                 | Fact   | Pre cancelled token on read throws operation canceled.                                      |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                   | Fact   | Permission denied logs warn not error and gates hold.                                       |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                            | Fact   | Successful insert logs exactly one info and reads log no info.                              |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                             | Fact   | With gates disabled happy path records no debug info or warn.                               |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                        | Fact   | Failure logs error with exception attached and still propagates.                            |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                             | Fact   | Each call emits one span with outcome tag.                                                  |
| `EachCallRecordsOneDurationMeasurementAsync`                                          | Fact   | Each call records one duration measurement.                                                 |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                       | Fact   | Audit log gets exactly one line including reads.                                            |
| `InsertPublishesExactlyOneCreatedEventAsync`                                          | Fact   | Insert publishes exactly one created event.                                                 |
| `ReadsAndFailuresPublishNothingAsync`                                                 | Fact   | Reads and failures publish nothing.                                                         |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                 | Fact   | Catalogue registers unique pascal case no underscore names.                                 |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                               | Fact   | List async clamps take and paginates deterministically.                                     |

## Etcd cluster event

### EtcdClusterEventServiceTests

`Jube.Test.Service.EtcdClusterEvent.EtcdClusterEventServiceTests` — 18 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByEndpointAscendingAndDescendingAsync`               | Fact   | List sorts by endpoint ascending and descending.                   |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Etcd member status

### EtcdMemberStatusServiceTests

`Jube.Test.Service.EtcdMemberStatus.EtcdMemberStatusServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchOnAlarmsAsync`                             | Fact   | List filters by search on alarms.                                  |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByDbSizeBytesAscendingAndDescendingAsync`            | Fact   | List sorts by db size bytes ascending and descending.              |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForDbSizeBytesAsync`         | Fact   | List statistics computes exact values for db size bytes.           |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Ha proxy reachability probe

### HaProxyReachabilityProbeServiceTests

`Jube.Test.Service.HaProxyReachabilityProbe.HaProxyReachabilityProbeServiceTests` — 17 tests. Category: Service.
Collection: Database.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact | List filters by date range.                                        |
| `ListFiltersBySearchAgainstTargetAsync`                        | Fact | List filters by search against target.                             |
| `ListIsNotTenantScopedAsync`                                   | Fact | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact | List defaults to last hour when from and to omitted.               |
| `ListSortsByConnectMicrosecondsAscendingAndDescendingAsync`    | Fact | List sorts by connect microseconds ascending and descending.       |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForConnectMicrosecondsAsync` | Fact | List statistics computes exact values for connect microseconds.    |
| `ListStatisticsIncludesOnlyConnectMicrosecondsAsync`           | Fact | List statistics includes only connect microseconds.                |
| `ListCapturesAFailedProbeWithNoHttpStatusCodeAsync`            | Fact | List captures a failed probe with no http status code.             |

## Ha proxy server status

### HaProxyServerStatusServiceTests

`Jube.Test.Service.HaProxyServerStatus.HaProxyServerStatusServiceTests` — 17 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact | List filters by date range.                                        |
| `ListFiltersBySearchAgainstPxNameAsync`                        | Fact | List filters by search against px name.                            |
| `ListFiltersBySearchAgainstStatusAsync`                        | Fact | List filters by search against status.                             |
| `ListIsNotTenantScopedAsync`                                   | Fact | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact | List defaults to last hour when from and to omitted.               |
| `ListSortsByChkFailAscendingAndDescendingAsync`                | Fact | List sorts by chk fail ascending and descending.                   |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForChkFailAsync`             | Fact | List statistics computes exact values for chk fail.                |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact | List statistics includes every continuous measured column.         |

## Http processing counter

### HttpProcessingCounterServiceTests

`Jube.Test.Service.HttpProcessingCounter.HttpProcessingCounterServiceTests` — 18 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAsync`                              | Fact   | List returns most recent first.                                    |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithBlankUserNameThrowsNotAuthenticatedAsync`           | Fact   | Create with blank user name throws not authenticated.              |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByAllAscendingAndDescendingAsync`                    | Fact   | List sorts by all ascending and descending.                        |
| `ListSortsByInstanceAscendingAndDescendingAsync`               | Fact   | List sorts by instance ascending and descending.                   |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Model invoke warning

### ModelInvokeWarningServiceTests

`Jube.Test.Service.ModelInvokeWarning.ModelInvokeWarningServiceTests` — 19 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersByEntityAnalysisModelGuidAsync`                    | Fact   | List filters by entity analysis model guid.                        |
| `ListFiltersBySearchAgainstMessageCaseInsensitivelyAsync`      | Fact   | List filters by search against message case insensitively.         |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListSortsByElapsedMicrosecondsAscendingAndDescendingAsync`    | Fact   | List sorts by elapsed microseconds ascending and descending.       |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForElapsedMicrosecondsAsync` | Fact   | List statistics computes exact values for elapsed microseconds.    |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Observability

### OtlpDispatchCountersTests

`Jube.Test.Service.Observability.OtlpDispatchCountersTests` — 3 tests. Category: Unit. Collection: OtlpDispatchCounters.

| Test                                                                | Kind | Verifies                                                                     |
|---------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `RecordDispatchAccumulatesCountSuccessFailureAndTimingForOneSignal` | Fact | Record dispatch accumulates count success failure and timing for one signal. |
| `RecordDroppedIsIndependentOfDispatchCount`                         | Fact | Record dropped is independent of dispatch count.                             |
| `TakeSnapshotResetsCountersButKeepsTheAccumulatorForTheNextCycle`   | Fact | Take snapshot resets counters but keeps the accumulator for the next cycle.  |

## Observability dispatch tracking exporter

### DispatchTrackingExporterTests

`Jube.Test.Service.Observability.DispatchTrackingExporter.DispatchTrackingExporterTests` — 4 tests. Category: Unit.
Collection: OtlpDispatchCounters.

| Test                                                                     | Kind | Verifies                                                                              |
|--------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `ExportReturnsSuccessAndRecordsItWhenInnerExporterSucceeds`              | Fact | Export returns success and records it when inner exporter succeeds.                   |
| `ExportReturnsFailureAndRecordsItWhenInnerExporterReturnsFailure`        | Fact | Export returns failure and records it when inner exporter returns failure.            |
| `ExportNeverThrowsAndIsCountedAsFailureWhenInnerExporterThrows`          | Fact | Export never throws and is counted as failure when inner exporter throws.             |
| `ToStringReturnsTheSignalNameSoSelfDiagnosticsCanAttributeDropsBackToIt` | Fact | To string returns the signal name so self diagnostics can attribute drops back to it. |

## Open telemetry exclude

### OpenTelemetryExcludeServiceTests

`Jube.Test.Service.OpenTelemetryExclude.OpenTelemetryExcludeServiceTests` — 9 tests. Category: Service. Collection:
Database.

| Test                                                   | Kind | Verifies                                               |
|--------------------------------------------------------|------|--------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`              | Fact | Insert persists and returns version one.               |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`      | Fact | Get all returns created row and get by id returns it.  |
| `UpdateChangesFieldsAndIncrementsVersionAsync`         | Fact | Update changes fields and increments version.          |
| `DeleteSoftDeletesRowAsync`                            | Fact | Delete soft deletes row.                               |
| `UpdateOnLockedRowThrowsLockedExceptionAsync`          | Fact | Update on locked row throws locked exception.          |
| `DeleteOnLockedRowThrowsLockedExceptionAsync`          | Fact | Delete on locked row throws locked exception.          |
| `InsertWithEmptyNameIsRejectedAsync`                   | Fact | Insert with empty name is rejected.                    |
| `InsertByUserWithoutPermissionThrowsForbiddenAsync`    | Fact | Insert by user without permission throws forbidden.    |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync` | Fact | Non landlord holding every specification is forbidden. |

## Open telemetry log counter

### OpenTelemetryLogCounterServiceTests

`Jube.Test.Service.OpenTelemetryLogCounter.OpenTelemetryLogCounterServiceTests` — 12 tests. Category: Service.
Collection: Database.

| Test                                                   | Kind | Verifies                                               |
|--------------------------------------------------------|------|--------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`              | Fact | Insert persists and returns version one.               |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`      | Fact | Get all returns created row and get by id returns it.  |
| `UpdateChangesFieldsAndIncrementsVersionAsync`         | Fact | Update changes fields and increments version.          |
| `DeleteSoftDeletesRowAsync`                            | Fact | Delete soft deletes row.                               |
| `DeleteOnMissingIdThrowsNotFoundAsync`                 | Fact | Delete on missing id throws not found.                 |
| `UpdateOnLockedRowThrowsLockedExceptionAsync`          | Fact | Update on locked row throws locked exception.          |
| `DeleteOnLockedRowThrowsLockedExceptionAsync`          | Fact | Delete on locked row throws locked exception.          |
| `InsertWithEmptyNameIsRejectedAsync`                   | Fact | Insert with empty name is rejected.                    |
| `InsertWithInvalidRegexIsRejectedAsync`                | Fact | Insert with invalid regex is rejected.                 |
| `InsertByUnauthenticatedUserThrowsAsync`               | Fact | Insert by unauthenticated user throws.                 |
| `InsertByUserWithoutPermissionThrowsForbiddenAsync`    | Fact | Insert by user without permission throws forbidden.    |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync` | Fact | Non landlord holding every specification is forbidden. |

## Open telemetry metric

### OpenTelemetryMetricServiceTests

`Jube.Test.Service.OpenTelemetryMetric.OpenTelemetryMetricServiceTests` — 21 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListFiltersByTagsAsync`                                       | Fact   | List filters by tags.                                              |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListClampsSamplePercentageToZeroToOneHundredRangeAsync`       | Fact   | List clamps sample percentage to zero to one hundred range.        |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsBySumAscendingAndDescendingAsync`                    | Fact   | List sorts by sum ascending and descending.                        |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForSumAsync`                 | Fact   | List statistics computes exact values for sum.                     |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Otlp dispatch counter

### OtlpDispatchCounterServiceTests

`Jube.Test.Service.OtlpDispatchCounter.OtlpDispatchCounterServiceTests` — 13 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `ListFiltersBySignalIdAsync`                                   | Fact   | List filters by signal id.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByCountAscendingAndDescendingAsync`                  | Fact   | List sorts by count ascending and descending.                      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Overlay network task drift

### OverlayNetworkTaskDriftServiceTests

`Jube.Test.Service.OverlayNetworkTaskDrift.OverlayNetworkTaskDriftServiceTests` — 15 tests. Category: Service.
Collection: Database.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact | List filters by date range.                                        |
| `ListIsNotTenantScopedAsync`                                   | Fact | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact | List defaults to last hour when from and to omitted.               |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsAlwaysEmptyForThisDtoAsync`                   | Fact | List statistics is always empty for this dto.                      |
| `ListCapturesAnInconsistentRowWithItsDriftedAddressesAsync`    | Fact | List captures an inconsistent row with its drifted addresses.      |
| `ListCapturesAConsistentRowWithEmptyDriftedAddressesAsync`     | Fact | List captures a consistent row with empty drifted addresses.       |

## Patroni cluster event

### PatroniClusterEventServiceTests

`Jube.Test.Service.PatroniClusterEvent.PatroniClusterEventServiceTests` — 18 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchOnReasonAsync`                             | Fact   | List filters by search on reason.                                  |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByNameAscendingAndDescendingAsync`                   | Fact   | List sorts by name ascending and descending.                       |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Patroni member status

### PatroniMemberStatusServiceTests

`Jube.Test.Service.PatroniMemberStatus.PatroniMemberStatusServiceTests` — 19 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByLagBytesAscendingAndDescendingAsync`               | Fact   | List sorts by lag bytes ascending and descending.                  |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForLagBytesAsync`            | Fact   | List statistics computes exact values for lag bytes.               |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Postgres activity

### PostgresActivityServiceApplySortTests

`Jube.Test.Service.PostgresActivity.PostgresActivityServiceApplySortTests` — 4 tests. Category: Unit.

| Test                                                            | Kind | Verifies                                                                |
|-----------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `UnrecognisedSortFieldReturnsRowsInTheirOriginalOrderUnchanged` | Fact | Unrecognised sort field returns rows in their original order unchanged. |
| `NullSortFieldReturnsRowsInTheirOriginalOrderUnchanged`         | Fact | Null sort field returns rows in their original order unchanged.         |
| `RecognisedSortFieldDefaultsToDescendingOrder`                  | Fact | Recognised sort field defaults to descending order.                     |
| `RecognisedSortFieldWithAscSortsAscending`                      | Fact | Recognised sort field with asc sorts ascending.                         |

### PostgresActivityServiceTests

`Jube.Test.Service.PostgresActivity.PostgresActivityServiceTests` — 10 tests. Category: Service. Collection: Database.

| Test                                                       | Kind | Verifies                                                   |
|------------------------------------------------------------|------|------------------------------------------------------------|
| `ListReturnsAtLeastOneLiveBackendAsync`                    | Fact | List returns at least one live backend.                    |
| `ListFiltersBySearchToNothingWhenNoMatchAsync`             | Fact | List filters by search to nothing when no match.           |
| `ListWithoutPermissionThrowsForbiddenAsync`                | Fact | List without permission throws forbidden.                  |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`     | Fact | Non landlord holding every specification is forbidden.     |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`        | Fact | Create with null user name throws not authenticated.       |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`       | Fact | Create with unknown tenant throws not authenticated.       |
| `ListIsNotTenantScopedAsync`                               | Fact | List is not tenant scoped.                                 |
| `ListSortsByPidAscendingAndDescendingAsync`                | Fact | List sorts by pid ascending and descending.                |
| `ListTotalReflectsRowCountAsync`                           | Fact | List total reflects row count.                             |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync` | Fact | List statistics includes every continuous measured column. |

## Postgres index statistics

### PostgresIndexStatisticsServiceTests

`Jube.Test.Service.PostgresIndexStatistics.PostgresIndexStatisticsServiceTests` — 11 tests. Category: Service.
Collection: Database.

| Test                                                       | Kind   | Verifies                                                   |
|------------------------------------------------------------|--------|------------------------------------------------------------|
| `ListReturnsRealIndexesFromTheSchemaAsync`                 | Fact   | List returns real indexes from the schema.                 |
| `ListWithoutPermissionThrowsForbiddenAsync`                | Fact   | List without permission throws forbidden.                  |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`     | Fact   | Non landlord holding every specification is forbidden.     |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`        | Fact   | Create with null user name throws not authenticated.       |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`       | Fact   | Create with unknown tenant throws not authenticated.       |
| `ListIsNotTenantScopedAsync`                               | Fact   | List is not tenant scoped.                                 |
| `ListSortsByIndexSizeBytesAscendingAndDescendingAsync`     | Fact   | List sorts by index size bytes ascending and descending.   |
| `ListWithUnrecognisedSortFieldPreservesQueryOrderAsync`    | Fact   | List with unrecognised sort field preserves query order.   |
| `ListTotalReflectsRowCountAsync`                           | Fact   | List total reflects row count.                             |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync` | Fact   | List statistics includes every continuous measured column. |
| `ListIgnoresInjectionInSortFieldAndSortDirectionAsync`     | Theory | List ignores injection in sort field and sort direction.   |

## Postgres log entry

### PostgresLogEntryServiceTests

`Jube.Test.Service.PostgresLogEntry.PostgresLogEntryServiceTests` — 18 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsAllFieldsAsync`          | Fact   | List returns most recent first and projects all fields.            |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByLevelAscendingAndDescendingAsync`                  | Fact   | List sorts by level ascending and descending.                      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Postgres metric

### PostgresMetricServiceTests

`Jube.Test.Service.PostgresMetric.PostgresMetricServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByActiveConnectionsAscendingAndDescendingAsync`      | Fact   | List sorts by active connections ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForActiveConnectionsAsync`   | Fact   | List statistics computes exact values for active connections.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Postgres replication status

### PostgresReplicationStatusServiceTests

`Jube.Test.Service.PostgresReplicationStatus.PostgresReplicationStatusServiceTests` — 19 tests. Category: Service.
Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByReplayLagSecondsAscendingAndDescendingAsync`       | Fact   | List sorts by replay lag seconds ascending and descending.         |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForReplayLagSecondsAsync`    | Fact   | List statistics computes exact values for replay lag seconds.      |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Postgres statement statistics

### PostgresStatementStatisticsServiceApplySortTests

`Jube.Test.Service.PostgresStatementStatistics.PostgresStatementStatisticsServiceApplySortTests` — 5 tests. Category:
Unit.

| Test                                                            | Kind | Verifies                                                                |
|-----------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `UnrecognisedSortFieldReturnsRowsInTheirOriginalOrderUnchanged` | Fact | Unrecognised sort field returns rows in their original order unchanged. |
| `NullSortFieldReturnsRowsInTheirOriginalOrderUnchanged`         | Fact | Null sort field returns rows in their original order unchanged.         |
| `RecognisedSortFieldDefaultsToDescendingOrder`                  | Fact | Recognised sort field defaults to descending order.                     |
| `RecognisedSortFieldWithAscSortsAscending`                      | Fact | Recognised sort field with asc sorts ascending.                         |
| `SortsByMeanExecTimeMillisecondsAscendingAndDescending`         | Fact | Sorts by mean exec time milliseconds ascending and descending.          |

### PostgresStatementStatisticsServiceTests

`Jube.Test.Service.PostgresStatementStatistics.PostgresStatementStatisticsServiceTests` — 9 tests. Category: Service.
Collection: Database.

| Test                                                       | Kind | Verifies                                                   |
|------------------------------------------------------------|------|------------------------------------------------------------|
| `ListWithoutPermissionThrowsForbiddenAsync`                | Fact | List without permission throws forbidden.                  |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`     | Fact | Non landlord holding every specification is forbidden.     |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`        | Fact | Create with null user name throws not authenticated.       |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`       | Fact | Create with unknown tenant throws not authenticated.       |
| `ListTotalReflectsRowCountAsync`                           | Fact | List total reflects row count.                             |
| `ListIsNotTenantScopedAsync`                               | Fact | List is not tenant scoped.                                 |
| `ListFiltersBySearchToNothingWhenNoMatchAsync`             | Fact | List filters by search to nothing when no match.           |
| `ListSortsByCallsAscendingAndDescendingAsync`              | Fact | List sorts by calls ascending and descending.              |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync` | Fact | List statistics includes every continuous measured column. |

## Postgres table statistics

### PostgresTableStatisticsServiceTests

`Jube.Test.Service.PostgresTableStatistics.PostgresTableStatisticsServiceTests` — 11 tests. Category: Service.
Collection: Database.

| Test                                                       | Kind   | Verifies                                                   |
|------------------------------------------------------------|--------|------------------------------------------------------------|
| `ListReturnsRealTablesFromTheSchemaAsync`                  | Fact   | List returns real tables from the schema.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                | Fact   | List without permission throws forbidden.                  |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`     | Fact   | Non landlord holding every specification is forbidden.     |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`        | Fact   | Create with null user name throws not authenticated.       |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`       | Fact   | Create with unknown tenant throws not authenticated.       |
| `ListIsNotTenantScopedAsync`                               | Fact   | List is not tenant scoped.                                 |
| `ListSortsByTotalSizeBytesAscendingAndDescendingAsync`     | Fact   | List sorts by total size bytes ascending and descending.   |
| `ListWithUnrecognisedSortFieldPreservesQueryOrderAsync`    | Fact   | List with unrecognised sort field preserves query order.   |
| `ListTotalReflectsRowCountAsync`                           | Fact   | List total reflects row count.                             |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync` | Fact   | List statistics includes every continuous measured column. |
| `ListIgnoresInjectionInSortFieldAndSortDirectionAsync`     | Theory | List ignores injection in sort field and sort direction.   |

## Redis call counter

### RedisCallCounterServiceTests

`Jube.Test.Service.RedisCallCounter.RedisCallCounterServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListSortsByCountAscendingAndDescendingAsync`                  | Fact   | List sorts by count ascending and descending.                      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForCountAsync`               | Fact   | List statistics computes exact values for count.                   |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Redis connection event

### RedisConnectionEventServiceTests

`Jube.Test.Service.RedisConnectionEvent.RedisConnectionEventServiceTests` — 19 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByRetryCountAscendingAndDescendingAsync`             | Fact   | List sorts by retry count ascending and descending.                |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForRetryCountAsync`          | Fact   | List statistics computes exact values for retry count.             |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Redis connection multiplexer metric

### RedisConnectionMultiplexerMetricServiceTests

`Jube.Test.Service.RedisConnectionMultiplexerMetric.RedisConnectionMultiplexerMetricServiceTests` — 19 tests. Category:
Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByTotalOutstandingAscendingAndDescendingAsync`       | Fact   | List sorts by total outstanding ascending and descending.          |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForTotalOutstandingAsync`    | Fact   | List statistics computes exact values for total outstanding.       |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`     | Fact   | List statistics includes every continuous measured column.         |

## Redis key activity

### RedisKeyActivityRepositoryTests

`Jube.Test.Service.RedisKeyActivity.RedisKeyActivityRepositoryTests` — 2 tests. Category: Service.

| Test                                                                               | Kind | Verifies                                                                                  |
|------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `SampleAsyncReadsIdleTimeAndLeavesFrequencyNullUnderTheDefaultEvictionPolicyAsync` | Fact | Sample async reads idle time and leaves frequency null under the default eviction policy. |
| `SampleAsyncNeverReturnsMoreThanTheFixedCapAsync`                                  | Fact | Sample async never returns more than the fixed cap.                                       |

## Redis metric

### RedisMetricServiceTests

`Jube.Test.Service.RedisMetric.RedisMetricServiceTests` — 22 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                        |
|--------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`                       | Fact   | List returns most recent first and projects fields.                             |
| `ListClampsTakeToOneHundredThousandAsync`                                | Fact   | List clamps take to one hundred thousand.                                       |
| `ListWithoutPermissionThrowsForbiddenAsync`                              | Fact   | List without permission throws forbidden.                                       |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`                   | Fact   | Non landlord holding every specification is forbidden.                          |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                      | Fact   | Create with null user name throws not authenticated.                            |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with unknown tenant throws not authenticated.                            |
| `ListFiltersByDateRangeAsync`                                            | Fact   | List filters by date range.                                                     |
| `ListFiltersBySearchAsync`                                               | Fact   | List filters by search.                                                         |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`              | Fact   | List with zero sample percentage excludes every matching row.                   |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`           | Fact   | List with hundred sample percentage includes every matching row.                |
| `ListIsNotTenantScopedAsync`                                             | Fact   | List is not tenant scoped.                                                      |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`                        | Fact   | List defaults to last hour when from and to omitted.                            |
| `ListSortsByConnectedClientsAscendingAndDescendingAsync`                 | Fact   | List sorts by connected clients ascending and descending.                       |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`                    | Theory | List sort direction is case insensitive for ascending.                          |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`                 | Fact   | List with garbage sort direction falls back to descending.                      |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`           | Fact   | List with unrecognised sort field falls back to most recent first.              |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`           | Fact   | List total reflects full filtered count even when take is smaller.              |
| `ListStatisticsComputesExactValuesForConnectedClientsAsync`              | Fact   | List statistics computes exact values for connected clients.                    |
| `ListProjectsLastAofRewriteAndLastBgSaveDatesAsync`                      | Fact   | List projects last aof rewrite and last bg save dates.                          |
| `ListProjectsNullLastAofRewriteAndLastBgSaveDatesWhenNeverObservedAsync` | Fact   | List projects null last aof rewrite and last bg save dates when never observed. |
| `ListSortsByLastBgSaveDateAscendingAndDescendingAsync`                   | Fact   | List sorts by last bg save date ascending and descending.                       |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`               | Fact   | List statistics includes every continuous measured column.                      |

## Redis sentinel event

### RedisSentinelEventServiceTests

`Jube.Test.Service.RedisSentinelEvent.RedisSentinelEventServiceTests` — 18 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`         | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                   | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`              | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByChannelAscendingAndDescendingAsync`                | Fact   | List sorts by channel ascending and descending.                    |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## Redis sentinel status

### RedisSentinelStatusServiceTests

`Jube.Test.Service.RedisSentinelStatus.RedisSentinelStatusServiceTests` — 20 tests. Category: Service. Collection:
Database.

| Test                                                             | Kind   | Verifies                                                           |
|------------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`               | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                        | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                      | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`           | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`              | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`             | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                    | Fact   | List filters by date range.                                        |
| `ListProjectsReplicationLagSecondsOnlyForSlaveRowsAsync`         | Fact   | List projects replication lag seconds only for slave rows.         |
| `ListFiltersBySearchAsync`                                       | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`      | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`   | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsNotTenantScopedAsync`                                     | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`                | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByReplicationLagSecondsAscendingAndDescendingAsync`    | Fact   | List sorts by replication lag seconds ascending and descending.    |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`            | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`         | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`   | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`   | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForReplicationLagSecondsAsync` | Fact   | List statistics computes exact values for replication lag seconds. |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`       | Fact   | List statistics includes every continuous measured column.         |

## Redis slow operation

### RedisSlowOperationServiceTests

`Jube.Test.Service.RedisSlowOperation.RedisSlowOperationServiceTests` — 21 tests. Category: Service. Collection:
Database.

| Test                                                            | Kind   | Verifies                                                           |
|-----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`              | Fact   | List returns most recent first and projects fields.                |
| `ListClampsTakeToOneHundredThousandAsync`                       | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                     | Fact   | List without permission throws forbidden.                          |
| `NonLandlordHoldingEverySpecificationIsForbiddenAsync`          | Fact   | Non landlord holding every specification is forbidden.             |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`             | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`            | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                   | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                      | Fact   | List filters by search.                                            |
| `ListProjectsAndFiltersByCommandNameAndKeyNameAsync`            | Fact   | List projects and filters by command name and key name.            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`     | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync`  | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListClampsSamplePercentageToZeroToOneHundredRangeAsync`        | Fact   | List clamps sample percentage to zero to one hundred range.        |
| `ListIsNotTenantScopedAsync`                                    | Fact   | List is not tenant scoped.                                         |
| `ListDefaultsToLastHourWhenFromAndToOmittedAsync`               | Fact   | List defaults to last hour when from and to omitted.               |
| `ListSortsByDurationMicrosecondsAscendingAndDescendingAsync`    | Fact   | List sorts by duration microseconds ascending and descending.      |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`           | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`        | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync`  | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync`  | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsComputesExactValuesForDurationMicrosecondsAsync` | Fact   | List statistics computes exact values for duration microseconds.   |
| `ListStatisticsIncludesEveryContinuousMeasuredColumnAsync`      | Fact   | List statistics includes every continuous measured column.         |
