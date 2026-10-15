---
layout: default
title: Service Authentication and Users
nav_order: 17
parent: Testing
---

# Service Authentication and Users

Tests of authentication, login and logout, role grants and the user-facing security services.

## Authentication

### AuthenticationAccountStateTests

`Jube.Test.Service.Authentication.AuthenticationAccountStateTests` — 26 tests. Category: Service. Collection: Database.

| Test                                                                                               | Kind   | Verifies                                                                                                 |
|----------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------|
| `DeletedUser_IsIndistinguishableFromUnknown_AndDoesNoHashWorkAsync`                                | Fact   | Deleted user is indistinguishable from unknown and does no hash work.                                    |
| `NotActiveUser_IsUnauthorised_FailureType2_WithoutHashWorkAsync`                                   | Theory | Not active user is unauthorised failure type2 without hash work.                                         |
| `NullActiveFlag_IsTreatedAsInactiveAsync`                                                          | Fact   | Null active flag is treated as inactive.                                                                 |
| `LockedUser_CannotLogIn_EvenWithTheCorrectPassword_FailureType3Async`                              | Fact   | Locked user cannot log in even with the correct password failure type3.                                  |
| `NullPasswordLockedFlag_IsTreatedAsUnlockedAsync`                                                  | Fact   | Null password locked flag is treated as unlocked.                                                        |
| `ExpiredPassword_WithTheCorrectPassword_IsForbidden_AndNotRecordedInTheLoginHistoryAsync`          | Fact   | Expired password with the correct password is forbidden and not recorded in the login history.           |
| `NullPasswordExpiry_WithTheCorrectPassword_IsForbiddenAsync`                                       | Fact   | Null password expiry with the correct password is forbidden.                                             |
| `PasswordExpiryBoundary_IsInclusive_OnTheInjectedClockAsync`                                       | Fact   | Password expiry boundary is inclusive on the injected clock.                                             |
| `ExpiredPassword_WithAWrongPassword_IsJustBadCredentials_NotForbiddenAsync`                        | Fact   | Expired password with a wrong password is just bad credentials not forbidden.                            |
| `ASuccessfulLogin_DoesNotChangeAnyOtherUserAsync`                                                  | Fact   | A successful login does not change any other user.                                                       |
| `UserWithNoTenantMembership_CanStillAuthenticateAsync`                                             | Fact   | User with no tenant membership can still authenticate.                                                   |
| `UserInBothTenants_AuthenticatesOnceAsync`                                                         | Fact   | User in both tenants authenticates once.                                                                 |
| `UserOfTenantB_AuthenticatesWithoutTenantContextAsync`                                             | Fact   | User of tenant b authenticates without tenant context.                                                   |
| `UserOfADisabledTenant_CanStillAuthenticateAsync`                                                  | Theory | User of a disabled tenant can still authenticate.                                                        |
| `UserWhoseRoleIsDeleted_CanStillAuthenticateAsync`                                                 | Fact   | User whose role is deleted can still authenticate.                                                       |
| `ACorruptOrMissingStoredHash_NeverAuthenticates_UnderTheRealArgon2SchemeAsync`                     | Theory | A corrupt or missing stored hash never authenticates under the real argon2 scheme.                       |
| `TwoUsersWithDifferentPasswords_CannotUseEachOthersPasswordAsync`                                  | Fact   | Two users with different passwords cannot use each others password.                                      |
| `EveryNonSuccessReason_ProducesTheSameOutcomeShape_ForAUserWithAnExpiryAsync`                      | Theory | Every non success reason produces the same outcome shape for a user with an expiry.                      |
| `EveryBranchDoesExactlyOneHashVerification_SoTimingDoesNotEnumerateUsersAsync`                     | Theory | Every branch does exactly one hash verification so timing does not enumerate users.                      |
| `AnEmptyPasswordForARealAccount_StillDoesOneDummyHashVerification_CR09Async`                       | Theory | An empty password for a real account still does one dummy hash verification CR 09.                       |
| `TheDummyVerification_CanNeverAuthenticateEvenWithTheDummyPasswordItselfAsync`                     | Theory | The dummy verification can never authenticate even with the dummy password itself.                       |
| `TheWireResponsesAndHistoryShapeStayIdentical_AcrossTheBranchesAsync`                              | Fact   | The wire responses and history shape stay identical across the branches.                                 |
| `Weakness_AWrongPasswordForAUserWithoutExpiry_IsForbiddenNotUnauthorised_AnEnumerationOracleAsync` | Fact   | Weakness a wrong password for a user without expiry is forbidden not unauthorised an enumeration oracle. |
| `Weakness_AUserWithoutPasswordCreatedDate_IsAlsoAnOracleAsync`                                     | Fact   | Weakness a user without password created date is also an oracle.                                         |
| `Weakness_TheCorrectPasswordForAnExpiredUser_IsConfirmedByA403Async`                               | Fact   | Weakness the correct password for an expired user is confirmed by a 403.                                 |
| `UnknownUser_AndWrongPassword_BothWriteOneFailedHistoryRow_WithDifferentInternalFailureTypesAsync` | Fact   | Unknown user and wrong password both write one failed history row with different internal failure types. |

### AuthenticationCookieTests

`Jube.Test.Service.Authentication.AuthenticationCookieTests` — 24 tests. Category: Service.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `TheCookieFlags_FollowSessionAndSecureSettings`                               | Theory | The cookie flags follow session and secure settings.                                |
| `StrictCookieFalse_IssuesLaxWhenSecure`                                       | Fact   | Strict cookie false issues lax when secure.                                         |
| `StrictCookieDefault_IssuesStrictWhenSecure`                                  | Fact   | Strict cookie default issues strict when secure.                                    |
| `StrictCookieFalse_DeletesWithLaxWhenSecure`                                  | Fact   | Strict cookie false deletes with lax when secure.                                   |
| `TheDefaultEnvironment_IsOneSessionCookie_NotSecure_AndHttpOnly`              | Fact   | The default environment is one session cookie not secure and http only.             |
| `TheCookieValue_IsTheToken_AndTheExpiryTravelsInAHeaderNotACookie`            | Fact   | The cookie value is the token and the expiry travels in a header not a cookie.      |
| `TheJsonBody_HasExactlyTokenAndExpiration`                                    | Fact   | The json body has exactly token and expiration.                                     |
| `TheIssuedToken_IsAcceptedByTheRealHandler_AsTheUser_ViaBearerAndCookieAsync` | Fact   | The issued token is accepted by the real handler as the user via bearer and cookie. |
| `TheTokenHeaderAndPayload_CarryOnlyTheNameIssuerAudienceAndExpiry`            | Fact   | The token header and payload carry only the name issuer audience and expiry.        |
| `Weakness_TheSameSecond_YieldsTheIdenticalToken_ThereIsNoJtiOrIssuedAt`       | Fact   | Weakness the same second yields the identical token there is no jti or issued at.   |
| `NamesSurviveTheTokenAndTheCookieRoundTrip_ExactlyAsync`                      | Theory | Names survive the token and the cookie round trip exactly.                          |
| `ATokenIssuedForAnEmptyName_IsRejectedByTheHandlerAsMalformedAsync`           | Fact   | A token issued for an empty name is rejected by the handler as malformed.           |
| `AJwtKeyBelow128Bits_CannotSign_SoLoginWouldFailAt500`                        | Theory | A jwt key below128 bits cannot sign so login would fail at500.                      |
| `ATamperedToken_IsRejectedAsync`                                              | Theory | A tampered token is rejected.                                                       |
| `AStructurallyMalformedToken_IsSimplyNotAuthenticated_NeverAnExceptionAsync`  | Theory | A structurally malformed token is simply not authenticated never an exception.      |
| `ATokenSignedWithADifferentKey_IsRejectedAsync`                               | Fact   | A token signed with a different key is rejected.                                    |
| `ATokenForAnotherIssuerOrAudience_IsRejectedAsync`                            | Theory | A token for another issuer or audience is rejected.                                 |
| `ExpiryIsEnforced_With30SecondsOfClockSkewAsync`                              | Theory | Expiry is enforced with30 seconds of clock skew.                                    |
| `AnExpiredTokenInTheCookie_IsRejectedTooAsync`                                | Fact   | An expired token in the cookie is rejected too.                                     |
| `AnEmptyCookie_IsNoCredentialsAtAll_NotAFailureAsync`                         | Fact   | An empty cookie is no credentials at all not a failure.                             |
| `AnAlteredExpiryHeader_DoesNotExtendTheTokensLifeAsync`                       | Fact   | An altered expiry header does not extend the tokens life.                           |
| `TheJwtSecurityTokenHandler_ReadsTheSameClaimsThatWereIssued`                 | Fact   | The jwt security token handler reads the same claims that were issued.              |
| `AnApiKeyAuthenticatedRequest_IsNeverIssuedARefreshedSessionCookieAsync`      | Fact   | An api key authenticated request is never issued a refreshed session cookie.        |
| `AJwtCookieAuthenticatedRequest_StillGetsARefreshedSessionCookieAsync`        | Fact   | A jwt cookie authenticated request still gets a refreshed session cookie.           |

