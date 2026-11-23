param([string]$OutputPath = 'BioShellLab-Studio.exe')
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sdk = Join-Path $PSScriptRoot 'vendor'
if (-not (Test-Path -LiteralPath (Join-Path $sdk 'Microsoft.Web.WebView2.Core.dll'))) {
    throw 'Missing WebView2 SDK files in vendor. See README.'
}
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 "/out:$OutputPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll "/reference:$sdk/Microsoft.Web.WebView2.Core.dll" "/reference:$sdk/Microsoft.Web.WebView2.WinForms.dll" "/resource:$sdk/Microsoft.Web.WebView2.Core.dll,Microsoft.Web.WebView2.Core.dll" "/resource:$sdk/Microsoft.Web.WebView2.WinForms.dll,Microsoft.Web.WebView2.WinForms.dll" "/resource:$sdk/WebView2Loader.dll,WebView2Loader.dll" /win32icon:bioshell.ico /resource:bioshell.ico,bioshell.ico /resource:studio.html,studio.html Engine.cs Desktop.cs Tests.cs
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
} finally { Pop-Location }
