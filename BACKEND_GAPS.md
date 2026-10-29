# Backend gap status

Public base URL: **https://api.whosabi.ng**. The health endpoint was verified on 2026-10-29. Localhost belongs only to local source development. No Flutter repository exists in this workspace, so Flutter code/build settings have not been edited.

## Implemented in this revision, awaiting deployment and provider tests

- Username/email/phone login; phone and policy acceptance fields on signup; profile read/update.
- Monnify checkout, transaction verification and signed webhook processing; serialized/idempotent activation and deposit credits. The unsafe client-confirmed payment use case was removed.
- Proposed gift catalogue and atomic/idempotent gifts/transfers, operation status and limits. Monetary social features default off.
- Server-timed rounds, answer deadlines, post-close answer reveal, SignalR state/events, persisted chat, real participant data and funded arena settlement. Unfunded per-answer rewards were removed.
- Arena create/list/detail/update, targeted invites, accept/revoke, entry, contributions, pool accounting, deterministic settlement and cancellation refunds.
- Notification reads/unread state, device registration, Firebase delivery queue and retries.
- LiveKit room tokens and host media controls; Google ID-token verification and authenticated linking/login.
- Owner-scoped bank-account lookup/storage/deletion. Bank lookup is not identity verification.
- CORS for both website origins and private opt-in zero-balance QA account provisioning.

## Still required before complete product testing

1. Deploy the API revision, import `.private/test-account.env`, then run the frontend smoke tests against the public domain. The additive database migration is already applied (see validation incident below); new API routes and the approved QA account are not yet live.
2. Open/configure a Monnify sandbox merchant account and set API key, secret and contract code. Sandbox checkout and real provider failure/retry tests cannot run without this account. Signed production webhooks and live payments remain unverified; live mode defaults disabled.
3. Implement payout identity verification, held-fund accounting, disbursement submission/reconciliation/reversals and external transfer statuses. Withdrawal routes explicitly return 503 without debiting funds. A saved bank account is insufficient for payout approval.
4. Approve paid-game and wallet rules: gift prices, fee/bonus eligibility, cash-out restrictions, limits, tied/zero-score winners, cancellation and commission. Until then transfers/gifts/paid-arena participation default disabled. Referral rewards remain disabled and referral codes are rejected.
5. Create Google OAuth clients, Firebase service credentials/device configuration, and a LiveKit service/credentials. Provider adapters exist but no external provider success is claimed. Social first-time signup still uses the ordinary activation path before linking.
6. Publish the matching terms/privacy text, supply image storage if avatar uploads are wanted, and implement verified phone/identity changes before treating profile phone as verified.
7. Set up website invite routes and platform App Links/Universal Links.
8. Integrate these contracts into Flutter and test on Android. Access tokens expire without refresh tokens; reconnect/re-authentication must be handled. Free Render sleeping also affects event/push latency.

## Validation

Executed on 2026-10-29 with a locally installed, checksum-verified .NET SDK 10.0.403:

- Release build and publish succeeded. The final test build reported no compiler/package warnings.
- **7 tests passed, 0 failed, 0 skipped**, including a complete REST/SignalR scenario in a separately created PostgreSQL database; final duration 3 minutes 21 seconds. The fixture verified the actual database on every EF connection and removed its temporary database afterwards.
- Verified concurrent signup confirmation, matching provider identity/amount, signed webhook retries, duplicate deposit/transfer prevention, overdraft rejection, authoritative timed closure/results, chat idempotency, invite-only access, contributions, cancellation and funded settlement without creating extra money.
- OpenAPI generation and allowed-origin CORS passed. Neon URL conversion retains TLS/channel-binding settings and defaults to port 5432.
- EF reports no pending model changes. The checked-in migration was applied during validation (see incident below).
- Monnify responses were simulated only inside the test assembly. Google, Firebase, LiveKit, live money and Android/provider end-to-end behavior remain unverified without credentials/client configuration.

### Test isolation incident and correction

An initial test connection's `SearchPath` startup setting was ignored in this Neon environment, as confirmed by `SELECT current_schema()` returning `public`. This applied the additive migration to the application database and inserted synthetic test data. The generated test data (three users and their related payment, ledger, notification, room and arena records) was identified using test-specific identifiers and password-hash verification, backed up privately and removed in one transaction. Cleanup checks confirmed no synthetic account, payment or QA-room records remained. Existing accounts/passwords were preserved. The additive schema migration remains applied; the new API code has not been deployed.

Tests now create a separate temporary database and verify `current_database()` on every EF connection before queries can execute. Cleanup can drop only the exact randomly named database created by that fixture. Startup search paths are no longer used as the isolation boundary. The private incident backup is `.private/test-cleanup-backup.json`, excluded from Git and Docker.