### AuthenticationEndpointTests

`Jube.Test.Service.Authentication.AuthenticationEndpointTests` — 50 tests. Category: Service. Collection: Database.

| Test                                                                                     | Kind   | Verifies                                                                                       |
|------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `ExactlyTheFiveLegacyRoutesAndLogout_AreMapped_WithTheirVerbsAsync`                      | Fact   | Exactly the five legacy routes and logout are mapped with their verbs.                         |
| `AnonymousEndpointsStayAnonymous_AndTheOthersStayProtectedAsync`                         | Theory | Anonymous endpoints stay anonymous and the others stay protected.                              |
| `TheNegotiateRoutes_AreBoundToTheNegotiateSchemeOnlyAsync`                               | Theory | The negotiate routes are bound to the negotiate scheme only.                                   |
| `ChangePassword_UsesTheDefaultSchemeAuthorisationAsync`                                  | Fact   | Change password uses the default scheme authorisation.                                         |
| `NothingInTheAuthenticationAreaIsExposedToTheAgentToolCatalogue`                         | Fact   | Nothing in the authentication area is exposed to the agent tool catalogue.                     |
| `WrongVerbs_Are405_AndReachNoLogicAsync`                                                 | Theory | Wrong verbs are405 and reach no logic.                                                         |
| `UnknownRoutes_Are404Async`                                                              | Theory | Unknown routes are404.                                                                         |
| `RoutingIsCaseInsensitive_AsTheLegacyControllerRoutesWereAsync`                          | Fact   | Routing is case insensitive as the legacy controller routes were.                              |
| `ACorrectLogin_Is200_WithATokenBodyAndExactlyOneHttpOnlyCookieAsync`                     | Fact   | A correct login is200 with a token body and exactly one http only cookie.                      |
| `TheIssuedToken_AuthenticatesLaterRequests_ViaBearerAndViaCookieAsync`                   | Fact   | The issued token authenticates later requests via bearer and via cookie.                       |
| `ARejectedToken_CannotReachChangePasswordAsync`                                          | Theory | A rejected token cannot reach change password.                                                 |
| `AMalformedBearerToken_Is401OnProtectedRoutes_AndAnonymousRoutesProceedAsAnonymousAsync` | Theory | A malformed bearer token is401 on protected routes and anonymous routes proceed as anonymous.  |
| `AMalformedCookieToken_IsTreatedTheSameAsync`                                            | Theory | A malformed cookie token is treated the same.                                                  |
| `AnUnacceptableToken_NeverBreaksAnAnonymousRoute_AndLeaksNoExceptionDetailAsync`         | Theory | An unacceptable token never breaks an anonymous route and leaks no exception detail.           |
| `NoCredentialsAtAll_On_ChangePassword_Is401WithAnEmptyBodyAsync`                         | Fact   | No credentials at all on change password is401 with an empty body.                             |
| `EveryFailedLoginReason_LooksIdenticalOnTheWireAsync`                                    | Theory | Every failed login reason looks identical on the wire.                                         |
| `Weakness_TheWireDiffersForAUserWithoutAPasswordExpiry_403VersusTheUsual401Async`        | Fact   | Weakness the wire differs for a user without a password expiry 403 versus the usual401.        |
| `AnExpiredPassword_IsA403_WithoutCookiesAsync`                                           | Fact   | An expired password is a 403 without cookies.                                                  |
| `ANewStrongPassword_WithTheOldOne_Is200_AndTheNewOneWorksAsync`                          | Fact   | A new strong password with the old one is200 and the new one works.                            |
| `AWeakNewPassword_Is400_WithTheErrorsArrayTheUiReadsAsync`                               | Fact   | A weak new password is400 with the errors array the ui reads.                                  |
| `ValidationFailures_Are400_WithABodyMatchingTheLegacyNewtonsoftShapeAsync`               | Theory | Validation failures are400 with a body matching the legacy newtonsoft shape.                   |
| `TheSuccessBody_MatchesTheLegacyNewtonsoftShapeAsync`                                    | Fact   | The success body matches the legacy newtonsoft shape.                                          |
| `WhenAnotherSchemeIsActive_LoginAndTheWireProbe_Are404Async`                             | Theory | When another scheme is active login and the wire probe are404.                                 |
| `AnAlreadyAuthenticatedCaller_GetsATokenForTheCredentialsSubmitted_NotForTheCallerAsync` | Fact   | An already authenticated caller gets a token for the credentials submitted not for the caller. |
| `ParallelLoginsOverHttp_AllGetTheirAnswers_AndWrongOnesNeverSucceedAsync`                | Fact   | Parallel logins over http all get their answers and wrong ones never succeed.                  |
| `WirePasswordHash_Is200_WithTheFlagInAJsonBodyAsync`                                     | Theory | Wire password hash is200 with the flag in a json body.                                         |
| `WirePasswordHash_UnknownAndMissingNamesAsync`                                           | Theory | Wire password hash unknown and missing names.                                                  |
| `ChangePassword_WithAValidToken_Is200WithAnEmptyBody_AndChangesTheHashAsync`             | Fact   | Change password with a valid token is200 with an empty body and changes the hash.              |
| `ChangePassword_WithTheWrongCurrentPassword_Is401Async`                                  | Fact   | Change password with the wrong current password is401.                                         |
| `ChangePassword_WeakNew_Is400_WithTheErrorsArrayAsync`                                   | Fact   | Change password weak new is400 with the errors array.                                          |
| `ChangePassword_IsForbidden_WhenNegotiateOwnsIdentityAsync`                              | Fact   | Change password is forbidden when negotiate owns identity.                                     |
| `ChangePassword_ForAValidTokenWhoseUserNoLongerExists_Is401WithNoDetailAsync`            | Fact   | Change password for a valid token whose user no longer exists is401 with no detail.            |
| `TheLoginHistoryRemoteIp_IsTheConnectionAddress_NeverTheBodyOrAForwardedHeaderAsync`     | Fact   | The login history remote ip is the connection address never the body or a forwarded header.    |
| `MalformedJsonOrWrongShapes_Are400_AndTouchNothingAsync`                                 | Theory | Malformed json or wrong shapes are400 and touch nothing.                                       |
| `AnEmptyOrNullBody_Is400_WithNoDetail_AndTouchesNothingAsync`                            | Theory | An empty or null body is400 with no detail and touches nothing.                                |
| `ChangePassword_WithNoRecognisedProperties_Is400_NotA500Async`                           | Fact   | Change password with no recognised properties is400 not a 500.                                 |
| `WirePasswordHash_IgnoresUnrecognisedProperties_AndAnswersFalseForNoUserNameAsync`       | Fact   | Wire password hash ignores unrecognised properties and answers false for no user name.         |
| `ANonJsonContentType_Is415_AndTouchesNothingAsync`                                       | Theory | A non json content type is415 and touches nothing.                                             |
| `APostWithNoBodyAtAll_Is400_WithoutTouchingTheDatabaseAsync`                             | Fact   | A post with no body at all is400 without touching the database.                                |
| `UnknownAndExtraJsonProperties_AreIgnored_AndCaseInsensitiveNamesBindAsync`              | Fact   | Unknown and extra json properties are ignored and case insensitive names bind.                 |
| `AHugeBody_OfTwoMegabytes_IsRejectedOrFailsClosed_NeverAuthenticatingAsync`              | Fact   | A huge body of two megabytes is rejected or fails closed never authenticating.                 |
| `ByNegotiate_WithAnEstablishedIdentity_Is200_WithTheSessionCookieAsync`                  | Fact   | By negotiate with an established identity is200 with the session cookie.                       |
| `ByNegotiate_WithoutAnIdentity_ChallengesWith401AndWwwAuthenticateNegotiateAsync`        | Theory | By negotiate without an identity challenges with401 and www authenticate negotiate.            |
| `ByNegotiate_AnUnknownIdentity_Is403Async`                                               | Fact   | By negotiate an unknown identity is403.                                                        |
| `ByNegotiate_WhenNegotiateIsNotEnabled_Is404_EvenWithAnIdentityHeaderAsync`              | Fact   | By negotiate when negotiate is not enabled is404 even with an identity header.                 |
| `ByNegotiate_TheHybridJwtIsNotAcceptedInPlaceOfANegotiateIdentityAsync`                  | Fact   | By negotiate the hybrid jwt is not accepted in place of a negotiate identity.                  |
| `ByNegotiate_WithMfaEnabled_Is202WithAnEmptyBody_AndThePostFlowCompletesAsync`           | Fact   | By negotiate with mfa enabled is202 with an empty body and the post flow completes.            |
| `WhenTheNegotiateSchemeIsNotRegistered_TheRoutesFailWith500NotAnAccidentalOpenAsync`     | Fact   | When the negotiate scheme is not registered the routes fail with500 not an accidental open.    |
| `UserNamePassword_WithMfa_RightCode200_WrongCode401_MissingCode400Async`                 | Fact   | User name password with mfa right code200 wrong code401 missing code400.                       |
| `AProviderOutage_DuringMfa_Is500_AndNeverAnAuthenticationAsync`                          | Fact   | A provider outage during mfa is500 and never an authentication.                                |

