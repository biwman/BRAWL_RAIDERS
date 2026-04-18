Add-Type -AssemblyName System.Drawing

$files = @(
    "asteroida_1.png",
    "asteroida_2.png",
    "asteroida_3.png",
    "asteroida_podluzna_1.png",
    "asteroida_podluzna_2.png"
)

$base = "D:\ROZNE NOWE\PROJEKTY ROZNE\BRAWL_RAIDERS\BRAWL_RAIDERS\Assets"

function Get-ColorDistanceSq([System.Drawing.Color]$c1, [System.Drawing.Color]$c2) {
    $dr = [int]$c1.R - [int]$c2.R
    $dg = [int]$c1.G - [int]$c2.G
    $db = [int]$c1.B - [int]$c2.B
    return $dr * $dr + $dg * $dg + $db * $db
}

foreach ($name in $files) {
    $file = Join-Path $base $name
    if (-not (Test-Path $file)) {
        continue
    }

    $output = Join-Path $base (($name -replace "\.png$", "") + "_clean.png")
    $bitmap = New-Object System.Drawing.Bitmap($file)
    $width = $bitmap.Width
    $height = $bitmap.Height
    $backgroundColor = $bitmap.GetPixel(0, 0)

    $visited = New-Object "bool[,]" $width, $height
    $queue = New-Object "System.Collections.Generic.Queue[System.Drawing.Point]"
    $queue.Enqueue([System.Drawing.Point]::new(0, 0))
    $queue.Enqueue([System.Drawing.Point]::new($width - 1, 0))
    $queue.Enqueue([System.Drawing.Point]::new(0, $height - 1))
    $queue.Enqueue([System.Drawing.Point]::new($width - 1, $height - 1))

    while ($queue.Count -gt 0) {
        $point = $queue.Dequeue()
        $x = $point.X
        $y = $point.Y

        if ($x -lt 0 -or $x -ge $width -or $y -lt 0 -or $y -ge $height) {
            continue
        }

        if ($visited[$x, $y]) {
            continue
        }

        $visited[$x, $y] = $true
        $pixel = $bitmap.GetPixel($x, $y)

        if ((Get-ColorDistanceSq $pixel $backgroundColor) -gt (50 * 50)) {
            continue
        }

        $bitmap.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, $pixel.R, $pixel.G, $pixel.B))
        $queue.Enqueue([System.Drawing.Point]::new($x + 1, $y))
        $queue.Enqueue([System.Drawing.Point]::new($x - 1, $y))
        $queue.Enqueue([System.Drawing.Point]::new($x, $y + 1))
        $queue.Enqueue([System.Drawing.Point]::new($x, $y - 1))
    }

    $minX = $width
    $minY = $height
    $maxX = -1
    $maxY = -1

    for ($x = 0; $x -lt $width; $x++) {
        for ($y = 0; $y -lt $height; $y++) {
            $pixel = $bitmap.GetPixel($x, $y)
            if ($pixel.A -le 12) {
                continue
            }

            if ($x -lt $minX) { $minX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }

    $canvas = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    if ($maxX -ge $minX -and $maxY -ge $minY) {
        $cropWidth = $maxX - $minX + 1
        $cropHeight = $maxY - $minY + 1
        $scale = [Math]::Min(($width * 0.95) / $cropWidth, ($height * 0.95) / $cropHeight)
        $destWidth = [int][Math]::Round($cropWidth * $scale)
        $destHeight = [int][Math]::Round($cropHeight * $scale)
        $destX = [int][Math]::Round(($width - $destWidth) / 2.0)
        $destY = [int][Math]::Round(($height - $destHeight) / 2.0)

        $graphics.DrawImage(
            $bitmap,
            [System.Drawing.Rectangle]::new($destX, $destY, $destWidth, $destHeight),
            [System.Drawing.Rectangle]::new($minX, $minY, $cropWidth, $cropHeight),
            [System.Drawing.GraphicsUnit]::Pixel)
    }
    else {
        $graphics.DrawImage($bitmap, 0, 0)
    }

    $graphics.Dispose()
    $bitmap.Dispose()

    if (Test-Path $output) {
        Remove-Item -LiteralPath $output -Force
    }

    $canvas.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Dispose()
    Write-Output ("Created: " + $output)
}
