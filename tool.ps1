param (
    [string]$argument
)

function set_android_env {
    # use Unity Android SDK if not specified
    if (-not $env:JAVE_HOME) {
        $env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
        # Unity Java is outdated
        # > Android Gradle plugin requires Java 17 to run. You are currently using Java 11.
        #   Your current JDK is located in C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK
        # $env:JAVA_HOME = "C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK"
    }
    if (-not $env:ANDROID_HOME) {
        $env:ANDROID_HOME = "C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
    }
}

function android_test {
    Write-Output "Testing Android connector..."
    Push-Location .\backend\android
    set_android_env
    .\gradlew :app:connector:test
    Pop-Location
}

function android_build {
    Write-Output "Building Android connector..."
    # get tag from package.json
    $packageJsonContent = Get-Content -Path "frontend/Unity/Packages/com.cynteract.connector/package.json" | ConvertFrom-Json
    $version = $packageJsonContent.version
    $tag = "v$version"
    $repoRoot = (Get-Location).Path
    Push-Location .\backend\android
    set_android_env
    .\gradlew :app:connector:assembleRelease
    Write-Output "Copying Android connector to Unity project..."
    $outputPath = Join-Path -Path $repoRoot -ChildPath "frontend/Unity/Assets/connector-release_$tag.aar"
    # ensure destination directory exists
    $destDir = Split-Path -Path $outputPath -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item -Path .\app\connector\build\outputs\aar\connector-release.aar -Destination $outputPath -Force
    Pop-Location
}

function windows_build {
    Write-Output "Building Windows connector..."
    # get tag from package.json
    $packageJsonContent = Get-Content -Path "frontend/Unity/Packages/com.cynteract.connector/package.json" | ConvertFrom-Json
    $version = $packageJsonContent.version
    $projectFolder = "./backend/windows/src/Connector.Windows"
    $projectPath = "$projectFolder/Connector.Windows.csproj"
    $outputPath = "./frontend/Unity/Assets/StreamingAssets/Connector_v$version.exe"
    # Do a clean build to show all warnings. This will slightly increase the build time for the next debug run as well.
    dotnet clean $projectPath
    dotnet publish -c Release -r win-x64 --self-contained  $projectPath
    if ($LASTEXITCODE -ne 0) {
        Write-Output "dotnet publish failed."
        exit $LASTEXITCODE
    }
    # ensure destination directory exists
    $destDir = Split-Path -Path $outputPath -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item -Path "$projectFolder\bin\Release\net8.0\win-x64\publish\Connector.Windows.exe" -Destination $outputPath -Force
}

function linux_build {
    Write-Output "Building Linux connector..."
    # get tag from package.json
    $packageJsonContent = Get-Content -Path "frontend/Unity/Packages/com.cynteract.connector/package.json" | ConvertFrom-Json
    $version = $packageJsonContent.version
    $projectFolder = "./backend/linux/src/Connector.Linux"
    $projectPath = "$projectFolder/Connector.Linux.csproj"
    $outputPath = "./frontend/Unity/Assets/StreamingAssets/Connector_v$version"
    # Do a clean build to show all warnings. This will slightly increase the build time for the next debug run as well.
    dotnet clean $projectPath
    dotnet publish -c Release -r linux-x64 --self-contained  $projectPath
    if ($LASTEXITCODE -ne 0) {
        Write-Output "dotnet publish failed."
        exit $LASTEXITCODE
    }
    # ensure destination directory exists
    $destDir = Split-Path -Path $outputPath -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item -Path "$projectFolder/bin/Release/net8.0/linux-x64/publish/Connector.Linux" -Destination $outputPath -Force
}

function build_all {
    android_build
    Write-Output ""
    windows_build
    Write-Output ""
    linux_build
}

