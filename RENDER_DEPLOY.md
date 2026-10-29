# Deploy WhoSabi: Render Free + Neon

1. Push this backend repository to GitHub. Do not commit `.env.render`.
2. In Render, choose New > Blueprint and connect the repository. The blueprint creates only the free Docker API; the database is hosted in Neon.
3. When prompted for DATABASE_URL, use the direct Neon URL from your private `.env.render`. The blueprint generates the JWT signing key and sets issuer/audience automatically.
4. Alternatively, create a Free Docker Web Service and import `.env.render` in its Environment settings. Use `/health` as the health check. The Dockerfile is at the repository root.
5. Deploy and visit `https://api.whosabi.ng/health`. Startup applies the checked-in EF migrations to Neon.

The existing Neon password is unchanged. DATABASE_URL is read directly by the backend, including sslmode and channel_binding. A separately configured ConnectionStrings__DefaultConnection takes precedence; remove any stale value in Render.

Render does not automatically load local .env files. Import the private file manually for a manually created service. Blueprint deployments generate their own JWT secret.

The public custom domain is `https://api.whosabi.ng`; Flutter must use it as `WHO_SABI_API_BASE_URL`. Localhost in launch profiles is only for source development. CORS is configured for `https://whosabi.ng` and `https://www.whosabi.ng`.

## Release the backend integration changes

The additive `BackendIntegration` database migration has already been applied during validation. Test-isolation cleanup and the remaining release work are recorded in `BACKEND_GAPS.md`. The new API code is not yet deployed.

1. Back up the database and run the PostgreSQL-backed tests before deploying this revision. A migration adds profile, room, chat, wallet-operation, arena and notification tables. It does not delete existing users or reset passwords. Unused sample rooms are moved to scheduled status; actual participants are counted and invented prize pools are cleared. Rooms with actual participation retain their existing status and require deliberate host assignment/reconciliation. Existing money entries are not rewritten.
2. Check for case-insensitive duplicate usernames before the migration: it adds username uniqueness. Resolve any real duplicates deliberately; the deployment should fail rather than rename users silently.
3. Import the private `.private/test-account.env` into Render alongside the existing database/JWT environment settings. This opt-in seed creates a dedicated zero-balance QA account and `qa-free-room` on startup. It refuses to overwrite a real account. Read the email/password privately from that file. Neon credentials stay unchanged.
4. Commit/push the source and migration; trigger Render deploy. Visit `/health`, `/api/config` and `/openapi/v1.json`. Use `scripts/smoke-live.ps1` to check the public test account and read routes after provisioning.
5. Hand `BACKEND_INTEGRATION.md` and `BACKEND_GAPS.md` to the frontend developer. They describe contract changes, provider gates and the work still required.

## Provider settings (secrets go in Render Environment)

| Integration | Environment settings |
|---|---|
| Monnify sandbox | `Monnify__ApiKey`, `Monnify__SecretKey`, `Monnify__ContractCode`, optional `Monnify__RedirectUrl` (default `https://whosabi.ng/payments/return`) |
| Google sign-in | `Google__ClientIds__0` and further numbered entries for approved OAuth clients |
| LiveKit media | `LiveKit__Url` (`wss://...`), `LiveKit__ApiKey`, `LiveKit__ApiSecret` |
| Firebase push | `Firebase__ProjectId`, `Firebase__ClientEmail`, `Firebase__PrivateKey` (PEM, literal `\n` supported) |
| Money gates | `Wallet__TransfersEnabled` defaults false; proposed limits can be overridden using `Wallet__MaxTransferKobo`, `Wallet__DailyTransferLimitKobo`, `Wallet__MaxDepositKobo` |

No provider credentials are currently supplied. Payment checkout, social login and media fail explicitly until configured. Withdrawals are not implemented beyond explicit unavailable responses. Referral rewards stay disabled. Live payments require explicit `Monnify__BaseUrl=https://api.monnify.com` and `Monnify__AllowLivePayments=true`; leave these disabled pending payout/reversal and bonus-policy work.

Free Render services sleep when idle, which delays round-closure broadcasts and push delivery. Persisted server deadlines still reject late answers. This setup is for development/testing and supports one SignalR API instance; scaling requires a backplane and a durable job deployment. Match Neon/Render regions when possible.
