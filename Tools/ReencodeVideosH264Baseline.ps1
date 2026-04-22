#Requires -Version 5.1
<#
  将工程内视频统一转为 H.264 Baseline + AAC，减少 Windows VideoPlayer 黑屏/时间戳警告。
  依赖：ffmpeg 在 PATH 中（https://ffmpeg.org/ 或 choco install ffmpeg）。

  用法（在仓库根目录，PowerShell）：
    .\Tools\ReencodeVideosH264Baseline.ps1
    .\Tools\ReencodeVideosH264Baseline.ps1 -ScanAllAssets   # 扫描整个 Assets 下 mp4/mov（较慢）
    .\Tools\ReencodeVideosH264Baseline.ps1 -WhatIf           # 只列出将处理的文件

  原文件会备份为同目录 *.pre_reencode_backup.mp4
#>
param(
    [switch] $ScanAllAssets,
    [switch] $WhatIf
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Error "未找到 ffmpeg。请先安装并加入 PATH：https://ffmpeg.org/download.html"
}

function Invoke-ReencodeOne([string] $InputPath) {
    $InputPath = [System.IO.Path]::GetFullPath($InputPath)
    if (-not (Test-Path -LiteralPath $InputPath)) {
        Write-Warning "跳过（不存在）: $InputPath"
        return
    }

    $dir = [System.IO.Path]::GetDirectoryName($InputPath)
    $base = [System.IO.Path]::GetFileNameWithoutExtension($InputPath)
    $ext = [System.IO.Path]::GetExtension($InputPath)
    $tempOut = Join-Path $dir "${base}__h264baseline_tmp${ext}"
    $backup = Join-Path $dir "${base}.pre_reencode_backup${ext}"

    if ($WhatIf) {
        Write-Host "[WhatIf] 将处理: $InputPath"
        return
    }

    Write-Host "编码: $InputPath"

    $hasAudio = $false
    try {
        $probe = & ffprobe -v error -select_streams a -show_entries stream=codec_type -of csv=p=0 $InputPath 2>$null
        if ($probe -match 'audio') { $hasAudio = $true }
    } catch { }

    if ($hasAudio) {
        & ffmpeg -hide_banner -nostdin -y -i $InputPath `
            -map 0:v:0 -c:v libx264 -profile:v baseline -level 3.1 -pix_fmt yuv420p -preset medium -crf 21 -movflags +faststart `
            -map 0:a:0 -c:a aac -b:a 192k -ac 2 -ar 48000 `
            $tempOut
    }
    else {
        & ffmpeg -hide_banner -nostdin -y -i $InputPath `
            -map 0:v:0 -c:v libx264 -profile:v baseline -level 3.1 -pix_fmt yuv420p -preset medium -crf 21 -movflags +faststart `
            -an `
            $tempOut
    }

    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $tempOut -ErrorAction SilentlyContinue
        throw "ffmpeg 失败: $InputPath"
    }

    if (-not (Test-Path -LiteralPath $backup)) {
        Copy-Item -LiteralPath $InputPath -Destination $backup -Force
    }
    Move-Item -LiteralPath $tempOut -Destination $InputPath -Force
    Write-Host "  完成（备份: $backup）"
}

# 脚本里约定的 StreamingAssets 文件名（存在才处理）
$explicitNames = @(
    'ending_part1.mp4',
    'ending_part2.mp4',
    'chapter1_outro.mp4',
    'chapter2_end.mp4',
    'rules.mp4',
    'intro.mp4',
    'chapter3_post_compass.mp4',
    'intro_bridge.mp4',
    'opening.mp4'
)

$streaming = Join-Path $ProjectRoot 'Assets\StreamingAssets'
$assetsRoot = Join-Path $ProjectRoot 'Assets'
$toProcess = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)

foreach ($name in $explicitNames) {
    $p = Join-Path $streaming $name
    if (Test-Path -LiteralPath $p) { [void]$toProcess.Add($p) }
}

# 素材内指定文件（微信导出等常为 High/HEVC，存在才处理）
$explicitAssetRelative = @(
    '素材\new\26计设素材\WeChat_20260410181153.mp4',
    '素材\dd\e0762de1c28407a973c6d15245ebf883.mp4'
)
foreach ($rel in $explicitAssetRelative) {
    $p = Join-Path $assetsRoot $rel
    if (Test-Path -LiteralPath $p) { [void]$toProcess.Add($p) }
}

if (Test-Path -LiteralPath $streaming) {
    Get-ChildItem -LiteralPath $streaming -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -match '\.(mp4|mov)$' } |
        ForEach-Object { [void]$toProcess.Add($_.FullName) }
}

if ($ScanAllAssets) {
    $assets = Join-Path $ProjectRoot 'Assets'
    Get-ChildItem -LiteralPath $assets -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notmatch '\\(Library|Temp|obj)\\' -and
            $_.Extension -match '\.(mp4|mov)$'
        } |
        ForEach-Object { [void]$toProcess.Add($_.FullName) }
}

$ordered = $toProcess | Sort-Object
if ($ordered.Count -eq 0) {
    Write-Host "未找到可处理的 .mp4 / .mov。"
    Write-Host "  StreamingAssets 路径: $streaming"
    Write-Host "  若视频在别处，请拷贝到 StreamingAssets 或使用 -ScanAllAssets。"
    exit 0
}

foreach ($f in $ordered) {
    Invoke-ReencodeOne $f
}

Write-Host "全部完成。请在 Unity 中选中相关 VideoClip 重新导入（或 Reimport All）。"
