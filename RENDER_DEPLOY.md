# Deploying WhoSabi to Render

1. Push this repository to GitHub.
2. In Render, select **New > Blueprint** and connect the repository.
3. Render detects `render.yaml`. Confirm the Blueprint to create:
   - `whosabi-api` (Docker web service)
   - `whosabi-db` (PostgreSQL)
4. Click **Apply**. Render builds the Docker image, supplies the database
   connection string, generates the JWT signing key, and runs the EF Core
   migrations during application startup.
5. After the service is live, open `https://<your-service>.onrender.com/health`.
   It should return `{ "status": "ok" }`.

## Environment variables

The Blueprint sets these automatically:

- `ConnectionStrings__DefaultConnection`
- `Jwt__SigningKey`
- `Jwt__Issuer`
- `Jwt__Audience`

If a browser frontend is deployed separately, add its public origin to the API's
CORS configuration before using authenticated API calls. Keep `Jwt__SigningKey`
secret; changing it invalidates existing login tokens.
