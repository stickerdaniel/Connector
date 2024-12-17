param (
    [string]$argument
)


function build_android {
    Write-Output "Building Android connector..."
    # get tag from package.json
    $packageJsonContent = Get-Content -Path "frontend/Unity/Packages/com.cynteract.connector/package.json" | ConvertFrom-Json
    $version = $packageJsonContent.version
    $tag = "v$version"
    Push-Location .\backend\android
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
    .\gradlew :app:connector:assembleRelease
    Write-Output "Copying Android connector to Unity project..."
    Copy-Item -Path .\app\connector\build\outputs\aar\connector-release.aar -Destination .\..\..\frontend\Unity\Assets\connector-release_$tag.aar -Force
    Pop-Location
}

function build_windows {
    Write-Output "Building Windows connector..."
    # get tag from package.json
    $packageJsonContent = Get-Content -Path "frontend/Unity/Packages/com.cynteract.connector/package.json" | ConvertFrom-Json
    $version = $packageJsonContent.version
    $tag = "v$version"

    Push-Location .\backend\windows
    dotnet publish -c Release -r win10-x64 --self-contained -o ../../frontend/Unity/Assets/StreamingAssets Connector.csproj



    Move-Item -Path .\..\..\frontend\Unity\Assets\StreamingAssets\Connector.exe -Destination .\..\..\frontend\Unity\Assets\StreamingAssets\Connector_$tag.exe -Force

    Pop-Location
}

function build_all {
    build_android
    Write-Output ""
    build_windows
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
    gh release create $tag --generate-notes frontend/Unity/Assets/connector-release_$tag.aar frontend/Unity/Assets/StreamingAssets/Connector_$tag.exe
}

switch ($argument) {
    "android" {
        build_android
    }
    "windows" {
        build_windows
    }
    "all" {
        build_all
    }
    "release" {
        upload_release
    }
    default {
        Write-Host "Invalid argument. Please use 'android', 'windows', 'all', or 'release'."
    }
}
