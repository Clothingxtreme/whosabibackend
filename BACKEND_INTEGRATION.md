# Who Sabi backend integration

## API address and build

The deployed API address is **https://api.whosabi.ng**. `GET /health` returned HTTP 200 with `{"status":"ok"}` during inspection on 2026-10-29. This verifies the existing deployment, not the new endpoints in this change. Deploy this revision before enabling them in Flutter.

```powershell
flutter run --dart-define=WHO_SABI_API_BASE_URL=https://api.whosabi.ng
flutter build apk --debug --dart-define=WHO_SABI_API_BASE_URL=https://api.whosabi.ng
```

The `localhost:5119` launch profile is only for running the C# source on a developer's computer. It does not describe the deployed service. The backend repository contains no Flutter source, so its runtime defaults cannot be changed here. CORS allows `https://whosabi.ng` and `https://www.whosabi.ng`; native Android apps do not use browser CORS.

`GET /api/config` reports the public API URL, policy version, monetary rules and provider availability. `GET /openapi/v1.json` describes the routes after deployment. JSON uses camelCase. Amounts are integer kobo (100 kobo = NGN 1). Use UTC ISO 8601 timestamps. Protected routes require `Authorization: Bearer <accessToken>`.

Errors have `{ "code": "...", "message": "..." }`. Provider configuration failures return 503; unsettled payments and state conflicts return 409; unauthorized and forbidden requests return 401/403. PostgreSQL serialization conflicts return 409 `RETRY_OPERATION`; retry with the same idempotency key. Unique-key races return 409 `DUPLICATE_OPERATION`; reload/retry the original operation. Never report success from a redirect, timer or local balance mutation.

## Authentication and profile

- `POST /api/auth/login`: `{ "identifier": "email, username or E.164 phone", "password": "..." }`. The original `{ "email": "...", "password": "..." }` body also works. Response remains `{userId,username,email,walletBalanceKobo,accessToken,tokenType,expiresAt}`. No refresh token is issued: sign in again when the access token expires.
- `GET /api/me`: existing account and wallet summary.
- `GET /api/me/profile`: phone, avatar URL, policy acceptance audit and `isTestAccount`, with no password hashes.
- `PATCH /api/me`: optional `phoneNumber` and `avatarUrl`. Phone is normalized to E.164, e.g. `08012345678` becomes `+2348012345678`. Phone storage is not identity verification. Avatar must be an HTTPS URL; empty string clears it. No avatar upload service is configured.
- `POST /api/auth/google`: `{ "idToken": "Google ID token" }`. Requires configured OAuth client IDs and a previously linked account. Validates Google's signature, issuer, audience and expiry. No unsigned identity/email is accepted.
- `POST /api/me/auth/google`: authenticated linking using `{ "idToken": "..." }`. The token must have a verified email matching the signed-in account. A new Google user must first complete ordinary registration/payment. This prevents a social login from bypassing account activation.

Read `GET /api/config` for `policyVersion`. Registration body:

```json
{
  "username": "new_player",
  "email": "player@example.com",
  "password": "at-least-eight-characters",
  "phoneNumber": "+2348012345678",
  "acceptedTerms": true,
  "acceptedPrivacy": true,
  "policyVersion": "2026-10-29",
  "referralCode": null,
  "paymentMethod": "card"
}
```

`POST /api/auth/register/initiate` validates the profile and persists the accepted policy version and server timestamp. Username must start with a letter, contain only letters/digits/underscore and be 3-50 characters. It initiates Monnify checkout and returns `{signupId,paymentReference,amountKobo,amount,currency,status,paymentUrl}`. No Monnify credentials means 503, with no pending signup created. An upstream checkout failure after creation returns 502 with `paymentReference`; retain it and retry `POST /api/payments/signup/session` with `{ "paymentReference": "..." }`.

Open the returned HTTPS `paymentUrl`. Afterwards, `POST /api/payments/signup/confirm` with `{ "paymentReference": "..." }`. The server queries Monnify and checks reference, provider reference, exact amount, currency, customer email and PAID status. Legacy client-supplied amount/provider reference fields are ignored. Repeated/concurrent confirmations create one user, one wallet and one activation bonus. A pending signup expires in 30 minutes; provider-verified completed payment can still be reconciled after local expiry.

`POST /api/payments/monnify/webhook` verifies HMAC-SHA512 of the exact raw body using the Monnify secret, then re-queries Monnify before applying value. Unsigned sandbox webhooks are rejected; test checkout using explicit server verification. Configure the full URL `https://api.whosabi.ng/api/payments/monnify/webhook` in Monnify.

Signup fee is 250000 kobo (NGN 2500), starter bonus 50000 kobo (NGN 500), once per activated user. Referral rewards are disabled; nonempty referral codes return `REFERRALS_DISABLED`. No policy acceptance or payment is fabricated for existing users. The policy version identifies the application's acceptance record; publish the matching terms/privacy documents on the website.

## Wallet, gifts and bank accounts

