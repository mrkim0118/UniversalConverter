# Signs the given files (wildcards allowed) with the "CN=Manggdy" code-signing certificate
# from the current user's certificate store. Skips with a warning when no such certificate exists,
# so the build still works on other machines.
param([Parameter(Mandatory, ValueFromRemainingArguments)][string[]]$Paths)

$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq 'CN=Manggdy' -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $cert) {
    Write-Warning 'No "CN=Manggdy" code-signing certificate found; skipping signing.'
    exit 0
}

foreach ($file in Get-Item -Path $Paths) {
    $result = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $cert -HashAlgorithm SHA256 `
        -TimestampServer 'http://timestamp.digicert.com'
    if ($result.Status -notin 'Valid', 'UnknownError') {   # UnknownError = self-signed root not trusted on this PC
        Write-Error "Signing failed for $($file.Name): $($result.StatusMessage)"
        exit 1
    }
    Write-Host "    Signed: $($file.Name)"
}
