@echo off
echo ========================================================
echo Memotong 4 Jenis Rumput dan Membuat PNG Transparan...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Drawing;" ^
  "$srcPath   = 'C:\Users\MyBook Hype AMD\.gemini\antigravity\brain\5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c\.user_uploaded\media_1791896014719_0236fe64.png';" ^
  "$targetDir = 'c:\git\ProjectDarkRide\UVMapping\Grass';" ^
  "if (-not (Test-Path $targetDir)) { [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null };" ^
  "if (-not (Test-Path $srcPath)) { Write-Host 'File sumber tidak ditemukan!'; exit };" ^
  "$srcBmp = [System.Drawing.Bitmap]::FromFile($srcPath);" ^
  "$W = $srcBmp.Width; $H = $srcBmp.Height;" ^
  "Write-Host 'Resolusi gambar:' $W 'x' $H;" ^
  "$w1 = [int]($W * 0.25);" ^
  "$w2 = [int]($W * 0.51);" ^
  "$w3 = [int]($W * 0.77);" ^
  "$slices = @(" ^
  "  @{ name='Grass_01_Transparan.png'; x=0;   y=0; w=$w1;            h=$H }," ^
  "  @{ name='Grass_02_Transparan.png'; x=$w1; y=0; w=($w2 - $w1);     h=$H }," ^
  "  @{ name='Grass_03_Transparan.png'; x=$w2; y=0; w=($w3 - $w2);     h=$H }," ^
  "  @{ name='Grass_04_Transparan.png'; x=$w3; y=0; w=($W - $w3);      h=$H }" ^
  ");" ^
  "function Make-TransparentClump($srcBmp, $box, $outPath) {" ^
  "  $cw = $box.w; $ch = $box.h;" ^
  "  $subBmp = New-Object System.Drawing.Bitmap($cw, $ch, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $g = [System.Drawing.Graphics]::FromImage($subBmp);" ^
  "  $sr = New-Object System.Drawing.Rectangle($box.x, $box.y, $cw, $ch);" ^
  "  $dr = New-Object System.Drawing.Rectangle(0, 0, $cw, $ch);" ^
  "  $g.DrawImage($srcBmp, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel);" ^
  "  $g.Dispose();" ^
  "  $rect = New-Object System.Drawing.Rectangle(0, 0, $cw, $ch);" ^
  "  $data = $subBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $bytes = $cw * $ch * 4;" ^
  "  $sBytes = New-Object byte[] $bytes;" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $sBytes, 0, $bytes);" ^
  "  $subBmp.UnlockBits($data);" ^
  "  $subBmp.Dispose();" ^
  "  $mask = New-Object byte[] ($cw * $ch);" ^
  "  for ($y = 0; $y -lt $ch; $y++) {" ^
  "    for ($x = 0; $x -lt $cw; $x++) {" ^
  "      $idx = ($y * $cw + $x) * 4;" ^
  "      $b = [int]$sBytes[$idx]; $gVal = [int]$sBytes[$idx+1]; $r = [int]$sBytes[$idx+2];" ^
  "      $isWhite = ($r -gt 238 -and $gVal -gt 238 -and $b -gt 238);" ^
  "      $isFringe = ($r -gt 210 -and $b -gt 210 -and $gVal -gt 210 -and [Math]::Abs($r - $b) -lt 15 -and [Math]::Abs($r - $gVal) -lt 15);" ^
  "      if ($isWhite -or $isFringe) { $mask[$y * $cw + $x] = 0 } else { $mask[$y * $cw + $x] = 255 };" ^
  "    }" ^
  "  };" ^
  "  $tempMask = $mask.Clone();" ^
  "  for ($y = 1; $y -lt ($ch - 1); $y++) {" ^
  "    for ($x = 1; $x -lt ($cw - 1); $x++) {" ^
  "      $c = $y * $cw + $x;" ^
  "      if ($mask[$c] -gt 0) {" ^
  "        if ($mask[$c - 1] -eq 0 -or $mask[$c + 1] -eq 0 -or $mask[$c - $cw] -eq 0 -or $mask[$c + $cw] -eq 0) {" ^
  "          $tempMask[$c] = 0;" ^
  "        }" ^
  "      }" ^
  "    }" ^
  "  };" ^
  "  $mask = $tempMask;" ^
  "  $minX = $cw; $maxX = 0; $minY = $ch; $maxY = 0;" ^
  "  for ($y = 0; $y -lt $ch; $y++) {" ^
  "    for ($x = 0; $x -lt $cw; $x++) {" ^
  "      if ($mask[$y * $cw + $x] -gt 0) {" ^
  "        if ($x -lt $minX) { $minX = $x };" ^
  "        if ($x -gt $maxX) { $maxX = $x };" ^
  "        if ($y -lt $minY) { $minY = $y };" ^
  "        if ($y -gt $maxY) { $maxY = $y };" ^
  "      }" ^
  "    }" ^
  "  };" ^
  "  if ($minX -ge $maxX -or $minY -ge $maxY) { $minX = 0; $maxX = $cw - 1; $minY = 0; $maxY = $ch - 1 };" ^
  "  $cropW = ($maxX - $minX) + 1;" ^
  "  $cropH = ($maxY - $minY) + 1;" ^
  "  $pad = 4;" ^
  "  $finalW = $cropW + ($pad * 2);" ^
  "  $finalH = $cropH + ($pad * 2);" ^
  "  $finalBmp = New-Object System.Drawing.Bitmap($finalW, $finalH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $fRect = New-Object System.Drawing.Rectangle(0, 0, $finalW, $finalH);" ^
  "  $fData = $finalBmp.LockBits($fRect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);" ^
  "  $fBytes = New-Object byte[] ($finalW * $finalH * 4);" ^
  "  for ($cy = 0; $cy -lt $cropH; $cy++) {" ^
  "    $origY = $minY + $cy;" ^
  "    $dstY  = $cy + $pad;" ^
  "    for ($cx = 0; $cx -lt $cropW; $cx++) {" ^
  "      $origX = $minX + $cx;" ^
  "      $dstX  = $cx + $pad;" ^
  "      $srcIdx = ($origY * $cw + $origX) * 4;" ^
  "      $dstIdx = ($dstY * $finalW + $dstX) * 4;" ^
  "      $m = $mask[$origY * $cw + $origX];" ^
  "      $fBytes[$dstIdx]     = $sBytes[$srcIdx];" ^
  "      $fBytes[$dstIdx + 1] = $sBytes[$srcIdx + 1];" ^
  "      $fBytes[$dstIdx + 2] = $sBytes[$srcIdx + 2];" ^
  "      $fBytes[$dstIdx + 3] = [byte]$m;" ^
  "    }" ^
  "  };" ^
  "  [System.Runtime.InteropServices.Marshal]::Copy($fBytes, 0, $fData.Scan0, ($finalW * $finalH * 4));" ^
  "  $finalBmp.UnlockBits($fData);" ^
  "  $finalBmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png);" ^
  "  $finalBmp.Dispose();" ^
  "  Write-Host 'Berhasil disimpan:' $outPath;" ^
  "};" ^
  "foreach ($item in $slices) {" ^
  "  $out1 = [System.IO.Path]::Combine($targetDir, $item.name);" ^
  "  $out2 = [System.IO.Path]::Combine('c:\git\ProjectDarkRide\UVMapping', $item.name);" ^
  "  Make-TransparentClump $srcBmp $item $out1;" ^
  "  Copy-Item $out1 $out2 -Force;" ^
  "};" ^
  "$srcBmp.Dispose();"

REM Salin ke Unity
for /d /r "c:\git\ProjectDarkRide" %%D in (UVMapping) do (
    if exist "%%D" if not "%%D"=="c:\git\ProjectDarkRide\UVMapping" (
        mkdir "%%D\Grass" 2>nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Grass\Grass_*_Transparan.png" "%%D\Grass\" >nul
        copy /Y "c:\git\ProjectDarkRide\UVMapping\Grass\Grass_*_Transparan.png" "%%D\" >nul
        echo Berhasil disalin ke Unity: %%D
    )
)

echo.
echo ========================================================
echo SELESAI! 4 Rumput Transparan Berhasil Dibuat:
echo.
echo  - Grass_01_Transparan.png (Rumput tinggi berbunga 1)
echo  - Grass_02_Transparan.png (Semak rumput hijau lebat)
echo  - Grass_03_Transparan.png (Rumput tinggi berbunga 2)
echo  - Grass_04_Transparan.png (Rumput kecil compact)
echo.
echo Lokasi: c:\git\ProjectDarkRide\UVMapping\Grass\
echo ========================================================
pause
