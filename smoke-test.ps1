$ErrorActionPreference = "Continue"
$base = "http://localhost:5099"
$json = "application/json"

function Req($method, $path, $body) {
    $params = @{ Uri = "$base$path"; Method = $method; ContentType = $json; UseBasicParsing = $true }
    if ($body) { $params.Body = ($body | ConvertTo-Json -Depth 5) }
    try {
        $resp = Invoke-WebRequest @params
        Write-Output "$method $path -> $($resp.StatusCode)"
        if ($resp.Content) { return ($resp.Content | ConvertFrom-Json) }
    } catch {
        $r = $_.Exception.Response
        $code = 0
        $body = ""
        if ($r) {
            try { $code = [int]$r.StatusCode } catch {}
            try {
                $stream = $r.GetResponseStream()
                $stream.Position = 0
                $sr = New-Object System.IO.StreamReader($stream)
                $body = $sr.ReadToEnd()
            } catch {}
        }
        Write-Output "$method $path -> $code"
        if ($body) { Write-Output "  body: $body" }
        return $null
    }
}

Write-Output "=== 1. Create user ==="
$u = Req POST "/users" @{ email = "alice@example.com"; displayName = "Alice" }
$u | ConvertTo-Json -Depth 3 -Compress
$userId = $u.id

Write-Output "`n=== 2. Duplicate user -> expect 409 ==="
Req POST "/users" @{ email = "alice@example.com"; displayName = "Alice2" } | Out-Null

Write-Output "`n=== 3. Invalid user (no email) -> expect 400 ==="
Req POST "/users" @{ email = ""; displayName = "X" } | Out-Null

Write-Output "`n=== 4. Create project ==="
$p = Req POST "/projects" @{ ownerId = $userId; name = "First Sprint" }
$p | ConvertTo-Json -Depth 3 -Compress
$projectId = $p.id

Write-Output "`n=== 5. Create task ==="
$t = Req POST "/projects/$projectId/tasks" @{ title = "Buy milk"; description = "2 liters" }
$t | ConvertTo-Json -Depth 3 -Compress
$taskId = $t.id

Write-Output "`n=== 6. Start task ==="
Req POST "/tasks/$taskId/start" $null | ConvertTo-Json -Depth 3 -Compress

Write-Output "`n=== 7. Add tag ==="
Req POST "/tasks/$taskId/tags" @{ name = "Urgent" } | ConvertTo-Json -Depth 3 -Compress

Write-Output "`n=== 8. Complete task ==="
Req POST "/tasks/$taskId/complete" $null | ConvertTo-Json -Depth 3 -Compress

Write-Output "`n=== 9. Try to Start after Done -> expect 400 (DomainException) ==="
Req POST "/tasks/$taskId/start" $null | Out-Null

Write-Output "`n=== 10. List tasks by project ==="
Req GET "/projects/$projectId/tasks" $null | ConvertTo-Json -Depth 4 -Compress

Write-Output "`n=== 11. Filter by invalid status -> expect 400 ==="
Req GET "/projects/$projectId/tasks?status=Bogus" $null | Out-Null

Write-Output "`n=== 12. Get non-existent task -> expect 404 ==="
Req GET "/tasks/00000000-0000-0000-0000-000000000000" $null | Out-Null

Write-Output "`n=== Done ==="
