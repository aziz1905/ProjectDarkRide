@echo off
echo ========================================================
echo Memproses PBR Karakter Full HD dari Gambar Native Baru...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792082078630_4769ec0e.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Character';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File tidak ditemukan'; exit };" ^
  "$srcBmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$W = $srcBmp.Width; $H = $srcBmp.Height;" ^
  "Write-Host 'Resolusi gambar asli:' $W 'x' $H;" ^
  "$outSize = [Math]::Max(2048, $W);" ^
  "Write-Host 'Membuat texture maps resolusi:' $outSize 'x' $outSize;" ^
  "$albBmp = New-Object System.Drawing.Bitmap($outSize, $outSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$g = [System.Drawing.Graphics]::FromImage($albBmp);" ^
  "$g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;" ^
  "$g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality;" ^
  "$g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality;" ^
  "$g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality;" ^
  "$g.DrawImage($srcBmp, (New-Object System.Drawing.Rectangle(0, 0, $outSize, $outSize)), (New-Object System.Drawing.Rectangle(0, 0, $W, $H)), [System.Drawing.GraphicsUnit]::Pixel);" ^
  "$g.Dispose();" ^
  "$srcBmp.Dispose();" ^
  "Write-Host '1. Menyimpan Character_Albedo.png Full HD...';" ^
  "$albBmp.Save([System.IO.Path]::Combine($targetDir, 'Character_Albedo.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$albBmp.Save('c:\git\ProjectDarkRide\UVMapping\Character_Albedo.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$rect = New-Object System.Drawing.Rectangle(0, 0, $outSize, $outSize);" ^
  "$data = $albBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$totalBytes = $outSize * $outSize * 4;" ^
  "$bytes = New-Object byte[] $totalBytes;" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $totalBytes);" ^
  "$albBmp.UnlockBits($data);" ^
  "Write-Host '2. Mengkalkulasi Character_Normal.png HD (Detail Otot & Jahitan Celana)...';" ^
  "$normBmp = New-Object System.Drawing.Bitmap($outSize, $outSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$nData = $normBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$nBytes = New-Object byte[] $totalBytes;" ^
  "for ($y = 1; $y -lt ($outSize - 1); $y++) {" ^
  "  for ($x = 1; $x -lt ($outSize - 1); $x++) {" ^
  "    $idx = ($y * $outSize + $x) * 4;" ^
  "    $b = [int]$bytes[$idx]; $gVal = [int]$bytes[$idx+1]; $r = [int]$bytes[$idx+2];" ^
  "    if ($r -lt 35 -and $gVal -lt 35 -and $b -lt 35 -and $y -lt ($outSize * 0.7) -and $x -lt ($outSize * 0.2)) {" ^
  "      $nBytes[$idx] = 255; $nBytes[$idx+1] = 128; $nBytes[$idx+2] = 128; $nBytes[$idx+3] = 255;" ^
  "    } else {" ^
  "      $gLeft  = [int]$bytes[($y * $outSize + ($x - 1)) * 4 + 1];" ^
  "      $gRight = [int]$bytes[($y * $outSize + ($x + 1)) * 4 + 1];" ^
  "      $gUp    = [int]$bytes[(($y - 1) * $outSize + $x) * 4 + 1];" ^
  "      $gDown  = [int]$bytes[(($y + 1) * $outSize + $x) * 4 + 1];" ^
  "      $dx = ($gLeft - $gRight) * 2.2;" ^
  "      $dy = ($gUp - $gDown) * 2.2;" ^
  "      $nx = [Math]::Max(-1.0, [Math]::Min(1.0, $dx / 255.0));" ^
  "      $ny = [Math]::Max(-1.0, [Math]::Min(1.0, $dy / 255.0));" ^
  "      $nz = [Math]::Sqrt([Math]::Max(0.0, 1.0 - $nx*$nx - $ny*$ny));" ^
  "      $nBytes[$idx]     = [byte][Math]::Round(($nz * 0.5 + 0.5) * 255);" ^
  "      $nBytes[$idx + 1] = [byte][Math]::Round(($ny * 0.5 + 0.5) * 255);" ^
  "      $nBytes[$idx + 2] = [byte][Math]::Round(($nx * 0.5 + 0.5) * 255);" ^
  "      $nBytes[$idx + 3] = 255;" ^
  "    }" ^
  "  }" ^
  "};" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($nBytes, 0, $nData.Scan0, $totalBytes);" ^
  "$normBmp.UnlockBits($nData);" ^
  "$normBmp.Save([System.IO.Path]::Combine($targetDir, 'Character_Normal.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$normBmp.Save('c:\git\ProjectDarkRide\UVMapping\Character_Normal.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$normBmp.Dispose();" ^
  "Write-Host '3. Membuat Character_Roughness.png HD...';" ^
  "$rouBmp = New-Object System.Drawing.Bitmap($outSize, $outSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$rData = $rouBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$rBytes = New-Object byte[] $totalBytes;" ^
  "for ($y = 0; $y -lt $outSize; $y++) {" ^
  "  for ($x = 0; $x -lt $outSize; $x++) {" ^
  "    $idx = ($y * $outSize + $x) * 4;" ^
  "    $b = [int]$bytes[$idx]; $gVal = [int]$bytes[$idx+1]; $r = [int]$bytes[$idx+2];" ^
  "    $lum = [int](($b * 0.11) + ($gVal * 0.59) + ($r * 0.3));" ^
  "    $val = [byte][Math]::Min(255, [Math]::Max(50, [int](255 - $lum * 0.85)));" ^
  "    $rBytes[$idx] = $val; $rBytes[$idx+1] = $val; $rBytes[$idx+2] = $val; $rBytes[$idx+3] = 255;" ^
  "  }" ^
  "};" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($rBytes, 0, $rData.Scan0, $totalBytes);" ^
  "$rouBmp.UnlockBits($rData);" ^
  "$rouBmp.Save([System.IO.Path]::Combine($targetDir, 'Character_Roughness.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$rouBmp.Save('c:\git\ProjectDarkRide\UVMapping\Character_Roughness.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$rouBmp.Dispose();" ^
  "Write-Host '4. Membuat Character_AO.png HD...';" ^
  "$aoBmp = New-Object System.Drawing.Bitmap($outSize, $outSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$aoData = $aoBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$aoBytes = New-Object byte[] $totalBytes;" ^
  "for ($y = 0; $y -lt $outSize; $y++) {" ^
  "  for ($x = 0; $x -lt $outSize; $x++) {" ^
  "    $idx = ($y * $outSize + $x) * 4;" ^
  "    $b = [int]$bytes[$idx]; $gVal = [int]$bytes[$idx+1]; $r = [int]$bytes[$idx+2];" ^
  "    $lum = [int](($b * 0.11) + ($gVal * 0.59) + ($r * 0.3));" ^
  "    $val = [byte][Math]::Min(255, [Math]::Max(60, [int]($lum * 1.6 + 40)));" ^
  "    $aoBytes[$idx] = $val; $aoBytes[$idx+1] = $val; $aoBytes[$idx+2] = $val; $aoBytes[$idx+3] = 255;" ^
  "  }" ^
  "};" ^
  "[System.Runtime.InteropServices.Marshal]::Copy($aoBytes, 0, $aoData.Scan0, $totalBytes);" ^
  "$aoBmp.UnlockBits($aoData);" ^
  "$aoBmp.Save([System.IO.Path]::Combine($targetDir, 'Character_AO.png'), [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$aoBmp.Save('c:\git\ProjectDarkRide\UVMapping\Character_AO.png', [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "$aoBmp.Dispose();" ^
  "$albBmp.Dispose();" ^
  "Write-Host 'Semua 4 PBR Maps Karakter Full HD berhasil dibuat bersih!'; "

REM Salin ke semua folder UVMapping Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Character" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Character\*.*" "%%D\Character\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Character\*.*" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI! 4 PBR Maps Karakter Full HD Berhasil Diperbarui:
echo.
echo  - Character_Albedo.png    (Warna kulit HD asli, tanpa blur)
echo  - Character_Normal.png    (Tangent Normal 3D detail otot & celana)
echo  - Character_Roughness.png (Peta kilap/keringat kulit HD)
echo  - Character_AO.png        (Ambient Occlusion rongga & lekuk otot)
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Character\
echo ========================================================
pause
