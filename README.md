# Desktop Image Frame
A MyToolBar extension for displaying images on the desktop.

![screenshot](https://raw.githubusercontent.com/TwilightLemon/Data/refs/heads/master/DesktopImgFrame.jpg)

## Features

- Display a slideshow of images on the desktop in SILENCE.
- Support arbitrary zoom and pan.
- More...

## Install

1. Clone this repository and build with `dotnet build`.
2. Copy the main dll from `\bin\Debug\net8.0-windows\DesktopImgFrame.dll` to your MyToolBar extensions directory.  
   A possible directory structure is:  
   ```
   MyToolBar(Install location)
   └── Plugins
       └── DesktopImgFrame
           └── DesktopImgFrame.dll
   ```
3. Restart MyToolBar to load the new extension.
4. Enable the Desktop Image Frame extension in the Service page.