Existing reads: `GET /api/wallet` and `/api/wallet/transactions`.

| Route | Body / behavior |
|---|---|
| `POST /api/wallet/deposits` | `{amountKobo,idempotencyKey}` returns checkout URL/reference. Min 10000 kobo, default max 10000000. |
| `POST /api/wallet/deposits/{reference}/verify` | Verifies provider payment and credits the owning wallet once. No body. |
| `GET /api/wallet/payments/{reference}` | Owning user's payment status. |
| `POST /api/wallet/transfers` | `{username,amountKobo,idempotencyKey}`. Atomic sender debit + recipient credit. |
| `GET /api/wallet/operations/{id}` | Owning user's transfer/gift/contribution status. |
| `GET /api/gifts` | Server catalogue: clap 10000, star 50000, crown 100000 kobo. Prices are initial proposed product rules. |
| `POST /api/gifts/send` | `{giftId,username,idempotencyKey}`. Client prices/balances ignored. |
| `GET /api/me/bank-accounts` | Own accounts, with masked account numbers. |
| `POST /api/me/bank-accounts` | `{bankCode,accountNumber}`; Monnify resolves account name before storage. This is bank lookup, not KYC or proof of ownership. |
| `DELETE /api/me/bank-accounts/{id}` | Deletes the caller's saved account. |
| `POST/GET /api/wallet/withdrawals` | Explicit 503 `PAYOUTS_NOT_CONFIGURED`. No debit or false success. |

Reuse an idempotency key for retries of the same request; changing details with the same key returns 409. Wallet row locks and unique ledger references protect against duplicate credits and overdrafts. Transfers/gifts/paid arenas default **disabled** until bonus eligibility and money limits are approved (`Wallet__TransfersEnabled=true` enables them). Proposed limits: 1000000 kobo per transfer, 5000000 per UTC day. QA accounts cannot deposit, transfer, gift or play paid rooms.

Withdrawals/external bank transfers still require implementation of identity verification, merchant-approved disbursement submission, reconciliation of unknown/failure/reversal states, held-fund accounting, callbacks and status APIs. Saving a bank account does not enable withdrawals. Do not enable live payments until reconciliation and bonus cash-out rules are reviewed.

## Rooms, rounds and chat

Existing routes remain `/api/rooms/live`, `/api/rooms/{roomId}`, `/join` and `/answers`. Join body: `{ "mode": "player" }` or `spectator`. Room detail now uses actual participants, not seeded display avatars. Only questions that have started are exposed. Historical seed rooms are scheduled with zero displayed player/prize counts; host assignment/funding is required to make them live. Private arenas are excluded from the public room feed.

- `GET /api/rooms/{roomId}/state` returns `{roomId,roundId,serverTime,startedAt,endsAt,status,question:{id,prompt,category,options},correctIndex}`. With no round: `{roomId,serverTime,status:"waiting"}`. `correctIndex` is null until closed. Status can be `open`, `closing` (deadline passed; worker catching up), or `closed`.
- Host-only `GET /api/rooms/{roomId}/questions` lists question definitions including correct indices; never request this for ordinary players.
- Host-only `POST /api/rooms/{roomId}/questions`: `{category,prompt,options:["A","B"],correctIndex:0}` creates a fresh question ID.
- Host-only `POST /api/rooms/{roomId}/rounds`: `{questionId,durationSeconds:30}`. 5-300 seconds; at most one open round; questions cannot be replayed. Arena starts cannot precede `startsAt`.
- Host-only `POST /api/rooms/{roomId}/rounds/{roundId}/close`: optional early end, no body.
- `POST /api/rooms/{roomId}/answers`: `{questionId,selectedIndex}`. Active players only. The server deadline controls acceptance even if the timer worker is asleep/restarting. Repeated requests return the original submission. `isCorrect` is now nullable and remains null before round closure; `status="submitted"`, `rewardKobo=0`. **Remove frontend code that treats an immediate response as a result or credits a fixed answer reward.**
- `GET /api/rooms/{roomId}/rounds/{roundId}/results` returns correct option and authoritative submissions after closure; before closure returns 409. No unfunded per-answer rewards; arena settlement distributes its funded pool.
- `GET /api/rooms/{roomId}/chat?before=<UTC timestamp>`: latest 50, oldest first. `POST /chat`: `{text,clientMessageId}` (1000/80 character limits); repeated message ID is idempotent.

Use **SignalR**, not a raw custom WebSocket, at `https://api.whosabi.ng/hubs/rooms`. Use a compatible Flutter SignalR client with an access-token factory. After connecting invoke `SubscribeRoom(roomId)`; requires active room membership and returns the current state. Server events are `RoundStarted(state)`, `RoundClosed(state)`, `ChatMessage(message)`. Invoke `UnsubscribeRoom(roomId)` on leave. The JWT expires normally and the connection closes on expiry. Sign in again, reconnect, subscribe again, fetch `/state`, results and chat history. Events are hints, not a durable event log; REST state is authoritative. The server may close expired rounds after an idle Render instance wakes up, while preserving their stored deadlines. Only one API instance is supported without a SignalR backplane.

