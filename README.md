# GameLogBook

GameLogBook is a local-first game library app built with .NET MAUI Blazor Hybrid.

The app runs as a native desktop/mobile app with an embedded Blazor WebView. It does not require a local web server or a browser tab. Data is stored in a local SQLite database under the app data directory for the current device.

## Targets

- Android
- iOS
- macOS, via Mac Catalyst
- Windows, when built on Windows

Linux is intentionally out of scope for the MAUI app.

## Local Setup

Install the .NET MAUI workload before building:

```bash
sudo dotnet workload install maui
```

On machines where the .NET SDK is installed somewhere writable by the current user, `sudo` may not be required.

### macOS and iOS

Mac Catalyst and iOS builds require full Xcode, not only the Command Line Tools.

After installing Xcode, select it as the active developer directory:

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
xcodebuild -runFirstLaunch
```

If Mac Catalyst fails in `AppleSdkSettings` before detecting Xcode, create the MAUI settings file:

```bash
mkdir -p ~/Library/Preferences/maui
plutil -create xml1 ~/Library/Preferences/maui/Settings.plist
/usr/libexec/PlistBuddy -c "Add :AppleSdkRoot string /Applications/Xcode.app" ~/Library/Preferences/maui/Settings.plist
```

### Android

Android builds require a Java runtime and Android SDK. The usual route is to install Android Studio, then install the Android SDK from Android Studio's SDK Manager.

If the Android SDK is in a nonstandard location, pass it to the build:

```bash
dotnet build GameLogBook.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android -p:AndroidSdkDirectory=/path/to/android/sdk -p:JavaSdkDirectory=/path/to/jdk
```

On the current development Mac, Android Studio provides both paths:

```bash
dotnet build GameLogBook.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android -p:AndroidSdkDirectory=/Users/kiradinan/Library/Android/sdk -p:JavaSdkDirectory="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
```

## Build

Build the macOS target:

```bash
dotnet build GameLogBook.csproj -f net10.0-maccatalyst -p:TargetFrameworks=net10.0-maccatalyst
```

Build the Android target:

```bash
dotnet build GameLogBook.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android
```

Build all currently enabled local targets:

```bash
dotnet build GameLogBook.csproj
```

iOS is opt-in until the Xcode iOS platform component is installed:

```bash
dotnet build GameLogBook.csproj -f net10.0-ios -p:EnableIosTarget=true
```

## Rider

Rider should use the .NET 10 SDK from `/usr/local/share/dotnet` and a version with .NET 10 support. If Rider shows unresolved symbols after setup, run `dotnet restore GameLogBook.csproj` successfully, then reload the project in Rider or invalidate caches.

The default target frameworks are Android and Mac Catalyst, because those are the installed local targets. iOS can be enabled with `EnableIosTarget=true` after installing the iOS platform component in Xcode.

## Notes

- The previous ASP.NET Core web server entrypoint has been replaced by `MauiProgram`.
- Razor components are hosted by `BlazorWebView` through `wwwroot/index.html`.
- SQLite uses `FileSystem.AppDataDirectory`, so each device gets its own local database.
- Future cloud sync or a hosted web app should be added as a separate project that shares UI/data code with this app.