### AuthenticationLockoutTests

`Jube.Test.Service.Authentication.AuthenticationLockoutTests` — 27 tests. Category: Service. Collection: Database.

| Test                                                                                               | Kind   | Verifies                                                                                                      |
|----------------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------------|
| `TheAccountLocksExactlyOnTheNthWrongPassword_WhereNIsPasswordAttemptsAsync`                        | Theory | The account locks exactly on the nth wrong password where n is password attempts.                             |
| `WithTheDefaultOfThree_TheThirdWrongPasswordLocks_NotTheFifthAsync`                                | Fact   | With the default of three the third wrong password locks not the fifth.                                       |
| `ANonPositiveThreshold_LocksOnTheVeryFirstFailure_AndNeverVerifiesAgainstTheRealHashTwiceAsync`    | Theory | A non positive threshold locks on the very first failure and never verifies against the real hash twice.      |
| `OnceLocked_EvenTheCorrectPasswordIsRefused_WithNoRealHashWork_AndTheCounterStopsMovingAsync`      | Fact   | Once locked even the correct password is refused with no real hash work and the counter stops moving.         |
| `ALockoutDoesNotExpireBeforeTheConfiguredWindowAsync`                                              | Fact   | A lockout does not expire before the configured window.                                                       |
| `ALockoutAutomaticallyExpiresAfterTheConfiguredWindowAsync`                                        | Fact   | A lockout automatically expires after the configured window.                                                  |
| `ANonPositiveLockoutMinutes_NeverAutoExpiresNoMatterHowOldTheLockIsAsync`                          | Theory | A non positive lockout minutes never auto expires no matter how old the lock is.                              |
| `EnablePasswordLockoutResetFalse_NeverAutoExpiresEvenWithAPositiveWindowAsync`                     | Fact   | Enable password lockout reset false never auto expires even with a positive window.                           |
| `EnablePasswordLockoutResetTrue_StillAutoExpiresAfterTheConfiguredWindowAsync`                     | Fact   | Enable password lockout reset true still auto expires after the configured window.                            |
| `ASuccessfulLogin_ResetsTheFailureCounterToZeroAsync`                                              | Fact   | A successful login resets the failure counter to zero.                                                        |
| `AfterAReset_TheCountRestartsFromZero_SoFailuresDoNotAccumulateAcrossSuccessesAsync`               | Fact   | After a reset the count restarts from zero so failures do not accumulate across successes.                    |
| `TheCorrectPasswordOnTheLastUnlockedAttempt_StillSucceedsAsync`                                    | Fact   | The correct password on the last unlocked attempt still succeeds.                                             |
| `ExpiredPasswordWithTheCorrectPassword_ClearsTheCounter_SoARightPasswordNeverBuildsUpToALockAsync` | Fact   | Expired password with the correct password clears the counter so a right password never builds up to a lock.  |
| `AUserWithoutAPasswordExpiry_IsCountedAndLockedLikeAnyOther_ButStillGetsTheMustChangeOutcomeAsync` | Fact   | A user without a password expiry is counted and locked like any other but still gets the must change outcome. |
| `ACorrectPasswordForAUserWithoutExpiry_StillGetsMustChange_AndIsNotCountedAsync`                   | Fact   | A correct password for a user without expiry still gets must change and is not counted.                       |
| `TheNoExpiryLockThresholdMatchesTheExpiringUsersThresholdAsync`                                    | Theory | The no expiry lock threshold matches the expiring users threshold.                                            |
| `ANullCreatedDateUserIsLockedTooAsync`                                                             | Fact   | A null created date user is locked too.                                                                       |
| `WhenANewPasswordIsSupplied_TheNoExpiryUserDoesLockOutAsync`                                       | Fact   | When a new password is supplied the no expiry user does lock out.                                             |
| `CountersAreIndependentPerUserAsync`                                                               | Fact   | Counters are independent per user.                                                                            |
| `ParallelWrongPasswords_LockAtTheBudget_AndOnlyTheAllowedNumberEverReachTheRealHashAsync`          | Theory | Parallel wrong passwords lock at the budget and only the allowed number ever reach the real hash.             |
| `ParallelFailuresBelowTheThreshold_ProduceExactlyNCountedFailures_NoLostUpdatesAsync`              | Theory | Parallel failures below the threshold produce exactly n counted failures no lost updates.                     |
| `ParallelGuesses_IncludingTheRightPassword_CannotAllBeTried_TheBudgetIsEnforcedAsync`              | Fact   | Parallel guesses including the right password cannot all be tried the budget is enforced.                     |
| `ParallelWrongPasswords_NeverExceedTheThresholdOfRealVerifications_EvenWithAHigherThresholdAsync`  | Fact   | Parallel wrong passwords never exceed the threshold of real verifications even with a higher threshold.       |
| `ParallelCorrectPasswordsOnALockedAccount_NeverSucceedAsync`                                       | Fact   | Parallel correct passwords on a locked account never succeed.                                                 |
| `ParallelMixOfRightAndWrong_OnlyTheRightOnesCanSucceed_AndNoneAreForgedAsync`                      | Fact   | Parallel mix of right and wrong only the right ones can succeed and none are forged.                          |
| `ParallelFailuresAgainstDifferentUsers_AreCountedExactlyAndDoNotBleedAsync`                        | Fact   | Parallel failures against different users are counted exactly and do not bleed.                               |
| `SequentialLoginsAfterAParallelBurst_BehaveAsLockedAsync`                                          | Fact   | Sequential logins after a parallel burst behave as locked.                                                    |

