# Requires: winwork-logo.png in the current directory
# Output: winwork-logo.ico with 16x16, 32x32, 48x48, 256x256 sizes
Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 256)
$iconStreams = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage([System.Drawing.Image]::FromFile('winwork-logo.png'), 0, 0, $size, $size)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $iconStreams += ,@($ms.ToArray(), $size)
    $bmp.Dispose()
}

# Write ICO header
$fs = [System.IO.File]::Open('src/WinWork.UI/winwork-logo.ico', [System.IO.FileMode]::Create)
$fs.WriteByte(0); $fs.WriteByte(0) # Reserved
$fs.WriteByte(1); $fs.WriteByte(0) # ICO type
$fs.WriteByte($sizes.Count); $fs.WriteByte(0) # Number of images

$offset = 6 + 16 * $sizes.Count
foreach ($entry in $iconStreams) {
    $img = $entry[0]; $size = $entry[1]
    $w = if ($size -eq 256) { 0 } else { $size }
    $fs.WriteByte([byte]$w) # Width
    $fs.WriteByte([byte]$w) # Height
    $fs.WriteByte(0) # Colors
    $fs.WriteByte(0) # Reserved
    $fs.WriteByte(1); $fs.WriteByte(0) # Color planes
    $fs.WriteByte(32); $fs.WriteByte(0) # Bits per pixel
    $fs.Write([BitConverter]::GetBytes($img.Length), 0, 4)
    $fs.Write([BitConverter]::GetBytes($offset), 0, 4)
    $offset += $img.Length
}
foreach ($entry in $iconStreams) {
    $img = $entry[0]
    $fs.Write($img, 0, $img.Length)
}
$fs.Close()
Write-Host "Multi-size ICO generated: src/WinWork.UI/winwork-logo.ico"