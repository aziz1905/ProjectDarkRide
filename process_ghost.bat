@echo off
echo ========================================================
echo Memproses PBR Karakter Horror / Ribcage Ghost (2K HD)...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792048142804_ad5818f7.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Ghost';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File tidak ditemukan'; exit };" ^
  "$bmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$W = $bmp.Width; $H = $bmp.Height;" ^
  "Write-Host 'Resolusi sumber:' $W 'x' $H;" ^
  "$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H);" ^
  "$data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$bytes = New-Object byte[] ($W * $H * 4);" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, ($W * $H * 4));" ^
  "$bmp.UnlockBits($data);" ^
  "$normMinX = $W; $normMaxX = 0; $normMinY = $H; $normMaxY = 0;" ^
  "for ($y = [int]($H * 0.28); $y -lt [int]($H * 0.65); $y++) {" ^
  "  for ($x = 0; $x -lt [int]($W * 0.5); $x++) {" ^
  "    $idx = ($y * $W + $x) * 4;" ^
  "    $b = [int]$bytes[$idx]; $g = [int]$bytes[$idx+1]; $r = [int]$bytes[$idx+2];" ^
  "    if ($b -gt 200 -and $r -gt 80 -and $r -lt 180 -and $g -gt 80 -and $g -lt 180) {" ^
  "      if ($x -lt $normMinX) { $normMinX = $x }; if ($x -gt $normMaxX) { $normMaxX = $x };" ^
  "      if ($y -lt $normMinY) { $normMinY = $y }; if ($y -gt $normMaxY) { $normMaxY = $y };" ^
  "    }" ^
  "  }" ^
  "};" ^
  "$boxW = ($normMaxX - $normMinX) + 1;" ^
  "$boxH = ($normMaxY - $normMinY) + 1;" ^
  "Write-Host 'Normal Map box terdeteksi: X=' $normMinX '..' $normMaxX ', Y=' $normMinY '..' $normMaxY ' (' $boxW 'x' $boxH ')';" ^
  "$col1X = $normMinX;" ^
  "$col2X = $W - $normMaxX - 1;" ^
  "if ($col2X -le $col1X) { $col2X = [int]($W * 0.525) };" ^
  "$row2Y = $normMinY;" ^
  "$row1Y = 0;" ^
  "for ($y = [int]($H * 0.01); $y -lt [int]($H * 0.15); $y++) {" ^
  "  $idx = ($y * $W + [int]($col1X + $boxW * 0.5)) * 4;" ^
  "  if ([int]$bytes[$idx+2] -gt 40 -or [int]$bytes[$idx+1] -gt 40) { $row1Y = $y; break };" ^
  "};" ^
  "if ($row1Y -eq 0) { $row1Y = [int]($H * 0.016) };" ^
  "$row3Y = 0;" ^
  "for ($y = [int]($normMaxY + $H * 0.05); $y -lt [int]($H * 0.75); $y++) {" ^
  "  $idx = ($y * $W + [int]($col1X + $boxW * 0.5)) * 4;" ^
  "  if ([int]$bytes[$idx] -gt 180 -and [int]$bytes[$idx+1] -gt 180 -and [int]$bytes[$idx+2] -gt 180) { $row3Y = $y; break };" ^
  "};" ^
  "if ($row3Y -eq 0) { $row3Y = [int]($H * 0.665) };" ^
  "Write-Host 'Row Y: R1=' $row1Y ', R2=' $row2Y ', R3=' $row3Y ', Col X: C1=' $col1X ', C2=' $col2X;" ^
  "function Save-Crop2K($src, $x, $y, $w, $h, $outPath) {" ^
  "  $sub = New-Object System.Drawing.Bitmap(2048, 2048, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($sub);" ^
  "  $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;" ^
  "  $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality;" ^
  "  $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality;" ^
  "  $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality;" ^
  "  $sr = New-Object System.Drawing.Rectangle($x, $y, $w, $h);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, 2048, 2048);" ^
  "  $g.DrawImage($src, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  $sub.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $sub.Dispose();" ^
  "  Write-Host 'Tersimpan 2K HD:' $outPath;" ^
  "};" ^
  "Save-Crop2K $bmp $col1X $row1Y $boxW $boxH ([System.IO.Path]::Combine($targetDir, 'Ghost_Albedo.png'));" ^
  "Save-Crop2K $bmp $col2X $row1Y $boxW $boxH ([System.IO.Path]::Combine($targetDir, 'Ghost_Roughness.png'));" ^
  "Save-Crop2K $bmp $col1X $row2Y $boxW $boxH ([System.IO.Path]::Combine($targetDir, 'Ghost_Normal.png'));" ^
  "Save-Crop2K $bmp $col2X $row2Y $boxW $boxH ([System.IO.Path]::Combine($targetDir, 'Ghost_Metallic.png'));" ^
  "Save-Crop2K $bmp $col1X $row3Y $boxW $boxH ([System.IO.Path]::Combine($targetDir, 'Ghost_AO.png'));" ^
  "Save-Crop2K $bmp $col2X $row3Y $boxW $boxH ([System.IO.Path]::Combine($targetDir, 'Ghost_Displacement.png'));" ^
  "$bmp.Dispose();" ^
  "Copy-Item ([System.IO.Path]::Combine($targetDir, '*.*')) 'c:\git\ProjectDarkRide\UVMapping\' -Force;"

REM Salin ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Ghost" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Ghost\*.*" "%%D\Ghost\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Ghost\*.*" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI! 6 PBR Maps Karakter Ghost 2K HD Berhasil Dibuat:
echo.
echo  - Ghost_Albedo.png       [2048 x 2048] (Warna baju darah, tulang rusuk, rambut)
echo  - Ghost_Roughness.png    [2048 x 2048] (Kilap darah basah vs kain robek matte)
echo  - Ghost_Normal.png       [2048 x 2048] (Relief 3D tulang rusuk, baju robek)
echo  - Ghost_Metallic.png     [2048 x 2048] (Peta non-metal solid)
echo  - Ghost_AO.png           [2048 x 2048] (Ambient Occlusion rongga tulang dada)
echo  - Ghost_Displacement.png [2048 x 2048] (Kedalaman rongga dada berlubang)
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Ghost\
echo ========================================================
pause
