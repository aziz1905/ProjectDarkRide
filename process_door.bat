@echo off
echo ========================================================
echo Memotong Texture Grid Pintu Rumah Sakit / Staff Door...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath   = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1791817984734_4ce3a80b.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Door';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File sumber tidak ditemukan!'; exit };" ^
  "$bmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$w = $bmp.Width; $h = $bmp.Height;" ^
  "$hw = [int]($w / 2); $hh = [int]($h / 2);" ^
  "function Crop-Rect($srcBmp, $x, $y, $cw, $ch) {" ^
  "  $res = New-Object System.Drawing.Bitmap($cw, $ch, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($res);" ^
  "  $sr = New-Object System.Drawing.Rectangle($x, $y, $cw, $ch);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, $cw, $ch);" ^
  "  $g.DrawImage($srcBmp, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  return $res;" ^
  "};" ^
  "Write-Host '1. Memotong Door_Albedo.png...';" ^
  "$alb = Crop-Rect $bmp 0 0 $hw $hh;" ^
  "$alb.Save([System.IO.Path]::Combine($targetDir, 'Door_Albedo.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$alb.Save('c:\git\ProjectDarkRide\UVMapping\Door_Albedo.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$alb.Dispose();" ^
  "Write-Host '2. Memotong Door_Normal.png...';" ^
  "$norm = Crop-Rect $bmp $hw 0 ($w - $hw) $hh;" ^
  "$norm.Save([System.IO.Path]::Combine($targetDir, 'Door_Normal.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$norm.Save('c:\git\ProjectDarkRide\UVMapping\Door_Normal.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$norm.Dispose();" ^
  "Write-Host '3. Memotong Door_Roughness.png...';" ^
  "$rough = Crop-Rect $bmp 0 $hh $hw ($h - $hh);" ^
  "$rough.Save([System.IO.Path]::Combine($targetDir, 'Door_Roughness.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$rough.Save('c:\git\ProjectDarkRide\UVMapping\Door_Roughness.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$rough.Dispose();" ^
  "Write-Host '4. Memotong Door_AO.png...';" ^
  "$ao = Crop-Rect $bmp $hw $hh ($w - $hw) ($h - $hh);" ^
  "$ao.Save([System.IO.Path]::Combine($targetDir, 'Door_AO.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$ao.Save('c:\git\ProjectDarkRide\UVMapping\Door_AO.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$ao.Dispose();" ^
  "$bmp.Dispose();" ^
  "Write-Host 'Semua 4 PBR Maps Pintu berhasil dibuat bersih!'; "

REM Salin otomatis ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Door" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Door\*.*" "%%D\Door\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Door\*.*" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI! 4 PBR Texture Maps Pintu (Door) Berhasil Dibuat:
echo.
echo  - Door_Albedo.png     (Warna cat pintu besi berkarat + Staff Only)
echo  - Door_Normal.png     (Detail 3D kusen, gagang push-bar & kaca kawat)
echo  - Door_Roughness.png  (Tingkat kilap cat terkelupas vs besi pelindung)
echo  - Door_AO.png         (Ambient Occlusion rongga kusen & baut)
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Door\
echo ========================================================
pause
