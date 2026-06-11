# Assets

Place app icons, thumbnail placeholders, wallpaper-pack art, and future shader assets here.

The current tray implementation uses `System.Drawing.SystemIcons.Application` so the app can build
without requiring a binary `.ico` asset. For production branding, add an `.ico` file and set
`<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>` in `LiveWallpaperApp.csproj`.
