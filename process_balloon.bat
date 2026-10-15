@echo off
echo ========================================================
echo Memproses PBR Texture Balon Merah (Red Balloon)...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792045003810_3927a046.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Balloon';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File tidak ditemukan'; exit };" ^
  "$bmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$W = $bmp.Width; $H = $bmp.Height;" ^
  "Write-Host 'Resolusi sumber:' $W 'x' $H;" ^
  "$pbrX = [int]($W * 0.677); $pbrW = [int]($W * 0.32);" ^
  "$col1X = [int]($W * 0.700); $col2X = [int]($W * 0.850);" ^
  "$row1Y = [int]($H * 0.200);" ^
  "$row2Y = [int]($H * 0.468);" ^
  "$row3Y = [int]($H * 0.730);" ^
  "$sphereSize = [int]($W * 0.117);" ^
  "function Crop-Sphere2K($src, $x, $y, $s, $outPath) {" ^
  "  $sub = New-Object System.Drawing.Bitmap(2048, 2048, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($sub);" ^
  "  $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;" ^
  "  $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality;" ^
  "  $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality;" ^
  "  $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality;" ^
  "  $sr = New-Object System.Drawing.Rectangle($x, $y, $s, $s);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, 2048, 2048);" ^
  "  $g.DrawImage($src, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  $sub.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $sub.Dispose();" ^
  "  Write-Host 'Tersimpan 2K HD:' $outPath;" ^
  "};" ^
  "Crop-Sphere2K $bmp $col1X $row1Y $sphereSize ([System.IO.Path]::Combine($targetDir, 'Balloon_Albedo.png'));" ^
  "Crop-Sphere2K $bmp $col2X $row1Y $sphereSize ([System.IO.Path]::Combine($targetDir, 'Balloon_Roughness.png'));" ^
  "Crop-Sphere2K $bmp $col1X $row2Y $sphereSize ([System.IO.Path]::Combine($targetDir, 'Balloon_Normal.png'));" ^
  "Crop-Sphere2K $bmp $col2X $row2Y $sphereSize ([System.IO.Path]::Combine($targetDir, 'Balloon_Metallic.png'));" ^
  "Crop-Sphere2K $bmp $col1X $row3Y $sphereSize ([System.IO.Path]::Combine($targetDir, 'Balloon_AO.png'));" ^
  "Crop-Sphere2K $bmp $col2X $row3Y $sphereSize ([System.IO.Path]::Combine($targetDir, 'Balloon_Translucency.png'));" ^
  "$bmp.Dispose();" ^
  "function Generate-UV-PBR($outDir) {" ^
  "  $sz = 2048;" ^
  "  Write-Host 'Membuat texture map UV 2K balon (Albedo, Normal, Roughness)...';" ^
  "  $alb = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($alb);" ^
  "  $redBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 198, 24, 24));" ^
  "  $g.FillRectangle($redBrush, 0, 0, $sz, $sz);" ^
  "  $g.Dispose();" ^
  "  $alb.Save([System.IO.Path]::Combine($outDir, 'Balloon_UV_Albedo.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $alb.Dispose();" ^
  "  $norm = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $rect = New-Object System.Drawing.Rectangle(0, 0, $sz, $sz);" ^
  "  $nData = $norm.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $nBytes = New-Object byte[] ($sz * $sz * 4);" ^
  "  for ($y = 0; $y -lt $sz; $y++) {" ^
  "    $nyRel = $y / $sz;" ^
  "    for ($x = 0; $x -lt $sz; $x++) {" ^
  "      $idx = ($y * $sz + $x) * 4;" ^
  "      $wrinkle = 0.0;" ^
  "      if ($nyRel -gt 0.75) {" ^
  "        $freq = ($nyRel - 0.75) / 0.25;" ^
  "        $wrinkle = [Math]::Sin(($x / $sz) * 60.0 * 3.14159) * $freq * 0.4;" ^
  "      }" ^
  "      $nx = $wrinkle;" ^
  "      $ny = 0.0;" ^
  "      $nz = [Math]::Sqrt([Math]::Max(0.0, 1.0 - $nx*$nx));" ^
  "      $nBytes[$idx]     = [byte][Math]::Round(($nz * 0.5 + 0.5) * 255);" ^
  "      $nBytes[$idx + 1] = 128;" ^
  "      $nBytes[$idx + 2] = [byte][Math]::Round(($nx * 0.5 + 0.5) * 255);" ^
  "      $nBytes[$idx + 3] = 255;" ^
  "    }" ^
  "  }" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($nBytes, 0, $nData.Scan0, ($sz * $sz * 4));" ^
  "  $norm.UnlockBits($nData);" ^
  "  $norm.Save([System.IO.Path]::Combine($outDir, 'Balloon_UV_Normal.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $norm.Dispose();" ^
  "  $rou = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g2 = [System.Drawing.Graphics]::FromImage($rou);" ^
  "  $rouBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 60, 60, 60));" ^
  "  $g2.FillRectangle($rouBrush, 0, 0, $sz, $sz);" ^
  "  $g2.Dispose();" ^
  "  $rou.Save([System.IO.Path]::Combine($outDir, 'Balloon_UV_Roughness.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $rou.Dispose();" ^
  "};" ^
  "Generate-UV-PBR $targetDir;" ^
  "Copy-Item ([System.IO.Path]::Combine($targetDir, '*.*')) 'c:\git\ProjectDarkRide\UVMapping\' -Force;"

REM Salin ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Balloon" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Balloon\*.*" "%%D\Balloon\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Balloon\*.*" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI! Set PBR Balon Merah (Balloon) Berhasil Dibuat:
echo.
echo  - Balloon_Albedo.png       (Warna karet lateks merah terang 2K)
echo  - Balloon_Normal.png       (Detail 3D lipatan/kerutan simpul balon 2K)
echo  - Balloon_Roughness.png    (Peta kilap lateks karet halus 2K)
echo  - Balloon_Metallic.png     (Peta non-metalik lateks 2K)
echo  - Balloon_AO.png           (Ambient Occlusion rongga simpul balon 2K)
echo  - Balloon_Translucency.png (Peta tembus cahaya/subsurface scattering 2K)
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Balloon\
echo ========================================================
pause