### AuthenticationLogoutServiceTests

`Jube.Test.Service.Authentication.AuthenticationLogoutServiceTests` — 11 tests. Category: Service. Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                             |
|---------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `AValidSession_RevokesTheUsersTokens_ClosesTheirHubsAndRecordsTheRowAsync`      | Fact   | A valid session revokes the users tokens closes their hubs and records the row.      |
| `NoSession_RevokesNothing_ClosesNothing_AndNeverThrowsAsync`                    | Fact   | No session revokes nothing closes nothing and never throws.                          |
| `AnAuthenticatedIdentityWithoutANameIsNoSessionAsync`                           | Theory | An authenticated identity without a name is no session.                              |
| `ADatabaseFailure_StillEndsTheBrowserSession_LogsAnError_AndClosesTheHubsAsync` | Fact   | A database failure still ends the browser session logs an error and closes the hubs. |
| `AFailingHubClose_IsLogged_AndDoesNotFailTheLogoutAsync`                        | Fact   | A failing hub close is logged and does not fail the logout.                          |
| `TheAuditRow_IsBestEffort_AFailingInsertNeverChangesTheOutcomeAsync`            | Fact   | The audit row is best effort a failing insert never changes the outcome.             |
| `ALogoutIsAuditedWithItsOutcomeAndSpanAsync`                                    | Fact   | A logout is audited with its outcome and span.                                       |
| `TheActorOfTheAuditLineIsNeverControlCharactersOfTheNameAsync`                  | Fact   | The actor of the audit line is never control characters of the name.                 |
| `NoSessionRows_AreWrittenOncePerAddressPerMinute_ButAlwaysForASessionAsync`     | Fact   | No session rows are written once per address per minute but always for a session.    |
| `TheRecorder_ClipsLengthAndStripsControlCharactersAsync`                        | Fact   | The recorder clips length and strips control characters.                             |
| `TheRecorder_NeverThrows_WhateverTheDatabaseDoesAsync`                          | Fact   | The recorder never throws whatever the database does.                                |

### AuthenticationMfaTests

`Jube.Test.Service.Authentication.AuthenticationMfaTests` — 30 tests. Category: Service. Collection: Database.

| Test                                                                                                  | Kind   | Verifies                                                                                                         |
|-------------------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------|
| `MfaRequired_ABlankCodeFailsValidation_BeforeAnyCredentialIsCheckedAsync`                             | Theory | Mfa required a blank code fails validation before any credential is checked.                                     |
| `MfaRequired_TheCorrectCode_IssuesTheCookies_AndTheProviderSawTheUserAndCodeAsync`                    | Fact   | Mfa required the correct code issues the cookies and the provider saw the user and code.                         |
| `MfaRequired_AWrongCode_IsUnauthorised_AndIssuesNothingAsync`                                         | Fact   | Mfa required a wrong code is unauthorised and issues nothing.                                                    |
| `AWrongMfaCode_IsCountedAsAFailure_AndDoesNotCommitTheSuccessOrResetTheCounterAsync`                  | Fact   | A wrong mfa code is counted as a failure and does not commit the success or reset the counter.                   |
| `ACorrectMfaCode_CompletesTheLogin_WritesTheSuccessRecord_AndResetsTheCounterAsync`                   | Fact   | A correct mfa code completes the login writes the success record and resets the counter.                         |
| `MfaGuessing_LocksTheAccountOnTheThirdWrongCode_AndALockedAccountRejectsTheRightCodeAndPasswordAsync` | Fact   | Mfa guessing locks the account on the third wrong code and a locked account rejects the right code and password. |
| `AlternatingRightPasswordAndWrongCode_CannotDodgeTheCounterAsync`                                     | Fact   | Alternating right password and wrong code cannot dodge the counter.                                              |
| `MixedPasswordAndMfaFailures_ShareOneCounterAsync`                                                    | Fact   | Mixed password and mfa failures share one counter.                                                               |
| `AProviderOutage_GivesTheReservedAttemptBack_SoItIsNotCountedAgainstTheUserAsync`                     | Fact   | A provider outage gives the reserved attempt back so it is not counted against the user.                         |
| `NegotiateMfa_WrongCodesAreCounted_TheAccountLocksOnTheThird_AndEverythingThenRefusesAsync`           | Fact   | Negotiate mfa wrong codes are counted the account locks on the third and everything then refuses.                |
| `NegotiateMfa_ParallelWrongCodes_NeverReachTheProviderMoreThanTheBudgetAllowsAsync`                   | Fact   | Negotiate mfa parallel wrong codes never reach the provider more than the budget allows.                         |
| `NegotiateMfa_ACorrectCodeResetsTheCounterAsync`                                                      | Fact   | Negotiate mfa a correct code resets the counter.                                                                 |
| `NegotiateMfa_ABlankCodeIsNotCountedAsync`                                                            | Fact   | Negotiate mfa a blank code is not counted.                                                                       |
| `MfaRequired_AWrongPassword_NeverReachesTheProviderAsync`                                             | Fact   | Mfa required a wrong password never reaches the provider.                                                        |
| `MfaRequired_AnAccountThatCannotLogIn_NeverReachesTheProviderAsync`                                   | Theory | Mfa required an account that cannot log in never reaches the provider.                                           |
| `MfaRequired_ACodeCannotBeReusedAsync`                                                                | Fact   | Mfa required a code cannot be reused.                                                                            |
| `MfaRequired_AnExpiredCode_IsRejected_OnTheInjectedClockAsync`                                        | Fact   | Mfa required an expired code is rejected on the injected clock.                                                  |
| `MfaRequired_ACodeIssuedForAnotherUser_IsRejectedAsync`                                               | Fact   | Mfa required a code issued for another user is rejected.                                                         |
| `MfaRequired_UnusualCodes_ArePassedToTheProviderVerbatim_AndRejectedAsync`                            | Theory | Mfa required unusual codes are passed to the provider verbatim and rejected.                                     |
| `MfaRequired_AProviderFailure_PropagatesAsAnException_NeverAsASuccessAsync`                           | Fact   | Mfa required a provider failure propagates as an exception never as a success.                                   |
| `MfaRequired_TheCancellationTokenReachesTheProviderAsync`                                             | Fact   | Mfa required the cancellation token reaches the provider.                                                        |
| `MfaNotRequired_ACodeIsIgnored_AndTheProviderIsNeverCalledAsync`                                      | Fact   | Mfa not required a code is ignored and the provider is never called.                                             |
| `MfaRequired_TheSubjectIsTheAuthenticatedIdentity_WhenOneIsPresentAsync`                              | Fact   | Mfa required the subject is the authenticated identity when one is present.                                      |
| `NegotiateWithMfa_TheGetSignalsMfaRequired_WithoutIssuingAnythingAsync`                               | Fact   | Negotiate with mfa the get signals mfa required without issuing anything.                                        |
| `NegotiateWithMfa_TheMfaPostWithoutACode_IsAccepted202_NotAnErrorAsync`                               | Fact   | Negotiate with mfa the mfa post without a code is accepted202 not an error.                                      |
| `NegotiateWithMfa_TheCorrectCode_IssuesCookiesForTheWindowsIdentityAsync`                             | Fact   | Negotiate with mfa the correct code issues cookies for the windows identity.                                     |
| `NegotiateWithMfa_AWrongCode_IsUnauthorised_AndAReusedCodeFailsAsync`                                 | Fact   | Negotiate with mfa a wrong code is unauthorised and a reused code fails.                                         |
| `NegotiateWithMfa_AnUnknownIdentity_IsForbidden_BeforeMfaIsConsultedAsync`                            | Fact   | Negotiate with mfa an unknown identity is forbidden before mfa is consulted.                                     |
| `NegotiateWithMfa_AProviderFailure_PropagatesAsync`                                                   | Fact   | Negotiate with mfa a provider failure propagates.                                                                |
| `NegotiateWithoutMfa_IssuesCookiesStraightAway_AndSuppliedCodesAreIgnoredAsync`                       | Fact   | Negotiate without mfa issues cookies straight away and supplied codes are ignored.                               |

