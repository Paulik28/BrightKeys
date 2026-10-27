Add-Type -AssemblyName System.Drawing

$size = 32
$bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$accent = [System.Drawing.Color]::FromArgb(255, 255, 196, 48)
$sunBrush = New-Object System.Drawing.SolidBrush($accent)
$sunPen = New-Object System.Drawing.Pen($accent, 2)
# sun disc
$g.FillEllipse($sunBrush, 11, 9, 10, 10)
# rays
for ($i = 0; $i -lt 8; $i++) {
    $ang = $i * [Math]::PI / 4.0
    $x1 = 16 + [Math]::Cos($ang) * 8.0
    $y1 = 14 + [Math]::Sin($ang) * 8.0
    $x2 = 16 + [Math]::Cos($ang) * 11.0
    $y2 = 14 + [Math]::Sin($ang) * 11.0
    $g.DrawLine($sunPen, [float]$x1, [float]$y1, [float]$x2, [float]$y2)
}
# monitor outline
$whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 245, 245, 250), 2)
$g.DrawRectangle($whitePen, 4, 19, 24, 8)
$g.DrawLine($whitePen, 16, 27, 16, 29)
$g.DrawLine($whitePen, 11, 29, 21, 29)

$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$stride = $data.Stride
$pixels = New-Object byte[] ($stride * $size)
[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
$bmp.UnlockBits($data)
$g.Dispose(); $bmp.Dispose()

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
# ICONDIR
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]1)
# ICONDIRENTRY
$xorSize = $size * $size * 4
$andSize = $size * 4
$imageSize = 40 + $xorSize + $andSize
$bw.Write([byte]$size)   # width
$bw.Write([byte]$size)   # height
$bw.Write([byte]0)       # colors
$bw.Write([byte]0)       # reserved
$bw.Write([UInt16]1)     # planes
$bw.Write([UInt16]32)    # bpp
$bw.Write([UInt32]$imageSize)
$bw.Write([UInt32]22)    # offset
# BITMAPINFOHEADER
$bw.Write([UInt32]40)
$bw.Write([Int32]$size)
$bw.Write([Int32]($size * 2))
$bw.Write([UInt16]1)
$bw.Write([UInt16]32)
$bw.Write([UInt32]0)
$bw.Write([UInt32]$xorSize)
$bw.Write([Int32]0)
$bw.Write([Int32]0)
$bw.Write([UInt32]0)
$bw.Write([UInt32]0)
# XOR bitmap, bottom-up
for ($y = $size - 1; $y -ge 0; $y--) {
    $bw.Write($pixels, $y * $stride, $size * 4)
}
# AND mask (all zero)
$zeros = New-Object byte[] $andSize
$bw.Write($zeros, 0, $andSize)
$bw.Flush()

$outPath = Join-Path $PSScriptRoot 'src\app.ico'
[System.IO.File]::WriteAllBytes($outPath, $ms.ToArray())
$bw.Dispose(); $ms.Dispose()
Write-Host "Icon written to $outPath ($((Get-Item $outPath).Length) bytes)"
# sanity check: can it be loaded?
$ico = New-Object System.Drawing.Icon($outPath)
Write-Host ("Icon loaded OK: " + $ico.Width + "x" + $ico.Height)
$ico.Dispose()
