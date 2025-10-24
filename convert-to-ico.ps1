Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

# Load the original PNG
$originalImage = [System.Drawing.Image]::FromFile("winwork-logo.png")

# Create different sizes for the ICO file
$sizes = @(16, 32, 48, 64, 128, 256)
$icons = @()

foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($originalImage, 0, 0, $size, $size)
    $graphics.Dispose()
    
    $iconHandle = $bitmap.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
    $icons += $icon
    
    # Clean up
    $bitmap.Dispose()
}

# Save the first (32x32) icon as our ICO file
$icons[1].Save([System.IO.FileStream]::new("src\WinWork.UI\winwork-logo-better.ico", [System.IO.FileMode]::Create))

Write-Host "Better ICO file created: winwork-logo-better.ico"

# Clean up
$originalImage.Dispose()
foreach ($icon in $icons) {
    $icon.Dispose()
}