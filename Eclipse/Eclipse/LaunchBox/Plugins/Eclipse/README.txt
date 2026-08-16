Eclipse is a plug-in and theme for Big Box that features voice search, random game selection, and a Netflix style interface. 

Demo
https://youtu.be/rcrl4AN2Jsw

Source Code
If you're interested in the source code, it's not pretty but it's available here: 
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
10. Options > Theme-Specific Options > Eclipse > Views > Platform List View: Platform Wheel 1
11. If you want to use the startup theme: Options > Theme-Specific Options > Eclipse > Game Startup
	- Show "Loading Game..." Message: Off
	- Enable Startup Screen: On
	- Startup Theme: Eclipse
	- Minimum Startup Screen Display Time: 5 seconds
	- Minimum Shutdown Screen Display Time: 5 seconds
	- Hide Mouse Cursor on Startup Screens: On

Logs
If you get any errors, you can check the log file called Eclipse.txt in your LaunchBox folder and message me on the forums to let me know

Settings
As of version 0.0.10, there is a settings screen that can be accessed from LaunchBox via Tools > Manage Eclipse

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