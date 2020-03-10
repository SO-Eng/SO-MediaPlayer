# SoftwOrt-Mediaplayer

SoftwOrt-Mediaplayer is a Media- & Webradioplayer.

## Installation

Use the exe file to install SoftwOrt-Mediaplayer


[setup.exe](https://github.com/SoftwOrt-Engineering/SoftwOrt-Mediaplayer/blob/master/setup.exe)

If you have no rights to install on your Computer? Use the [ZIP](https://github.com/SoftwOrt-Engineering/SoftwOrt-Mediaplayer/blob/master/SoftwOrt-Mediaplayer.zip) file
and unzip in a folder of your choice.

## Features

Besides the normal features you know about a simple mediaplayer like:

- Open folders
- Drag&Drop files into Player
- Show them in a list to select
- Controll player with keyboard and mouse
	- Up-, Down arrowkeys = Skip For-, Backward
	- Left, Right arrowkeys = jump 10sec. For-, Backward
	- Space = Play and Pause track
	- Mousewheel Up, Down = Volumecontrol
- Thumbbuttons in Taskbar for Play/Pause, skip For- and Backward
- Loop, Loop 1 and Shuffle

A big feature is the Webradioplayer with following features:

- Big list of radiostations
- Create and manage a list of your favorites
- Add radiostations of your choice
- Search in internal list

Settings:

- Choose between 3 languages
	- English, German, Spanish
- Choose between 2 viewmodes
	- Light-, Darkmode
- Choose between 3 controlbutton designs
	- Blue, Futuristic, Gray

- Collapse lists for individual listcontrol

## Information

When adding a radiostation be sure to use directlinks.
	For example, a link like this won't work:
	https://www.radio.de/s/hr3
	
	Please try to find directlinks like this:
	http://metafiles.gl-systemhaus.de/hr/hr3_2.m3u
Because it is smoetimes hard to find correct link, there is a "Prelisten" function implemented.
Also SSL URLs won´t work properly, mostly you can reach radiostation per URL when trim to 'http://' instead of 'https://'

Added wrong Radiostaion or you have a typo? You can uncheck "Save Webstation-list at exit" before closing
the player, your new list won´t be saved!
You can also editing the list by jump into folder "RadioStations" where application is installed and open
"RadioStation-List.csv" file.
Be sure to never delete headerrow and have no empty rows before last row.

## License

[GNU](https://choosealicense.com/licenses/gpl-3.0/)
