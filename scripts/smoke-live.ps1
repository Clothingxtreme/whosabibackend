param(
    [string]$BaseUrl = 'https://api.whosabi.ng',
    [string]$CredentialsFile = "$PSScriptRoot/../.private/test-account.env"
)
$ErrorActionPreference = 'Stop'
$settings = @{}
foreach ($line in [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $CredentialsFile))) {
    if ($line -match '^([^#=]+)=(.*)$') { $settings[$matches[1]] = $matches[2] }
}
if (-not $settings['TestAccount__Email'] -or -not $settings['TestAccount__Password']) { throw 'Private test-account settings are missing.' }
$health = Invoke-RestMethod "$BaseUrl/health" -TimeoutSec 90
if ($health.status -ne 'ok') { throw 'Health check failed.' }
$config = Invoke-RestMethod "$BaseUrl/api/config" -TimeoutSec 30
if ($config.apiBaseUrl -ne $BaseUrl.TrimEnd('/')) { throw 'Public API config mismatch.' }
$body = @{ email = $settings['TestAccount__Email']; password = $settings['TestAccount__Password'] } | ConvertTo-Json
$login = Invoke-RestMethod "$BaseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 30
if (-not $login.accessToken) { throw 'Login did not return an access token.' }
$headers = @{ Authorization = 'Bearer ' + $login.accessToken }
foreach ($route in @('/api/me','/api/me/profile','/api/rooms/live','/api/wallet','/api/wallet/transactions','/api/rooms/qa-free-room','/api/notifications/unread-count')) {
    $null = Invoke-RestMethod ($BaseUrl + $route) -Headers $headers -TimeoutSec 30
    Write-Output ('PASS ' + $route)
}
# This script performs reads plus login only. It never initiates a payment or submits an answer.
Write-Output 'Live test-account authentication and read flows passed. No credentials or tokens were printed.'
