Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO

$img = [System.Drawing.Image]::FromFile('razer-logo.png')

$iconSizes = @(16, 32, 48, 256)
$ms = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($ms)

$writer.Write([ushort]0)
$writer.Write([ushort]1)
$writer.Write([ushort]$iconSizes.Count)

$imageDataOffset = 6 + $iconSizes.Count * 16
$imageData = @()

foreach ($size in $iconSizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($img, 0, 0, $size, $size)
    $g.Dispose()
    
    $bmpMs = New-Object System.IO.MemoryStream
    $bmp.Save($bmpMs, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $bmpMs.ToArray()
    $imageData += $bytes
    $bmp.Dispose()
    $bmpMs.Dispose()
}

$offset = $imageDataOffset
for ($i = 0; $i -lt $iconSizes.Count; $i++) {
    $size = $iconSizes[$i]
    $bytes = $imageData[$i]
    $writer.Write([byte]$size)
    $writer.Write([byte]$size)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([ushort]1)
    $writer.Write([ushort]32)
    $writer.Write([uint]$bytes.Length)
    $writer.Write([uint]$offset)
    $offset += $bytes.Length
}

foreach ($bytes in $imageData) {
    $writer.Write($bytes)
}

$writer.Flush()
$ms.Position = 0

$iconBytes = $ms.ToArray()
[System.IO.File]::WriteAllBytes('razer-logo.ico', $iconBytes)

$img.Dispose()
$ms.Dispose()
$writer.Dispose()

Write-Host 'Created razer-logo.ico with multiple resolutions'