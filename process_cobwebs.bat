@echo off
echo ========================================================
echo Memproses Ulang Jaring Laba-Laba: 100%% Background Bolong Transparan
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1792085922121_58762be8.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Decal';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File tidak ditemukan'; exit };" ^
  "$srcBmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$imgWidth = [int]$srcBmp.Width; $imgHeight = [int]$srcBmp.Height;" ^
  "Write-Host 'Resolusi sumber:' $imgWidth 'x' $imgHeight;" ^
  "$webList = @(" ^
  "  @{ name='Cobweb_01_Corner_Small.png';  x=[int]($imgWidth * 0.420); y=[int]($imgHeight * 0.115); w=[int]($imgWidth * 0.125); h=[int]($imgHeight * 0.170) }," ^
  "  @{ name='Cobweb_02_Dome.png';          x=[int]($imgWidth * 0.500); y=[int]($imgHeight * 0.190); w=[int]($imgWidth * 0.185); h=[int]($imgHeight * 0.180) }," ^
  "  @{ name='Cobweb_03_Wedge.png';         x=[int]($imgWidth * 0.330); y=[int]($imgHeight * 0.245); w=[int]($imgWidth * 0.145); h=[int]($imgHeight * 0.225) }," ^
  "  @{ name='Cobweb_04_Corner_Fan.png';    x=[int]($imgWidth * 0.175); y=[int]($imgHeight * 0.360); w=[int]($imgWidth * 0.180); h=[int]($imgHeight * 0.280) }," ^
  "  @{ name='Cobweb_05_Horizontal.png';    x=[int]($imgWidth * 0.510); y=[int]($imgHeight * 0.330); w=[int]($imgWidth * 0.200); h=[int]($imgHeight * 0.195) }," ^
  "  @{ name='Cobweb_06_Hanging.png';       x=[int]($imgWidth * 0.370); y=[int]($imgHeight * 0.485); w=[int]($imgWidth * 0.215); h=[int]($imgHeight * 0.295) }," ^
  "  @{ name='Cobweb_07_Vertical.png';      x=[int]($imgWidth * 0.605); y=[int]($imgHeight * 0.505); w=[int]($imgWidth * 0.135); h=[int]($imgHeight * 0.380) }" ^
  ");" ^
  "function Extract-PureWhiteWeb($subBmp, $outPath) {" ^
  "  $cw = [int]$subBmp.Width; $ch = [int]$subBmp.Height;" ^
  "  $rect = New-Object System.Drawing.Rectangle(0, 0, $cw, $ch);" ^
  "  $data = $subBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $byteCount = [int]($cw * $ch * 4);" ^
  "  $pix = New-Object byte[] $byteCount;" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pix, 0, $byteCount);" ^
  "  for ($i = 0; $i -lt $byteCount; $i += 4) {" ^
  "    $b = [int]$pix[$i]; $gVal = [int]$pix[$i+1]; $r = [int]$pix[$i+2];" ^
  "    $lum = [int](($b * 0.11) + ($gVal * 0.59) + ($r * 0.3));" ^
  "    $cutoff = 52;" ^
  "    if ($lum -le $cutoff) {" ^
  "      $pix[$i] = 0; $pix[$i+1] = 0; $pix[$i+2] = 0; $pix[$i+3] = 0;" ^
  "    } else {" ^
  "      $alpha = [int]([Math]::Min(255, ($lum - $cutoff) * 2.7));" ^
  "      $pix[$i+3] = [byte]$alpha;" ^
  "      $pix[$i]   = 245;" ^
  "      $pix[$i+1] = 245;" ^
  "      $pix[$i+2] = 245;" ^
  "    }" ^
  "  };" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($pix, 0, $data.Scan0, $byteCount);" ^
  "  $subBmp.UnlockBits($data);" ^
  "  $subBmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $subBmp.Dispose();" ^
  "  Write-Host '100% Transparan Selesai:' $outPath;" ^
  "};" ^
  "Write-Host '1. Memproses 7 Potongan Jaring Laba-Laba...';" ^
  "foreach ($item in $webList) {" ^
  "  $sub = New-Object System.Drawing.Bitmap($item.w, $item.h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($sub);" ^
  "  $sr = New-Object System.Drawing.Rectangle($item.x, $item.y, $item.w, $item.h);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, $item.w, $item.h);" ^
  "  $g.DrawImage($srcBmp, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  $p1 = [System.IO.Path]::Combine($targetDir, $item.name);" ^
  "  $p2 = [System.IO.Path]::Combine('c:\git\ProjectDarkRide\UVMapping', $item.name);" ^
  "  Extract-PureWhiteWeb $sub $p1;" ^
  "  Copy-Item $p1 $p2 -Force;" ^
  "};" ^
  "Write-Host '2. Memproses Full Sheet Jaring Laba-Laba Transparan...';" ^
  "$fullSub = New-Object System.Drawing.Bitmap($imgWidth, $imgHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "$g2 = [System.Drawing.Graphics]::FromImage($fullSub);" ^
  "$fullSr = New-Object System.Drawing.Rectangle(0, 0, $imgWidth, $imgHeight);" ^
  "$fullDr = New-Object System.Drawing.Rectangle(0, 0, $imgWidth, $imgHeight);" ^
  "$g2.DrawImage($srcBmp, $fullDr, $fullSr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "$g2.Dispose();" ^
  "$fullP1 = [System.IO.Path]::Combine($targetDir, 'Cobwebs_All_Sheet_Transparan.png');" ^
  "$fullP2 = [System.IO.Path]::Combine('c:\git\ProjectDarkRide\UVMapping', 'Cobwebs_All_Sheet_Transparan.png');" ^
  "Extract-PureWhiteWeb $fullSub $fullP1;" ^
  "Copy-Item $fullP1 $fullP2 -Force;" ^
  "$srcBmp.Dispose();"

REM Salin ke Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Decal" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Decal\Cobweb*.*" "%%D\Decal\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Decal\Cobweb*.*" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D\Decal
    )
)

echo.
echo ========================================================
echo SELESAI! Background Hitam Sudah 100%% Bolong Transparan:
echo.
echo  - Cobweb_01_Corner_Small.png       (Transparan)
echo  - Cobweb_02_Dome.png               (Transparan)
echo  - Cobweb_03_Wedge.png              (Transparan)
echo  - Cobweb_04_Corner_Fan.png         (Transparan)
echo  - Cobweb_05_Horizontal.png         (Transparan)
echo  - Cobweb_06_Hanging.png            (Transparan)
echo  - Cobweb_07_Vertical.png           (Transparan)
echo  - Cobwebs_All_Sheet_Transparan.png (Full Sheet Transparan)
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Decal\
echo ========================================================
pause
