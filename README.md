TLDR - How to use the app:

1. Select the Album folder to copy. (Eg. "E:\Nintendo\Album", or copy the album to your PC and then refer that folder)
2. Select the target folder to copy into.
3. Manage your copy settings.
4. Scan codes to find unknown codes in the original album.
5. Manually add unknown codes to "GameCodes.txt" (See "Game categories and customizing" below)
6. Start!

____

Settings explanation

* Maintain date folders
	- Keep part, or the entirety, of the date folder structure seen in the SD card album.

* Separate shots and clips
	- Separate screenshots and clips into two different folders.

* Create album folder
	- Will add a new folder to the "Copy to:" directory
	E.g Copy to: "C:\Switch" + Create album folder: "Album" = "C:\Switch\Album"
	- Unchecking this option will copy the album content directly to the destination folder.

* Include 'Extra' folder
	- This will include content from the Extra folder (E.g - Smash Ultimate Replays)
	- You can opt to keep the extra folder separate from other clips/screenshots
	- If you know of other games that utilize the Extra folder please inform me so that I may update the tool if necessary.

* Organize by game
	- Separate screenshots and clips by game.
	- This setting will take all the other settings into account, separating by dates, screenshots and clips and extra folder.
	- You can choose to copy all content or one particular game.


These settings take precedence, when applicable, in the following order:

[TargetDirectory] \ AlbumName \ GameCategory \ Extra? \ ScreenshotOrClip \ Date \ content.jpgORmp4

____

Data and duplicates

- This tool will not erase any data, both from the original album folder nor the destination folder.

- The settings you define in the tool will be saved automatically, and overwritten, when you successfully start copying an album.
	These settings will not be saved when exiting the application.

- The next time you copy your SD Card album you can copy it directly into the same folder as you did previously, as long as you utilize the exact same settings as before. Under those circumstances the tool will automatically skip files with the same name as the originals.
- Also, if you copy an album that has already been partially copied before (E.g - if you did not clear your SD Card) the tool will skip already copied content and will go straight to the new content.

____

Game categories and customizing:

- The SwitchSDCopy app will allow you to organize your content by game category.
- Switch content file names contain the following data:
	Year Month Day Hour Minute Second TimestampCounter - Gamecode
	Eg. 2017120623304800-397A963DA4660090D65D330174AC6B04.jpg -> This would be a screenshot from 2017/12/06, 23:30:48, 0 counter - Splatoon 2.
	(If you're curious, the timestamp counter is used to differentiate screenshots taken with the same timestamp.)

- The file named "GameCodes.txt" contains a list of "[Code] [Game title]" that will be used to categorize your content.
  The game title is the name that will be given to the corresponding game folder.
  If different codes have the same game title their content will be placed under the same folder.
     By default Splatoon 2 and Splatoon 2 (Demo) are arranged in this way.
  You can also append folders to the game title. By default the Super Smash Bros Ultimate replays are placed into a Replays folder within the game category.
  You are free to add, remove or modify these entries at your own discretion.

- If any of your owned games is not in the "GameCodes.txt" file, you can use the Scan Codes option to automatically retrieve unknown codes.
  You will then have to manually add them to this file in the same format -> "CODE Title"

- When copying the album, any files with unknown game codes will be placed in a directory named "Other".
