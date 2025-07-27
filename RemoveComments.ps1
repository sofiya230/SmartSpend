Get-ChildItem -Recurse -Include *.cs -Path . | ForEach-Object {
    $filePath = $_.FullName
    (Get-Content $filePath) |
        Where-Object { $_ -notmatch '^\s*//.*$' } |
        Set-Content $filePath
}
