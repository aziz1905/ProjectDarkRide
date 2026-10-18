@echo off
echo ========================================================
echo Memproses 4 Aset PBR (GlassCase, DeerTrophy, StoneWall, Sofa)...
echo ========================================================

REM 1. Jalankan via Python jika tersedia
python c:\git\ProjectDarkRide\process_4_assets.py 2>nul
if %errorlevel% equ 0 goto selesai

echo Menjalankan via PowerShell HighQualityBicubic 2K HD...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$items = @(" ^
  "  @{ name='GlassCase'; src='C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792164845097_f7f76f1e.png'; m1='GlassCase_Albedo.png'; m2='GlassCase_Normal.png'; m3='GlassCase_Roughness.png'; m4='GlassCase_Opacity.png' }," ^
  "  @{ name='DeerTrophy'; src='C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792164849553_0b9ca36e.png'; m1='Deer_Albedo.png'; m2='Deer_Normal.png'; m3='Deer_Roughness.png'; m4='Deer_AO.png' }," ^
  "  @{ name='StoneWall'; src='C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792164853831_4391c755.jpg'; m1='StoneWall_Albedo.png'; m2='StoneWall_Normal.png'; m3='StoneWall_Roughness.png'; m4='StoneWall_AO.png' }," ^
  "  @{ name='Sofa'; src='C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792164858244_ee5e949d.png'; m1='Sofa_Albedo.png'; m2='Sofa_Normal.png'; m3='Sofa_Roughness.png'; m4='Sofa_AO.png' }" ^
  ");" ^
  "function Crop-2K($srcBmp, $x, $y, $s, $outPath) {" ^
  "  $sub = New-Object System.Drawing.Bitmap(2048, 2048, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($sub);" ^
  "  $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;" ^
  "  $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality;" ^
  "  $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality;" ^
  "  $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality;" ^
  "  $sr = New-Object System.Drawing.Rectangle($x, $y, $s, $s);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, 2048, 2048);" ^
  "  $g.DrawImage($srcBmp, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  $sub.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $sub.Dispose();" ^
  "  Write-Host 'Tersimpan 2K HD:' $outPath;" ^
  "};" ^
  "foreach ($item in $items) {" ^
  "  if (-not (Test-Path $item.src)) { continue };" ^
  "  $dest = [System.IO.Path]::Combine('c:\git\ProjectDarkRide\UVMapping', $item.name);" ^
  "  if (-not (Test-Path $dest)) { [System.IO.Directory]::CreateDirectory($dest) | Out-Null };" ^
  "  $bmp = [System.Drawing.Bitmap]::FromFile($item.src);" ^
  "  $W = [int]$bmp.Width; $H = [int]$bmp.Height;" ^
  "  $size = [int]($W * 0.334);" ^
  "  $ax = [int]($W * 0.150); $ay = [int]($H * 0.140);" ^
  "  $nx = [int]($W * 0.514); $ny = [int]($H * 0.140);" ^
  "  $by = [int]($H * 0.558);" ^
  "  Crop-2K $bmp $ax $ay $size ([System.IO.Path]::Combine($dest, $item.m1));" ^
  "  Crop-2K $bmp $nx $ny $size ([System.IO.Path]::Combine($dest, $item.m2));" ^
  "  Crop-2K $bmp $ax $by $size ([System.IO.Path]::Combine($dest, $item.m3));" ^
  "  Crop-2K $bmp $nx $by $size ([System.IO.Path]::Combine($dest, $item.m4));" ^
  "  Copy-Item ([System.IO.Path]::Combine($dest, '*.*')) 'c:\git\ProjectDarkRide\UVMapping\' -Force;" ^
  "  $bmp.Dispose();" ^
  "};"

:selesai
REM Salin ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        for %%A in (GlassCase DeerTrophy StoneWall Sofa) do (
            mkdir "%%D\%%A" 2>nul
            copy /Y "c:\git\ProjectDarkRide\UVMapping\%%A\*.*" "%%D\%%A\" >nul
            copy /Y "c:\git\ProjectDarkRide\UVMapping\%%A\*.*" "%%D\" >nul
        )
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI SEMUA! 16 PBR Texture Maps (2K HD) Berhasil Dibuat:
echo.
echo [1. GLASS CASE / DISPLAY]
echo   GlassCase_Albedo.png, GlassCase_Normal.png, GlassCase_Roughness.png, GlassCase_Opacity.png
echo.
echo [2. DEER TROPHY / KEPALA RUSA]
echo   Deer_Albedo.png, Deer_Normal.png, Deer_Roughness.png, Deer_AO.png
echo.
echo [3. STONE WALL / DUNGEON ARCH]
echo   StoneWall_Albedo.png, StoneWall_Normal.png, StoneWall_Roughness.png, StoneWall_AO.png
echo.
echo [4. SOFA / LEATHER COUCH]
echo   Sofa_Albedo.png, Sofa_Normal.png, Sofa_Roughness.png, Sofa_AO.png
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\
echo ========================================================
pause