function upload_release {
    # check if the current branch is main
    $branch = git branch --show-current
    if ($branch -ne "main") {
        Write-Output "You're not on the main branch. If you still want to continue, press Enter."
        $null = Read-Host
    }

    # check for uncommited changes
    git diff-index --quiet HEAD --
    if ($LASTEXITCODE -ne 0) {
        Write-Output "There are uncommited changes. If you still want to continue, press Enter."
        $null = Read-Host
    }

    # get tag from package.json
    $packageJsonContent = Get-Content -Path "frontend/Unity/Packages/com.cynteract.connector/package.json" | ConvertFrom-Json
    $version = $packageJsonContent.version
    $tag = "v$version"

    # create a new tag if it doesn't exist
    git tag $tag 2>$null
    $checkTag = git describe --tags --abbrev=0
    if (-not $checkTag -or $checkTag -ne $tag) {
        Write-Output "created tag $tag does not match tag $checkTag reported by git"
        return
    }

    # check if HEAD is tagged
    git describe --tags --exact-match HEAD 2>$null
    if ($LASTEXITCODE) {
        Write-Output "HEAD is not tagged. If you want to update the last release, press Enter."
        $null = Read-Host
    }

    # login to github
    gh auth status >$null
    if ($LASTEXITCODE) {
        Write-Output "Please login to GitHub..."
        gh auth login
        gh auth status
        if ($LASTEXITCODE) {
            Write-Output "Failed to login to GitHub."
            return
        }
    }

    # upload release
    Write-Output "Upload to GitHub..."
    $tag = git describe --tags --abbrev=0
    if (-not $tag) {
        Write-Output "Failed to get the tag."
        return
    }
    git push origin $tag
    gh release delete $tag --yes
    gh release create $tag --generate-notes frontend/Unity/Assets/connector-release_$tag.aar frontend/Unity/Assets/StreamingAssets/Connector_$tag.exe frontend/Unity/Assets/StreamingAssets/Connector_$tag
}

function copy_files {
    $projectRoot = (Get-Location).Path

    $syncedFiles = @{
        "backend/shared/tests/Connector.Tests/TestData/testdata.json" = "backend/android/app/connector/src/test/resources/testdata.json"
        "backend/shared/src/Connector/Protocol.cs"                    = "frontend/Unity/Packages/com.cynteract.connector/Runtime/Protocol.cs"
    }

    $syncedFolders = @{
        "backend/shared/src/Connector/Messages" = "frontend/Unity/Packages/com.cynteract.connector/Runtime/Messages"
    }

    foreach ($file in $syncedFiles.GetEnumerator()) {
        $src = Join-Path $projectRoot $file.Key
        $dst = Join-Path $projectRoot $file.Value
        if (-not (Test-Path $src)) {
            Write-Output "Skipping copy (source missing): $src"
            continue
        }
        $dstDir = Split-Path -Path $dst -Parent
        if (-not (Test-Path $dstDir)) {
            New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
        }
        Copy-Item -Path $src -Destination $dst -Force
    }

    foreach ($folder in $syncedFolders.GetEnumerator()) {
        $srcFolder = Join-Path $projectRoot $folder.Key
        $dstFolder = Join-Path $projectRoot $folder.Value
        if (-not (Test-Path $srcFolder)) {
            Write-Output "Skipping folder copy (source missing): $srcFolder"
            continue
        }
        if (-not (Test-Path $dstFolder)) {
            New-Item -ItemType Directory -Path $dstFolder -Force | Out-Null
        }
        Copy-Item -Path (Join-Path $srcFolder "*") -Destination $dstFolder -Recurse -Force
    }
}

switch ($argument) {
    "android" {
        android_build
    }
    "android_test" {
        android_test
    }
    "windows" {
        windows_build
    }
    "linux" {
        linux_build
    }
    "all" {
        build_all
    }
    "release" {
        upload_release
    }
    "copy_files" {
        copy_files
    }
    default {
        Write-Host "Invalid argument. Possible commands: [android | windows | linux | all | release | copy_files]."
    }
}
