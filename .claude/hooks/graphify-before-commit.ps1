[Console]::In.ReadToEnd() | Out-Null
$projectDirectory = $env:CLAUDE_PROJECT_DIR
if ([string]::IsNullOrWhiteSpace($projectDirectory)) {
  [Console]::Error.WriteLine('CLAUDE_PROJECT_DIR is not set.')
  exit 2
}
if (-not (Get-Command graphify -ErrorAction SilentlyContinue)) {
  [Console]::Error.WriteLine('Graphify is not installed or is not on PATH.')
  exit 2
}
Push-Location $projectDirectory
try {
  $output = & graphify update . 2>&1
  if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine(($output -join [Environment]::NewLine))
    exit 2
  }
} finally {
  Pop-Location
}
exit 0
