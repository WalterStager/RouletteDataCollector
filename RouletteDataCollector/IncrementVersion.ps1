# increments build number in csproj file (automatically runs on "build debug" vscode task)
[xml]$config = Get-Content RouletteDataCollector/RouletteDataCollector.csproj
$vers = $config.Project.PropertyGroup.Version.Split('.')
$vers[$args[0]] = [int]$vers[$args[0]] + 1
$config.Project.PropertyGroup.Version = $vers -join '.'
Write-Host $config.Project.PropertyGroup.Version
($config | Format-Xml) | Out-File RouletteDataCollector/RouletteDataCollector.csproj -Encoding ascii