### AuthenticationNegotiateTests

`Jube.Test.Service.Authentication.AuthenticationNegotiateTests` — 16 tests. Category: Service. Collection: Database.

| Test                                                                                 | Kind   | Verifies                                                                                 |
|--------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `GetByNegotiate_ForAKnownActiveUser_IssuesCookies_AndRecordsAType2SuccessAsync`      | Fact   | Get by negotiate for a known active user issues cookies and records a type2 success.     |
| `PostByNegotiateMfa_ForAKnownActiveUser_IssuesCookies_WithoutMfaConfiguredAsync`     | Fact   | Post by negotiate mfa for a known active user issues cookies without mfa configured.     |
| `UnknownWindowsIdentity_IsForbidden_AndRecordedAsFailureType1UnderThatIdentityAsync` | Fact   | Unknown windows identity is forbidden and recorded as failure type1 under that identity. |
| `IdentityMatchingIsExact_DomainQualifiersAreNotStripped_AndCaseMattersAsync`         | Theory | Identity matching is exact domain qualifiers are not stripped and case matters.          |
| `ADomainQualifiedRegistryName_MatchesTheSameQualifiedIdentityAsync`                  | Theory | A domain qualified registry name matches the same qualified identity.                    |
| `DeletedInactiveOrLockedUsers_AreForbidden_WithTheMatchingFailureTypeAsync`          | Theory | Deleted inactive or locked users are forbidden with the matching failure type.           |
| `Negotiate_DoesNotCheckPasswordsExpiryOrFailureCountersAsync`                        | Fact   | Negotiate does not check passwords expiry or failure counters.                           |
| `GetByNegotiate_AnUnauthenticatedIdentity_IsUnauthorised_ButThePostIsForbiddenAsync` | Fact   | Get by negotiate an unauthenticated identity is unauthorised but the post is forbidden.  |
| `AnAuthenticatedIdentityWithoutAName_IsUnauthorised_OnBothRoutesAsync`               | Fact   | An authenticated identity without a name is unauthorised on both routes.                 |
| `BothRoutes_Are404_WhenNegotiateIsNotTheActiveSchemeAsync`                           | Theory | Both routes are404 when negotiate is not the active scheme.                              |
| `HostileIdentities_MatchNothingAsync`                                                | Theory | Hostile identities match nothing.                                                        |
| `ACancelledToken_IsSwallowedByTheGenericCatch_AndBecomesForbiddenAsync`              | Fact   | A cancelled token is swallowed by the generic catch and becomes forbidden.               |
| `ControlCharactersInAnIdentity_NeverReachTheLogOrAuditLinesRawAsync`                 | Fact   | Control characters in an identity never reach the log or audit lines raw.                |
| `TheAuditLine_NamesTheEstablishedIdentityAsActor_AndTheOutcomeAsync`                 | Fact   | The audit line names the established identity as actor and the outcome.                  |
| `NegotiateOutcomes_AreLogged_SuccessAtInfo_AndFailureAtErrorWithTheCauseAsync`       | Fact   | Negotiate outcomes are logged success at info and failure at error with the cause.       |
| `ManyParallelNegotiateLogins_AllSucceedAndAllAreRecordedAsync`                       | Fact   | Many parallel negotiate logins all succeed and all are recorded.                         |

### AuthenticationPasswordLoginTests

`Jube.Test.Service.Authentication.AuthenticationPasswordLoginTests` — 24 tests. Category: Service. Collection: Database.

| Test                                                                                                      | Kind   | Verifies                                                                                                           |
|-----------------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------|
| `CorrectPassword_IssuesCookiesForTheUser_AndRecordsASuccessfulLoginAsync`                                 | Fact   | Correct password issues cookies for the user and records a successful login.                                       |
| `WrongPassword_IsUnauthorised_IssuesNothing_IncrementsTheCounter_AndRecordsFailureType5Async`             | Fact   | Wrong password is unauthorised issues nothing increments the counter and records failure type5.                    |
| `UnknownUser_IsUnauthorised_AndRecordedAsFailureType1UnderTheAttemptedNameAsync`                          | Fact   | Unknown user is unauthorised and recorded as failure type1 under the attempted name.                               |
| `BlankUserName_FailsValidation_TouchesNothingAsync`                                                       | Theory | Blank user name fails validation touches nothing.                                                                  |
| `BlankPassword_FailsValidation_TouchesNothing_AndDoesNotCountAsAFailureAsync`                             | Theory | Blank password fails validation touches nothing and does not count as a failure.                                   |
| `NullUserAndNullPassword_ReportBothValidationErrorsAsync`                                                 | Fact   | Null user and null password report both validation errors.                                                         |
| `HostileUserName_IsJustAnUnknownUser_AndTheRealUserIsUntouchedAsync`                                      | Theory | Hostile user name is just an unknown user and the real user is untouched.                                          |
| `HostilePassword_IsJustAWrongPasswordAsync`                                                               | Theory | Hostile password is just a wrong password.                                                                         |
| `UserNameWithANulByte_IsRejected_AndNothingIsRecordedAsync`                                               | Fact   | User name with a nul byte is rejected and nothing is recorded.                                                     |
| `OneMegabyteUserName_IsUnauthorised_ButLeavesNoLoginHistory_BecauseTheIndexRowLimitRejectsTheInsertAsync` | Fact   | One megabyte user name is unauthorised but leaves no login history because the index row limit rejects the insert. |
| `Weakness_AHundredKilobyteUserName_IsPersistedInFullInTheLoginHistoryAsync`                               | Fact   | Weakness a hundred kilobyte user name is persisted in full in the login history.                                   |
| `OneMegabytePassword_IsJustAWrongPasswordAsync`                                                           | Fact   | One megabyte password is just a wrong password.                                                                    |
| `UnicodeUserNameAndPassword_AuthenticateExactlyAsync`                                                     | Theory | Unicode user name and password authenticate exactly.                                                               |
| `UnicodeNormalisationDifferences_DoNotMatchAsync`                                                         | Fact   | Unicode normalisation differences do not match.                                                                    |
| `UserNameMatchingIsExact_CaseAndWhitespaceSensitiveAsync`                                                 | Theory | User name matching is exact case and whitespace sensitive.                                                         |
| `PasswordMatchingIsExact_CaseSensitiveAsync`                                                              | Fact   | Password matching is exact case sensitive.                                                                         |
| `AuthenticatedIdentityOnTheRequest_WinsOverTheSubmittedUserNameAsync`                                     | Fact   | Authenticated identity on the request wins over the submitted user name.                                           |
| `UnparsablePasswordAttemptsSetting_MakesEveryLoginFail_EvenTheCorrectOneAsync`                            | Theory | Unparsable password attempts setting makes every login fail even the correct one.                                  |
| `AnEmptyPasswordAttemptsSetting_IsUnsetAndFallsBackToTheDefault_SoTheCorrectLoginSucceedsAsync` | Fact | An empty password attempts setting is unset and falls back to the default so the correct login succeeds. |
| `ThePasswordHashingKeyFromTheEnvironmentIsWhatVerifyReceivesAsync`                                        | Fact   | The password hashing key from the environment is what verify receives.                                             |
| `ASpoofedBodyRemoteIp_NeverReachesTheLoginHistory_TheConnectionAddressDoesAsync`                          | Fact   | A spoofed body remote ip never reaches the login history the connection address does.                              |
| `ASpoofedBodyRemoteIp_IsIgnoredOnFailuresToo_AndWhenTheConnectionHasNoneItIsNullAsync`                    | Theory | A spoofed body remote ip is ignored on failures too and when the connection has none it is null.                   |
| `ACancelledToken_AbortsBeforeAnyCredentialWorkAsync`                                                      | Fact   | A cancelled token aborts before any credential work.                                                               |
| `EachLoginWritesExactlyOneAuditLine_WithAnonymousActor_AndNoCredentialsAsync`                             | Fact   | Each login writes exactly one audit line with anonymous actor and no credentials.                                  |
| `OAuthOrNegotiateEnabled_HidesTheUserNamePasswordRouteAsync`                                              | Fact   | O auth or negotiate enabled hides the user name password route.                                                    |

