@echo off
echo ========================================================
echo Memproses Plafon Baru: Plaster/Tiles Baru + Wood Ceiling
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$woodGridPath    = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1791792532770_ba5ba5b6.png';" ^
  "$woodNormPath    = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\ceiling_wood_normal_1791792809752.jpg';" ^
  "$newPlasterPath  = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1791794229091_ce6beac6.png';" ^
  "$targetDir       = 'c:\git\ProjectDarkRide\UVMapping\Ceiling';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "function Crop-Rect($srcBmp, $x, $y, $cw, $ch) {" ^
  "  $res = New-Object System.Drawing.Bitmap($cw, $ch, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($res);" ^
  "  $sr = New-Object System.Drawing.Rectangle($x, $y, $cw, $ch);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, $cw, $ch);" ^
  "  $g.DrawImage($srcBmp, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  return $res;" ^
  "};" ^
  "function Make-Grayscale($srcBmp, $invert) {" ^
  "  $cw = $srcBmp.Width; $ch = $srcBmp.Height;" ^
  "  $res = New-Object System.Drawing.Bitmap($cw, $ch, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $rect = New-Object System.Drawing.Rectangle(0, 0, $cw, $ch);" ^
  "  $srcData = $srcBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $dstData = $res.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $bytes = $cw * $ch * 4;" ^
  "  $sBytes = New-Object byte[] $bytes;" ^
  "  $dBytes = New-Object byte[] $bytes;" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($srcData.Scan0, $sBytes, 0, $bytes);" ^
  "  for ($i = 0; $i -lt $bytes; $i += 4) {" ^
  "    $lum = [byte](($sBytes[$i] * 0.11) + ($sBytes[$i+1] * 0.59) + ($sBytes[$i+2] * 0.3));" ^
  "    if ($invert) { $lum = [byte](255 - $lum) };" ^
  "    $dBytes[$i] = $lum; $dBytes[$i+1] = $lum; $dBytes[$i+2] = $lum; $dBytes[$i+3] = 255;" ^
  "  }" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($dBytes, 0, $dstData.Scan0, $bytes);" ^
  "  $srcBmp.UnlockBits($srcData); $res.UnlockBits($dstData);" ^
  "  return $res;" ^
  "};" ^
  "Write-Host '--- 1. MEMPROSES PLAFON PLASTER / TILES BARU ---';" ^
  "$pBmp = [System.Drawing.Bitmap]::FromFile($newPlasterPath);" ^
  "$pw = $pBmp.Width; $ph = $pBmp.Height;" ^
  "$phw = [int]($pw / 2); $phh = [int]($ph / 2);" ^
  "$pAlb = Crop-Rect $pBmp 0 0 $phw $phh;" ^
  "$pAlb.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Plaster_Albedo.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$pNorm = Crop-Rect $pBmp $phw 0 ($pw - $phw) $phh;" ^
  "$pNorm.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Plaster_Normal.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$pRough = Crop-Rect $pBmp 0 $phh $phw ($ph - $phh);" ^
  "$pRough.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Plaster_Roughness.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$pAo = Crop-Rect $pBmp $phw $phh ($pw - $phw) ($ph - $phh);" ^
  "$pAo.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Plaster_AO.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$pBmp.Dispose(); $pAlb.Dispose(); $pNorm.Dispose(); $pRough.Dispose(); $pAo.Dispose();" ^
  "Write-Host '--- 2. MEMPROSES PLAFON KAYU (WOOD) ---';" ^
  "$wBmp = [System.Drawing.Bitmap]::FromFile($woodGridPath);" ^
  "$ww = $wBmp.Width; $wh = $wBmp.Height;" ^
  "$whw = [int]($ww / 2); $whh = [int]($wh / 2);" ^
  "$wAlb = Crop-Rect $wBmp 0 0 $whw $whh;" ^
  "$wAlb.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Wood_Albedo.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$wRough = Crop-Rect $wBmp 0 $whh $whw ($wh - $whh);" ^
  "$wRough.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Wood_Roughness.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "if (Test-Path $woodNormPath) { Copy-Item $woodNormPath ([System.IO.Path]::Combine($targetDir, 'Ceiling_Wood_Normal.png')) -Force };" ^
  "$wAo = Make-Grayscale $wAlb $true;" ^
  "$wAo.Save([System.IO.Path]::Combine($targetDir, 'Ceiling_Wood_AO.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$wAo.Dispose(); $wBmp.Dispose(); $wAlb.Dispose(); $wRough.Dispose();" ^
  "Write-Host 'Semua 8 file tekstur PBR berhasil diperbarui bersih 100%!';"

REM Salin otomatis ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Ceiling" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Ceiling\*.*" "%%D\Ceiling\" >nul
        echo Berhasil disalin ke Unity: %%D\Ceiling
    )
)

echo.
echo ========================================================
echo SELESAI! Plafon Plaster/Tile Baru Berhasil Diperbarui:
echo.
echo [PLAFON PLASTER / ACOUSTIC TILES BARU]
echo  - Ceiling_Plaster_Albedo.png
echo  - Ceiling_Plaster_Normal.png
echo  - Ceiling_Plaster_Roughness.png
echo  - Ceiling_Plaster_AO.png
echo.
echo [PLAFON KAYU / WOOD]
echo  - Ceiling_Wood_Albedo.png
echo  - Ceiling_Wood_Normal.png
echo  - Ceiling_Wood_Roughness.png
echo  - Ceiling_Wood_AO.png
echo ========================================================
pause
