$canvasPath = if ($env:ABD_CANVAS_PATH) { $env:ABD_CANVAS_PATH } else { "C:\dev\abd-canvas" }

if (-not (Test-Path -LiteralPath $canvasPath)) {
    Write-Error "abd-canvas not found at '$canvasPath'. Set `$env:ABD_CANVAS_PATH or clone https://github.com/<owner>/abd-canvas to C:\dev\abd-canvas."
    exit 1
}

Set-Location -LiteralPath $canvasPath

if (-not (Test-Path -LiteralPath (Join-Path $canvasPath "node_modules"))) {
    Write-Host "Installing abd-canvas dependencies..."
    npm install
}

npm run dev