### AuthenticationPasswordSchemeTests

`Jube.Test.Service.Authentication.AuthenticationPasswordSchemeTests` — 49 tests. Category: Service. Collection:
Database.

| Test                                                                                           | Kind   | Verifies                                                                                              |
|------------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------|
| `Argon2_HashesVerify_AreSalted_AndCarryTheArgon2Prefix`                                        | Fact   | Argon2 hashes verify are salted and carry the argon2 prefix.                                          |
| `Argon2_Verify_IsExactOnPasswordAndKey`                                                        | Theory | Argon2 verify is exact on password and key.                                                           |
| `Argon2_NullKey_NeverVerifies_EvenTheRightPassword`                                            | Fact   | Argon2 null key never verifies even the right password.                                               |
| `Argon2_EmptyKey_UsesTheUnkeyedForm_AndDoesNotVerifyAgainstAKeyedHash`                         | Fact   | Argon2 empty key uses the unkeyed form and does not verify against a keyed hash.                      |
| `Argon2_HandlesUnusualPasswords_ExactlyAndFailsClosedOnNeighbours`                             | Theory | Argon2 handles unusual passwords exactly and fails closed on neighbours.                              |
| `Argon2_OneMegabytePassword_HashesAndVerifies`                                                 | Fact   | Argon2 one megabyte password hashes and verifies.                                                     |
| `Argon2_ATamperedHash_FailsClosed_NeverVerifies`                                               | Fact   | Argon2 a tampered hash fails closed never verifies.                                                   |
| `Sha256_ProducesLowerCaseHexOfTheUtf8Bytes`                                                    | Theory | Sha256 produces lower case hex of the utf8 bytes.                                                     |
| `Sha256_IsDeterministic_AndUnicodeSafe`                                                        | Fact   | Sha256 is deterministic and unicode safe.                                                             |
| `RealArgon2_EndToEndLogin_AcceptsTheRightPasswordAndRejectsTheWrongOneAsync`                   | Fact   | Real argon2 end to end login accepts the right password and rejects the wrong one.                    |
| `RealArgon2_NewPasswordIsStoredAsAnArgon2Hash_AndVerifiesAfterwardsAsync`                      | Fact   | Real argon2 new password is stored as an argon2 hash and verifies afterwards.                         |
| `AWrongPasswordCostsTheSameHashWorkAsARightOne_ForAKnownUserAsync`                             | Fact   | A wrong password costs the same hash work as a right one for a known user.                            |
| `WirePasswordHash_ReflectsTheUsersFlag_AndUnknownAndDeletedUsersLookLikeWireUsersAsync`        | Theory | Wire password hash reflects the users flag and unknown and deleted users look like wire users.        |
| `WirePasswordHash_NullUserName_IsFalse_WithoutTouchingTheDatabaseAsync`                        | Fact   | Wire password hash null user name is false without touching the database.                             |
| `Weakness_WirePasswordHash_ProbeDistinguishesWireZeroUsersFromUnknownOnesAsync`                | Fact   | Weakness wire password hash probe distinguishes wire zero users from unknown ones.                    |
| `WirePasswordHash_IsAlways404_WhenAnotherSchemeIsEnabled_EvenForAnAuthenticatedCallerAsync`    | Theory | Wire password hash is always404 when another scheme is enabled even for an authenticated caller.      |
| `Encryption_On_ACorrectlyEncryptedPassword_AuthenticatesAsync`                                 | Fact   | Encryption on a correctly encrypted password authenticates.                                           |
| `Encryption_On_AnEncryptedWrongPassword_IsABadCredential_AndCountsAsync`                       | Fact   | Encryption on an encrypted wrong password is a bad credential and counts.                             |
| `Encryption_On_UnicodePassword_RoundTripsAsync`                                                | Fact   | Encryption on unicode password round trips.                                                           |
| `Encryption_On_ThePlaintextPassword_IsNotAcceptable_AndLeavesNoTraceAsync`                     | Fact   | Encryption on the plaintext password is not acceptable and leaves no trace.                           |
| `Encryption_On_ACiphertextForTheWrongPrivateKey_IsRejectedAsync`                               | Fact   | Encryption on a ciphertext for the wrong private key is rejected.                                     |
| `Encryption_On_TamperedOrGarbledCiphertext_NeverAuthenticatesAsync`                            | Theory | Encryption on tampered or garbled ciphertext never authenticates.                                     |
| `Encryption_On_WhitespaceInsideTheBase64_IsIgnoredByTheDecoder_SoStillAuthenticatesAsync`      | Fact   | Encryption on whitespace inside the base64 is ignored by the decoder so still authenticates.          |
| `UndecryptableCiphertext_IsACountedFailure_LocksTheAccount_AndLeavesAHistoryRowAsync`          | Fact   | Undecryptable ciphertext is a counted failure locks the account and leaves a history row.             |
| `Encryption_Off_ACiphertextIsJustAWrongPasswordAsync`                                          | Fact   | Encryption off a ciphertext is just a wrong password.                                                 |
| `Encryption_Off_ThePlaintextAuthenticatesAsync`                                                | Fact   | Encryption off the plaintext authenticates.                                                           |
| `Weakness_EncryptionOnWithoutAPrivateKey_SilentlyAcceptsPlaintextPasswordsAsync`               | Fact   | Weakness encryption on without a private key silently accepts plaintext passwords.                    |
| `Encryption_On_TheNewPasswordIsDecryptedBeforeItIsHashedAndStoredAsync`                        | Fact   | Encryption on the new password is decrypted before it is hashed and stored.                           |
| `Encryption_On_AnUndecryptableNewPassword_ThrowsAfterTheOldOneVerified_AndChangesNothingAsync` | Fact   | Encryption on an undecryptable new password throws after the old one verified and changes nothing.    |
| `CorrectPasswordWithANewOne_ChangesThePassword_ResetsCounters_AndExtendsExpiryBy90DaysAsync`   | Fact   | Correct password with a new one changes the password resets counters and extends expiry by90 days.    |
| `AnExpiredPassword_CanBeReplaced_ByPresentingTheOldOneWithANewOneAsync`                        | Fact   | An expired password can be replaced by presenting the old one with a new one.                         |
| `AWeakNewPassword_IsRejectedWithTheSpecificRule_AndNothingChangesAsync`                        | Theory | A weak new password is rejected with the specific rule and nothing changes.                           |
| `ANewPasswordLongerThan128Characters_IsRejectedAsync`                                          | Fact   | A new password longer than128 characters is rejected.                                                 |
| `ManyWeaknesses_AreAllReportedTogetherAsync`                                                   | Fact   | Many weaknesses are all reported together.                                                            |
| `Weakness_WirePasswordHashUsers_HaveNoStrengthRulesApplied_BecauseTheServerOnlySeesAHashAsync` | Fact   | Weakness wire password hash users have no strength rules applied because the server only sees a hash. |
| `ANewPasswordWithAWrongCurrentOne_ChangesNothingAsync`                                         | Fact   | A new password with a wrong current one changes nothing.                                              |
| `ALockedUser_CannotChangeThePasswordThroughLoginAsync`                                         | Fact   | A locked user cannot change the password through login.                                               |
| `PasswordChangeState_WithABlankPassword_IsAPasswordEmptyFailure_Type6Async`                    | Fact   | Password change state with a blank password is a password empty failure type6.                        |
| `PasswordChangeState_WithoutANewPassword_FailsValidationAsync`                                 | Fact   | Password change state without a new password fails validation.                                        |
| `ChangePassword_CorrectCurrentAndStrongNew_ReplacesTheHash_WithAnEmptySuccessAsync`            | Fact   | Change password correct current and strong new replaces the hash with an empty success.               |
| `ChangePassword_WrongCurrent_IsUnauthorised_ChangesNothing_AndIsNotCountedAsync`               | Fact   | Change password wrong current is unauthorised changes nothing and is not counted.                     |
| `ChangePassword_MissingNewPassword_IsAStrengthFailureAsync`                                    | Theory | Change password missing new password is a strength failure.                                           |
| `ChangePassword_WeakNew_ListsTheBrokenRulesAsync`                                              | Fact   | Change password weak new lists the broken rules.                                                      |
| `ChangePassword_IsForbidden_WhenAnotherSchemeOwnsIdentityAsync`                                | Theory | Change password is forbidden when another scheme owns identity.                                       |
| `ChangePassword_WithoutAnIdentityName_IsABadRequestWithTheLegacyMessageAsync`                  | Fact   | Change password without an identity name is a bad request with the legacy message.                    |
| `ChangePassword_EmptyCurrentPassword_IsABadRequest_NotAnExceptionAsync`                        | Fact   | Change password empty current password is a bad request not an exception.                             |
| `ChangePassword_ForAnIdentityWithNoUserRecord_IsUnauthorised_WithEquivalentHashWorkAsync`      | Fact   | Change password for an identity with no user record is unauthorised with equivalent hash work.        |
| `Weakness_ChangePassword_UnlocksALockedAccount_BecauseSetPasswordClearsTheLockAsync`           | Fact   | Weakness change password unlocks a locked account because set password clears the lock.               |
| `ChangePassword_EncryptedCurrentAndNew_AreDecryptedAsync`                                      | Fact   | Change password encrypted current and new are decrypted.                                              |

