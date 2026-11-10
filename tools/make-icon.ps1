<#
.SYNOPSIS
    Renders the PinTop app icon (a pushpin on a violet tile) and writes it as a
    multi-size .ico file plus a 256px PNG preview.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
#>
param(
    [string]$IcoPath = (Join-Path $PSScriptRoot '..\src\PinTop\Resources\app.ico'),
    [string]$PngPath = (Join-Path $PSScriptRoot '..\docs\icon.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius)

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-PinBitmap {
    param([int]$Size)

    $scale = $Size / 64.0
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad = [float]($Size * 0.035)
    $rect = [System.Drawing.RectangleF]::new($pad, $pad, [float]($Size - 2 * $pad), [float]($Size - 2 * $pad))
    $radius = [float]($Size * 0.24)

    $tile = New-RoundedRectPath -X $rect.X -Y $rect.Y -Width $rect.Width -Height $rect.Height -Radius $radius
    $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 139, 106, 255),
        [System.Drawing.Color]::FromArgb(255, 71, 41, 196),
        55.0)
    $g.FillPath($brush, $tile)

    $white = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
    $pinBrush = [System.Drawing.SolidBrush]::new($white)

    # A window with an upward arrow: "this window goes on top".
    $window = New-RoundedRectPath -X ([float](13.0 * $scale)) -Y ([float](15.0 * $scale)) -Width ([float](38.0 * $scale)) -Height ([float](34.0 * $scale)) -Radius ([float](6.0 * $scale))
    $g.FillPath($pinBrush, $window)

    $arrow = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $arrowPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new([float](28.0 * $scale), [float](33.0 * $scale)),
        [System.Drawing.PointF]::new([float](28.0 * $scale), [float](44.0 * $scale)),
        [System.Drawing.PointF]::new([float](36.0 * $scale), [float](44.0 * $scale)),
        [System.Drawing.PointF]::new([float](36.0 * $scale), [float](33.0 * $scale)),
        [System.Drawing.PointF]::new([float](44.0 * $scale), [float](33.0 * $scale)),
        [System.Drawing.PointF]::new([float](32.0 * $scale), [float](20.0 * $scale)),
        [System.Drawing.PointF]::new([float](20.0 * $scale), [float](33.0 * $scale))
    )
    $arrow.AddPolygon($arrowPoints)
    $g.FillPath($brush, $arrow)

    $pinBrush.Dispose()
    $arrow.Dispose()
    $window.Dispose()
    $brush.Dispose()
    $tile.Dispose()
    $g.Dispose()

    return $bitmap
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[object]]::new()

foreach ($size in $sizes) {
    $bitmap = New-PinBitmap -Size $size
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add([pscustomobject]@{ Size = $size; Data = $stream.ToArray() })
    $stream.Dispose()
    $bitmap.Dispose()
}

$icoDirectory = Split-Path -Parent $IcoPath
if (-not (Test-Path $icoDirectory)) { New-Item -ItemType Directory -Path $icoDirectory -Force | Out-Null }
$pngDirectory = Split-Path -Parent $PngPath
if (-not (Test-Path $pngDirectory)) { New-Item -ItemType Directory -Path $pngDirectory -Force | Out-Null }

$buffer = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($buffer)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$images.Count)

$offset = 6 + 16 * $images.Count
foreach ($image in $images) {
    $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$image.Data.Length)
    $writer.Write([uint32]$offset)
    $offset += $image.Data.Length
}

foreach ($image in $images) {
    $writer.Write($image.Data)
}

$writer.Flush()
[System.IO.File]::WriteAllBytes($IcoPath, $buffer.ToArray())
$writer.Dispose()
$buffer.Dispose()

$preview = New-PinBitmap -Size 256
$preview.Save($PngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

Write-Host "Wrote $IcoPath"
Write-Host "Wrote $PngPath"
