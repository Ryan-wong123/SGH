param(
    [Parameter(Mandatory)][Uri]$BaseUri,
    [switch]$IncludeRateLimit
)
$ErrorActionPreference = 'Stop'
# Only invalid synthetic uploads are submitted: this script never exercises the
# SMTP success branch or reads a gameplay CSV. Run against a dedicated test API.
if ($BaseUri.Scheme -ne 'https' -and -not ($BaseUri.Scheme -eq 'http' -and $BaseUri.IsLoopback)) {
    throw 'Use HTTPS, or HTTP loopback for a local development API.'
}
Add-Type -AssemblyName System.Net.Http
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.BaseAddress = [Uri]::new($BaseUri.AbsoluteUri.TrimEnd('/') + '/')
$client.Timeout = [TimeSpan]::FromSeconds(20)
function Assert-Response($response, [int]$expected, [string]$label) {
    try {
        if ([int]$response.StatusCode -ne $expected) {
            throw "$label expected HTTP $expected, got $([int]$response.StatusCode). Check TLS configuration and use a fresh rate-limit window."
        }
        Write-Host "PASS $label"
    } finally { $response.Dispose() }
}
function Send-InvalidUpload([string]$filename, [int]$bytes, [string]$fieldName = 'csv') {
    $form = [System.Net.Http.MultipartFormDataContent]::new()
    try {
        $form.Add([System.Net.Http.StringContent]::new('synthetic-room'), 'roomName')
        $form.Add([System.Net.Http.StringContent]::new('2026-10-30T00:00:00Z'), 'playedAt')
        $content = [System.Net.Http.ByteArrayContent]::new([byte[]]::new($bytes))
        $form.Add($content, $fieldName, $filename)
        return $client.PostAsync('api/gameplay-csv', $form).GetAwaiter().GetResult()
    } finally { $form.Dispose() }
}
try {
    $health = $client.GetAsync('health').GetAwaiter().GetResult()
    try {
        if ([int]$health.StatusCode -ne 200) { throw "Health returned $([int]$health.StatusCode)" }
        $body = $health.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
        if ($body.status -ne 'ok') { throw 'Unexpected health response' }
        Write-Host 'PASS health'
    } finally { $health.Dispose() }
    $json = [System.Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    try { Assert-Response ($client.PostAsync('api/gameplay-csv', $json).GetAwaiter().GetResult()) 400 'non-form body' }
    finally { $json.Dispose() }
    Assert-Response (Send-InvalidUpload 'synthetic.csv' 0) 400 'empty CSV'
    Assert-Response (Send-InvalidUpload 'synthetic.txt' 1) 400 'wrong extension'
    Assert-Response (Send-InvalidUpload 'synthetic.csv' 1 'wrong-field') 400 'missing csv field'
    Assert-Response (Send-InvalidUpload 'synthetic.csv' (5 * 1024 * 1024 + 1)) 400 'over 5 MiB'
    if ($IncludeRateLimit) {
        $rejected = $false
        for ($i = 0; $i -lt 25; $i++) {
            $response = Send-InvalidUpload 'synthetic.txt' 1
            try {
                if ([int]$response.StatusCode -eq 429) { $rejected = $true; break }
                if ([int]$response.StatusCode -ne 400) { throw "Unexpected status: $([int]$response.StatusCode)" }
            } finally { $response.Dispose() }
        }
        if (-not $rejected) { throw 'No 429 response observed; retry in a fresh fixed-window interval.' }
        Write-Host 'PASS fixed-window rejection'
    }
} finally { $client.Dispose(); $handler.Dispose() }