### AuthenticationRsaMfaLockoutTests

`Jube.Test.Service.Authentication.AuthenticationRsaMfaLockoutTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                         |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `WrongRsaCodes_AreCounted_LockTheAccount_AndTheRightCodeIsThenRefusedAsync` | Fact | Wrong rsa codes are counted lock the account and the right code is then refused. |
| `TheRightRsaCode_CompletesTheLoginAndResetsTheCounterAsync`                 | Fact | The right rsa code completes the login and resets the counter.                   |

### AuthenticationRsaMfaTests

`Jube.Test.Service.Authentication.AuthenticationRsaMfaTests` — 11 tests. Category: Service.

| Test                                                                | Kind   | Verifies                                                                 |
|---------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `TheCorrectCode_VerifiesAsync`                                      | Fact   | The correct code verifies.                                               |
| `AnyOtherCode_IsRejectedAsync`                                      | Theory | Any other code is rejected.                                              |
| `TheRequest_CarriesTheSubjectClientIdAndTheSecuridFactorAsync`      | Fact   | The request carries the subject client id and the securid factor.        |
| `AWrongClientKey_IsAProviderFailure_NotAFalseAsync`                 | Fact   | A wrong client key is a provider failure not a false.                    |
| `AnUnreachableProvider_ThrowsRatherThanAcceptingTheCodeAsync`       | Fact   | An unreachable provider throws rather than accepting the code.           |
| `ACancelledToken_AbortsTheCallAsync`                                | Fact   | A cancelled token aborts the call.                                       |
| `TheRsaProviderNeverLogsTheMfaCode_OnSuccessOrRejectionAsync`       | Theory | The rsa provider never logs the mfa code on success or rejection.        |
| `TheProviderLog_NeverContainsTheClientKeyAsync`                     | Fact   | The provider log never contains the client key.                          |
| `ProviderFailures_AreLoggedAtError_WithoutTheCodeAsync`             | Fact   | Provider failures are logged at error without the code.                  |
| `ARejectedProviderResponse_LeavesNoCodeOrResponseBodyInAnyLogAsync` | Fact   | A rejected provider response leaves no code or response body in any log. |
| `Weakness_EachVerification_BuildsANewProviderAndHttpClientAsync`    | Fact   | Weakness each verification builds a new provider and http client.        |

### AuthenticationSecretsInLogsTests

`Jube.Test.Service.Authentication.AuthenticationSecretsInLogsTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                                       | Kind   | Verifies                                                                                         |
|--------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `NoSecretEverReachesAnyLogOrAuditLineAsync`                                                | Theory | No secret ever reaches any log or audit line.                                                    |
| `EveryScenario_TogetherProduceAtLeastOneAuditLine_PerOperation_AndNoneCarryAPasswordAsync` | Fact   | Every scenario together produce at least one audit line per operation and none carry a password. |

### LoginSourceIpThrottleTests

`Jube.Test.Service.Authentication.LoginSourceIpThrottleTests` — 6 tests. Category: Unit.

| Test                                                | Kind | Verifies                                                   |
|-----------------------------------------------------|------|------------------------------------------------------------|
| `SuccessfulOrUnrecordedAttemptsAreNeverThrottled`   | Fact | Successful or unrecorded attempts are never throttled.     |
| `FailedAttemptsWithinTheThresholdAreNeverExceeded`  | Fact | Failed attempts within the threshold are never exceeded.   |
| `TheFailedAttemptThatCrossesTheThresholdIsExceeded` | Fact | The failed attempt that crosses the threshold is exceeded. |
| `DifferentSourceIpsAreCountedIndependently`         | Fact | Different source ips are counted independently.            |
| `TheWindowResetsOnceItHasElapsed`                   | Fact | The window resets once it has elapsed.                     |
| `ANullOrEmptyRemoteIpIsNeverThrottled`              | Fact | A null or empty remote ip is never throttled.              |

### OAuthPromptSelectorTests

`Jube.Test.Service.Authentication.OAuthPromptSelectorTests` — 3 tests. Category: Unit.

| Test                                                        | Kind   | Verifies                                                           |
|-------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `Resolve_AppliesTheConfiguredPromptWhenNoneIsAlreadySet`    | Theory | Resolve applies the configured prompt when none is already set.    |
| `Resolve_LeavesThePromptUnsetWhenNothingIsConfigured`       | Theory | Resolve leaves the prompt unset when nothing is configured.        |
| `Resolve_NeverOverwritesAPromptAlreadyOnTheProtocolMessage` | Fact   | Resolve never overwrites a prompt already on the protocol message. |

### OAuthSignInInterstitialTests

`Jube.Test.Service.Authentication.OAuthSignInInterstitialTests` — 4 tests. Category: Unit.

| Test                                                         | Kind   | Verifies                                                            |
|--------------------------------------------------------------|--------|---------------------------------------------------------------------|
| `IsSafeTarget_AcceptsLocalPathsAndHttpUrls`                  | Theory | Is safe target accepts local paths and http urls.                   |
| `IsSafeTarget_RejectsOpenRedirectsSchemesAndHeaderInjection` | Theory | Is safe target rejects open redirects schemes and header injection. |
| `Write_SetsA200WithRefreshToTheTargetAndNoCaching`           | Fact   | Write sets a 200 with refresh to the target and no caching.         |
| `ApplyNoCacheHeaders_SetsCacheControlPragmaAndExpires`       | Fact   | Apply no cache headers sets cache control pragma and expires.       |

## Role grant access

### CaseWorkflowRoleGrantAccessTests

`Jube.Test.Service.RoleGrantAccess.CaseWorkflowRoleGrantAccessTests` — 8 tests.

| Test                                            | Kind   | Verifies                                          |
|-------------------------------------------------|--------|---------------------------------------------------|
| `NoGrantMeansNoAccessAsync`                     | Theory | No grant means no access.                         |
| `GrantGivesAccessToThatRoleOnlyAsync`           | Theory | Grant gives access to that role only.             |
| `RevokeRemovesAccessAtOnceAsync`                | Theory | Revoke removes access at once.                    |
| `RegrantAfterRevokeRestoresAccessAsync`         | Theory | Regrant after revoke restores access.             |
| `EachRoleKeepsItsOwnAccessAsync`                | Theory | Each role keeps its own access.                   |
| `DeletedRoleLosesAccessWithoutErrorsAsync`      | Theory | Deleted role loses access without errors.         |
| `MovingAUserToAnotherRoleMovesTheirAccessAsync` | Theory | Moving a user to another role moves their access. |
| `AnotherTenantsRoleNeverGrantsAsync`            | Theory | Another tenants role never grants.                |

