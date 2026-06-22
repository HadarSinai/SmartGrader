$ErrorActionPreference = 'Stop'

$file = Join-Path (Get-Location).Path 'Api\appsettings.Development.json'

if (Test-Path $file) {
    $content = Get-Content -LiteralPath $file -Raw
    # Blank any ApiKey value (even if previously a secret)
    $updated = [regex]::Replace($content, '"ApiKey"\s*:\s*"[^"]*"', '"ApiKey": ""')

    if ($updated -ne $content) {
        Set-Content -LiteralPath $file -Value $updated -Encoding UTF8
    }
}
