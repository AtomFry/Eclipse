Eclipse is a plug-in and theme for Big Box that features voice search, random game selection, and a Netflix style interface. 

Here's a demo:
https://youtu.be/rcrl4AN2Jsw

WARNING
This theme/plug-in may not be for you. If you have a large library, a slow computer, or are looking for a hands off, plug-and-play theme, this might not be the theme for you.  Reasons why you may not want to use this theme:

1. Startup time on initial load due to image scaling
- It takes a significant amount of time to load up the first time
- The amount of time will depend on your machine and the number of box front images you have in your LaunchBox\Images folder
- What's going on here is, in order to improve the overall user experience while you are in the theme, the box front images are copied and scaled to the right size for your monitor so that the program doesn't have to scale them while you are cycling through them 
- Initial load for my old laptop with 1500 games and 5600 box front images was 10 minutes
- Each subsequent load only has to copy/scale any new images that you've added. If you add a new game or even 100 new games, those will process pretty quickly.

2. Startup time in general
- Most big box themes take around 5-10 seconds to load up for me
- This plug-in takes around 45 seconds to load up on my old laptop

Source Code
If you're interested in the source code, it's (not pretty and it's) available here: 
https://github.com/AtomFry/Eclipse

Installation Instructions
1. Download: https://forums.launchbox-app.com/files/file/3220-eclipse/
2. Extract the contents to a folder
3. Inside the Eclipse folder is a folder called LaunchBox
4. Copy the Plugins, StartupThemes, and Themes folders
5. Go to your Launch Box installation folder and paste the copied folders
6. Open Big Box
7. Esc to get to options
8. Select Options > Views
9. Set Theme to Eclipse
10. Set Platforms List View to Platform Wheel 1
11. If you want to use the startup theme, Options > Game Startup
	- Uncheck Show "Loading Game..." Message
	- Check Enable Startup Screen
	- Startup Theme: Eclipse
	- Minimum Startup Screen Display Time: 5 seconds
	- Maximum Startup screen Display Time: 5 seconds
	- Check Hide Mouse Cursor on Startup Screens
12. Go back - the first time the theme starts will take a long load time to generate the image cache
13. NOTE - If you get any errors, you can check the log file called Eclipse.txt in your LaunchBox folder and message me on the forums to let me know

General usage
- Up, Down, Left, Right - moves around
- Enter - selects something
- Escape - cancel or go back - pressing it will get you back to the BigBox settings where you can exit the application
- Page Up - pick a random game
- Page Down - voice search

Bezels
- Bezel images can be displayed around the preview videos
- The system will first look for a game specific bezel. If not found, it will look for a platform specific bezel. If not found, it will look for a default bezel. 
- A few default bezels are provided with the installation. You can delete them from the folders specified below if you prefer the videos without bezels.

Game specific bezels
- The system tries to find a game specific bezel image in the following order:

1. In plug-in media directory:
..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\{CleanGameTitle}.png

- Here {CleanGameTitle} replaces any invalid characters with an underscore. Characters like ' and : cannot appear in file names so they are replaced with an underscore
- For example: A bezel file for the game "19XX: The War Against Destiny" should have the following path and file name ..\LaunchBox\Plugins\Eclipse\Media\Images\Arcade\Bezel\19XX_ The War Against Destiny.png

MAME bezels
- If a game specific bezel isn't found in the plug-ins media folder as described above, then the program will look into the MAME installation folder. In order for MAME bezels to work, installing the bezel project for MAME would create files with this structure:

..\LaunchBox\Emulators\MAME\artwork{game.ApplicationFilePath}"Bezel.png"

Retroarch bezels
- Bezels installed by the bezel project for retroarch will go into a folder location like this:

..\LaunchBox\Emulators\Retroarch\overlays\GameBezels{RetroarchPlatform}{game.ApplicationFilePath}.png

Platform specific bezels
- These are used if there are no game specific bezels found
- You can specify a different image for horizontally and vertically oriented games so that they fit appropriately
- The platform specific bezel image files must have the following file names and locations:
..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Horizontal.png
..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Vertical.png

System default bezels
- These are used if there are no game specific or platform specific bezels found
- You can specify a different image for horizontally and vertically oriented games so that they fit appropriately
- The default bezel image files must have the following file name and location:
..\LaunchBox\Plugins\Eclipse\Media\Bezels\Platforms\Default\Horizontal.png
..\LaunchBox\Plugins\Eclipse\Media\Bezels\Platforms\Default\Vertical.png