@echo off
echo ========================================================
echo Memproses Noda / Cipratan (Splatter Decal) Transparan...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792082757173_0c96100d.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Decal';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File tidak ditemukan'; exit };" ^
  "$srcBmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$W = $srcBmp.Width; $H = $srcBmp.Height;" ^
  "Write-Host 'Resolusi gambar:' $W 'x' $H;" ^
  "$outBmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H);" ^
  "$srcData = $srcBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$dstData = $outBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$totalBytes = $W * $H * 4;" ^
  "$sBytes = New-Object byte[] $totalBytes;" ^
  "$dBytes = New-Object byte[] $totalBytes;" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($srcData.Scan0, $sBytes, 0, $totalBytes);" ^
  "for ($i = 0; $i -lt $totalBytes; $i += 4) {" ^
  "  $b = [int]$sBytes[$i]; $g = [int]$sBytes[$i+1]; $r = [int]$sBytes[$i+2];" ^
  "  if ($r -gt 242 -and $g -gt 242 -and $b -gt 242) {" ^
  "    $a = 0;" ^
  "  } else {" ^
  "    $whiteness = [Math]::Min($r, [Math]::Min($g, $b));" ^
  "    $alphaVal = [int]((255 - $whiteness) * 2.8);" ^
  "    $a = [byte][Math]::Min(255, [Math]::Max(0, $alphaVal));" ^
  "    if ($a -lt 25) { $a = 0 };" ^
  "  };" ^
  "  $dBytes[$i]     = $b;" ^
  "  $dBytes[$i + 1] = $g;" ^
  "  $dBytes[$i + 2] = $r;" ^
  "  $dBytes[$i + 3] = [byte]$a;" ^
  "};" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($dBytes, 0, $dstData.Scan0, $totalBytes);" ^
  "$srcBmp.UnlockBits($srcData);" ^
  "$outBmp.UnlockBits($dstData);" ^
  "$outBmp.Save([System.IO.Path]::Combine($targetDir, 'Splatter_Stain_Transparan.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$outBmp.Save('c:\git\ProjectDarkRide\UVMapping\Splatter_Stain_Transparan.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$srcBmp.Dispose();" ^
  "$outBmp.Dispose();" ^
  "Write-Host 'Berhasil disimpan sebagai PNG Transparan Halus!';"

REM Salin ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Decal" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Decal\*.*" "%%D\Decal\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Splatter_Stain_Transparan.png" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI! Gambar Cipratan / Noda Transparan Berhasil Dibuat:
echo.
echo  - Splatter_Stain_Transparan.png
echo.
echo (Hanya 1 file PNG transparan murni, tanpa set PBR)
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Decal\
echo ========================================================
pause
