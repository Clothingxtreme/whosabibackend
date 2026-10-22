# Deploy WhoSabi: Render Free + Neon

1. Push this backend repository to GitHub. Do not commit `.env.render`.
2. In Render, choose New > Blueprint and connect the repository. The blueprint creates only the free Docker API; the database is hosted in Neon.
3. When prompted for DATABASE_URL, use the direct Neon URL from your private `.env.render`. The blueprint generates the JWT signing key and sets issuer/audience automatically.
4. Alternatively, create a Free Docker Web Service and import `.env.render` in its Environment settings. Use `/health` as the health check. The Dockerfile is at the repository root.
5. Deploy and visit `https://<service>.onrender.com/health`. Startup applies the checked-in EF migrations to Neon.

The existing Neon password is unchanged. DATABASE_URL is read directly by the backend, including sslmode and channel_binding. A separately configured ConnectionStrings__DefaultConnection takes precedence; remove any stale value in Render.

Render does not automatically load local .env files. Import the private file manually for a manually created service. Blueprint deployments generate their own JWT secret.

Free Render services sleep when idle. Configure CORS for the frontend's actual origin before browser integration. Payment confirmation still requires server-side provider verification before real payments.