### EntityAnalysisModelRoleGrantAccessTests

`Jube.Test.Service.RoleGrantAccess.EntityAnalysisModelRoleGrantAccessTests` — 8 tests.

| Test                                            | Kind | Verifies                                          |
|-------------------------------------------------|------|---------------------------------------------------|
| `NoGrantMeansNoAccessAsync`                     | Fact | No grant means no access.                         |
| `GrantGivesAccessToThatRoleOnlyAsync`           | Fact | Grant gives access to that role only.             |
| `RevokeRemovesAccessAtOnceAsync`                | Fact | Revoke removes access at once.                    |
| `RegrantAfterRevokeRestoresAccessAsync`         | Fact | Regrant after revoke restores access.             |
| `EachRoleKeepsItsOwnAccessAsync`                | Fact | Each role keeps its own access.                   |
| `DeletedRoleLosesAccessWithoutErrorsAsync`      | Fact | Deleted role loses access without errors.         |
| `MovingAUserToAnotherRoleMovesTheirAccessAsync` | Fact | Moving a user to another role moves their access. |
| `AnotherTenantsRoleNeverGrantsAsync`            | Fact | Another tenants role never grants.                |

### VisualisationRoleGrantAccessTests

`Jube.Test.Service.RoleGrantAccess.VisualisationRoleGrantAccessTests` — 8 tests.

| Test                                            | Kind   | Verifies                                          |
|-------------------------------------------------|--------|---------------------------------------------------|
| `NoGrantMeansNoAccessAsync`                     | Theory | No grant means no access.                         |
| `GrantGivesAccessToThatRoleOnlyAsync`           | Theory | Grant gives access to that role only.             |
| `RevokeRemovesAccessAtOnceAsync`                | Theory | Revoke removes access at once.                    |
| `RegrantAfterRevokeRestoresAccessAsync`         | Theory | Regrant after revoke restores access.             |
| `EachRoleKeepsItsOwnAccessAsync`                | Theory | Each role keeps its own access.                   |
| `DeletedRoleLosesAccessWithoutErrorsAsync`      | Theory | Deleted role loses access without errors.         |
| `MovingAUserToAnotherRoleMovesTheirAccessAsync` | Theory | Moving a user to another role moves their access. |
| `AnotherTenantsRoleNeverGrantsAsync`            | Theory | Another tenants role never grants.                |

## Security

### HtmlSanitiserHardeningTests

`Jube.Test.Service.Security.HtmlSanitiserHardeningTests` — 6 tests. Category: Service.

| Test                                                        | Kind   | Verifies                                                             |
|-------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `ImageSourcesThatAreSameOriginOrExecutableAreDropped`       | Theory | Image sources that are same origin or executable are dropped.        |
| `SafeImageSourcesAreKept`                                   | Theory | Safe image sources are kept.                                         |
| `LinksAndOrdinaryMarkupAreUnchangedAndScriptIsStillDropped` | Fact   | Links and ordinary markup are unchanged and script is still dropped. |
| `StyleValuesCannotCarryAUrlOrAnExpressionWithParentheses`   | Theory | Style values cannot carry a url or an expression with parentheses.   |
| `HarmlessParenthesisedStyleValuesAreKept`                   | Fact   | Harmless parenthesised style values are kept.                        |
| `HostileLongInputCostsLinearTime`                           | Fact   | Hostile long input costs linear time.                                |

## User login

### UserLoginServiceTests

`Jube.Test.Service.UserLogin.UserLoginServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                |
| `ListDescribesOAuthFailureWithMessageAsync`                    | Fact   | List describes o auth failure with message.                        |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                          |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.               |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.               |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                            |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.      |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.   |
| `ListIsScopedToTheCallersTenantAsync`                          | Fact   | List is scoped to the callers tenant.                              |
| `AttemptsAgainstUnknownNamesAreVisibleToALandlordOnlyAsync`    | Fact   | Attempts against unknown names are visible to a landlord only.     |
| `LandlordSeesTheLoginAttemptsOfEveryTenantAsync`               | Fact   | Landlord sees the login attempts of every tenant.                  |
| `ListSortsByCreatedUserAscendingAndDescendingAsync`            | Fact   | List sorts by created user ascending and descending.               |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.             |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.         |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first. |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller. |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                             |

## User logout

### UserLogoutServiceTests

`Jube.Test.Service.UserLogout.UserLogoutServiceTests` — 27 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                               |
|----------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `ListReturnsMostRecentFirstAndProjectsFieldsAsync`             | Fact   | List returns most recent first and projects fields.                    |
| `ListDescribesEveryReasonAndOutcomeAsync`                      | Theory | List describes every reason and outcome.                               |
| `ListProjectsHowLongTheSessionLivedAsync`                      | Fact   | List projects how long the session lived.                              |
| `ListLeavesTheSessionLengthEmptyWhenTheStartIsUnknownAsync`    | Fact   | List leaves the session length empty when the start is unknown.        |
| `ListClampsTakeToOneHundredThousandAsync`                      | Fact   | List clamps take to one hundred thousand.                              |
| `ListWithoutPermissionThrowsForbiddenAsync`                    | Fact   | List without permission throws forbidden.                              |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`            | Fact   | Create with null user name throws not authenticated.                   |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with unknown tenant throws not authenticated.                   |
| `ListFiltersByDateRangeAsync`                                  | Fact   | List filters by date range.                                            |
| `ListDefaultsToTheLastHourAsync`                               | Fact   | List defaults to the last hour.                                        |
| `ListFiltersBySearchAsync`                                     | Fact   | List filters by search.                                                |
| `ListSearchMatchesEveryTextColumnCaseInsensitivelyAsync`       | Theory | List search matches every text column case insensitively.              |
| `ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync`    | Fact   | List with zero sample percentage excludes every matching row.          |
| `ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync` | Fact   | List with hundred sample percentage includes every matching row.       |
| `ListIsScopedToTheCallersTenantAsync`                          | Fact   | List is scoped to the callers tenant.                                  |
| `RowsOfNoTenantAreVisibleToALandlordOnlyAsync`                 | Fact   | Rows of no tenant are visible to a landlord only.                      |
| `LandlordSeesTheSessionsCutInEveryTenantAsync`                 | Fact   | Landlord sees the sessions cut in every tenant.                        |
| `ListSortsByCreatedUserAscendingAndDescendingAsync`            | Fact   | List sorts by created user ascending and descending.                   |
| `ListSortDirectionIsCaseInsensitiveForAscendingAsync`          | Theory | List sort direction is case insensitive for ascending.                 |
| `ListWithGarbageSortDirectionFallsBackToDescendingAsync`       | Fact   | List with garbage sort direction falls back to descending.             |
| `ListSortsByEveryDocumentedColumnWithoutErrorAsync`            | Theory | List sorts by every documented column without error.                   |
| `ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync` | Fact   | List with unrecognised sort field falls back to most recent first.     |
| `ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync` | Fact   | List total reflects full filtered count even when take is smaller.     |
| `ListStatisticsIsEmptyForThisDtoAsync`                         | Fact   | List statistics is empty for this dto.                                 |
| `ListEmitsOneAuditLineAndOneSpanAndNoChangeEventAsync`         | Fact   | List emits one audit line and one span and no change event.            |
| `ForbiddenListIsAuditedAsForbiddenAsync`                       | Fact   | Forbidden list is audited as forbidden.                                |
| `ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue`   | Fact   | Tool is declared on the service method and published by the catalogue. |
