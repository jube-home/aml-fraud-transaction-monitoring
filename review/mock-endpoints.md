# mock-endpoints

## What changed

- New DynamicEnvironment setting `EnableMockMfaEndpoint`, default False. `/api/Mfa` is mapped in `Startup.Configure`
  only when `EnableMockEndpoints` is not False AND `EnableMockMfaEndpoint` is True. `/api/MockHttpAdaptation/*`
  unchanged (default True stands).
- Production-shaped compose files set `EnableMockEndpoints=False` on jube-ui, jube-api and jube-jobs. Root
  `docker-compose.yml` and `docker-compose.redis.yml` (quickstart) and `.github/zap/docker-compose.zap.yml` (already
  False) untouched. No helm, k8s, swarm stack files exist; Jube.App Dockerfile has no env defaults.

## Files

- Jube.DynamicEnvironment/DynamicEnvironment.cs (4 lines, hot file, re-read first)
- Jube.App/Startup.cs (nested gate around MapMockRsaMfaEndpoints)
- Jube.Cluster/docker-compose.yml, Jube.Cluster/docker-compose.redis.yml (3 lines each)
- Jube.Tests/Security/Surface/PenTestMockEndpointsGate.cs
- docs: Concepts/EnvironmentVariables (EnableMockEndpoints row edited, new EnableMockMfaEndpoint row,
  MultifactorAuthenticationEndpoint row, OTP corrected 123456 to 12345678), Concepts/Authentication,
  Configuration/Models/HTTPAdaptationProtocol (5.4), GettingStarted/DeployingWithDocker,
  GettingStarted/DeployingWithJubeCluster/DeploymentRunbook (new "Production hardening: mock endpoints"),
  Testing/SecuritySurfaceAndPenTest.md (gate table 2 to 4 tests)

## Usages of the mock MFA checked

Jube.Tests Mocks goldens (MockEndpointsGoldenTests, MocksCases), AuthenticationRsaMfaTests,
AuthenticationRsaMfaLockoutTests all call `MapMockRsaMfaEndpoints` directly on their own host, not via Startup, so they
are unaffected. No e2e spec, ZAP/CI compose, seeded migration or quickstart compose enables MFA or calls /api/Mfa.
LoadTest Mock.json not MFA related (grep hit only).

## Tests changed

- `TheFlagDefaultsOn_SoADeploymentKeepsTheDocumentedFixturesAsync`: old, asserts /api/MockHttpAdaptation 200 only; new,
  also asserts POST /api/Mfa is 404 by default.
- `WithTheFlagOff_NoMockRouteIsMapped...`: unchanged (already includes /api/Mfa in 404 routes).
- Added `WithBothFlagsOn_TheMockMfaRouteIsMappedAsync` and
  `WithTheMfaFlagOnButTheMasterFlagOff_TheMockMfaRouteIsNotMappedAsync`.

## Tests to run

`dotnet test --filter FullyQualifiedName~PenTestMockEndpointsGate`, then Category=PenTest Surface, Jube.Tests/Mocks,
AuthenticationRsaMfa*.

## Locale

No new SPA or resx keys, so no locale proposals file; fr and the rest of the set are not affected.

## Risks

- Cluster seeded example model's adaptation targets a mock route that is now unmapped: it logs failures in the engine
  (same as the ZAP stack). Documented in the runbook.
- Anyone enabling MFA with the default endpoint now needs EnableMockMfaEndpoint=True (documented).
- Not built or run.
