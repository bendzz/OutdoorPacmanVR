(GIF)

![](https://github.com/bendzz/OutdoorPacmanVR/blob/main/0_WIP/Gif%20(1220).gif)

README

Life sized pacman VR! Run around on an open field dodging the ghosts in real life! Grow the map even larger so you really have to sprint away from them, or shrink it down to a board game to play in your living room.

CONTROLS

	TRIGGERS pause the game:

		-LEFT TRIGGER: 
		
			-Minimizes map
			
			-JOYSTICK up and down to adjust ghost speed (Blinky demonstrates)
			
		-RIGHT TRIGGER:
		
			-JOYSTICK up and down to scale game board big and small
			
	A BUTTON:
	
		-Removes the player movement 2x speed multiplier, as long as it's held.
		
		-(Releasing causes the player to jerk over to where they'd normally be)

NOTES:

-Not entirely finished; it only has 1 level and winning or dying don't actually end it. (Just crank the ghost speeds up to simulate later levels). It's also missing some nuances of the original like cherries and some AI glitches. But it's pretty authentic; The pixel perfect map, ghost AI and spawning is all about right.

-Yes you can walk through walls. Even if I enforce rules here later I'll make them optional; it's fun =)

-(Haven't actually tested this at full scale yet in an outdoor field; use at your own risk. -July 14 '22)

-Oculus Quests only; only tested on a Quest 2.


DEVELOPERS

Made in unity 2021.1.15f1. 

The most valuable piece of code is the GPU Instancing system; It allows me to create over 4000 meshes to make the maze walls and dots etc, and to individually animate them, with almost no impact on the quest 2's perf. (The frag shader to draw lights through the walls drops the framerate sometimes though; it's very hacky). Unity's GPU instancing system only gets like 25 fps on the quest meanwhile. It took a while to iron out the bugs so enjoy it!
