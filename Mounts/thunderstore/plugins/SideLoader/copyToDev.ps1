$Path = "C:\Users\Exp\AppData\Roaming\r2modmanPlus-local\OutwardDe\profiles\Dev\BepInEx\plugins\exp111-Mounts\SideLoader"
Get-ChildItem $Path | ForEach {Remove-Item $_.FullName -Recurse}

Copy-Item .\Items $Path -Recurse
Copy-Item .\AssetBundles $Path -Recurse
Copy-Item .\manifest.txt $Path