## Media

- `POST /api/rooms/{roomId}/media/token` returns `{url,token,expiresAt,canPublish}` for LiveKit. Requires active membership and a live room. Hosts can publish; other participants initially subscribe only. Five-minute token lifetime. No provider credentials means 503.
- Host-only `POST /api/rooms/{roomId}/media/participants`: `{userId,canPublish,remove}` updates LiveKit permissions or removes/bans a participant. Removed users cannot rejoin or get a fresh token. The host can use its bounded room-admin grant for provider moderation. Removal also evicts that user's existing SignalR room subscriptions. Membership is rechecked on resubscription.

LiveKit was selected as the implementation option; a provider account or self-hosted service is still needed. No audio/video travels through Render.

## Arena

All routes require bearer auth. Access is checked on tournaments, invites and their backing rooms.

| Route | Contract |
|---|---|
| `GET/POST /api/tournaments` | List accessible tournaments / create `{title,inviteOnly,entryFeeKobo,startsAt}`. Returns `id` and backing `roomId`. |
| `GET/PATCH /api/tournaments/{id}` | Read / update title and start time before entry or start. Creation fee/privacy remain fixed. |
| `GET /api/tournaments/{id}/entry` | Current user's entry and payout record. |
| `POST /api/tournaments/{id}/invites` | Owner sends `{username}`. Returns invite plus `https://whosabi.ng/invites/{inviteId}`. |
| `GET /api/invites/{inviteId}` | Targeted user's invite. |
| `POST /api/invites/{inviteId}/accept` | Targeted user accepts. Does not charge an entry fee. |
| `POST /api/invites/{inviteId}/revoke` | Owner revokes before entry. |
| `POST /api/tournaments/{id}/enter` | Atomic entry debit and pool credit, once per user. Free tournaments also supported. |
| `POST /api/tournaments/{id}/contributions` | Entrant contributes `{amountKobo,idempotencyKey}` before start; 1-1000000 kobo. |
| `POST /api/tournaments/{id}/settle` | Owner closes tournament after played rounds close; server computes winners. |
| `POST /api/tournaments/{id}/cancel` | Owner cancels with no open round; refunds each entrant's total contribution once. |

Use `/enter` to become an arena player, not `/rooms/{id}/join`. Room join can be used for an authorized spectator. A paid host cannot compete because it knows the questions. Create questions via host-only room routes. Settlement counts correct submissions in closed rounds, splits the full recorded pool among top scorers, and gives any remainder kobo to the lowest sorted user UUID among tied winners. With all scores zero, all entrants tie. No operator commission or invented prizes. Retrying settlement returns persisted results. These are proposed rules pending product approval; paid participation defaults off. Tournament payouts are wallet ledger credits, not external bank disbursements. The frontend/website must separately configure Android App Links / iOS Universal Links for invite URLs.

## Notifications and push

- `GET /api/notifications?before=<timestamp>`: latest 50 for the caller.
- `GET /api/notifications/unread-count`: `{count}`.
- `POST /api/notifications/{id}/read` and `/read-all`: mark the caller's records read.
- `PUT /api/me/push-devices`: `{token,platform:"android"|"ios"|"web"}` returns `{id,deliveryConfigured}`.
- `DELETE /api/me/push-devices/{id}`: revoke the caller's device.

Wallet receipts, invites and settlements persist notifications. Configured Firebase delivers new notifications to registered devices using HTTP v1, a durable delivery queue and up to five attempts. Devices registered later do not receive old notifications. The app should deduplicate by `notificationId` because an interrupted successful send can be retried. Delivery pauses while the free API sleeps. APNs/FCM client-side project configuration is still required.

## Test account and release

The private `.private/test-account.env` file contains a separate generated test email/password and opt-in provisioning settings. Import it into Render with your existing environment settings, deploy, and sign in using that file. It does not use or guess `hebronjesuloba@gmail.com` and never resets an existing real user's password. The test account is **not provisioned in the currently deployed service until those settings and this revision are deployed**.

The generated account has zero wallet balance, no paid-registration flag or bonus, and a private free room `qa-free-room` (question `qa-question-1`). Join it as player; the generated account is its host and can start a test round. There are no invented paid successes. Repeated startup does not reset passwords or balances. Disable `TestAccount__Enabled` after provisioning if desired; it controls seeding, not whether an existing account can sign in.

Release requires a schema migration. Build/tests run against a disposable PostgreSQL database using `TEST_DATABASE_URL`; tests create/drop only a dedicated randomly named temporary database, with actual database verification on every connection. GitHub Actions provisions PostgreSQL and executes the suite. `dotnet test tests/WhoSabi.Api.Tests/WhoSabi.Api.Tests.csproj -c Release` is the local command. Check the validation notes in `BACKEND_GAPS.md` for the actual results of this work.
