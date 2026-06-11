import sys

path = r'e:\LiveWallpaperApp\Views\MainWindow.xaml'
with open(path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

dash_start = 152
dash_end = 244
lib_start = 245
lib_end = 352

print("Dashboard Start:", lines[dash_start].strip())
print("Dashboard End:", lines[dash_end-1].strip())
print("Library Start:", lines[lib_start].strip())
print("Library End:", lines[lib_end-1].strip())
