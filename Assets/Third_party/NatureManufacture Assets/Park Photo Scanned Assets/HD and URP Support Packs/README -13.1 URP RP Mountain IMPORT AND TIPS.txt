
BEFORE YOU START:
- you need Unity 2022.1+
- you need URP SRP pipline 13.1 if you use higher please import 13.1 support pack.
- wind setup is in wind prefab at each scene
Be patient URP RP tech is still fluid and fresh...

Step 1 - You can improve FPS amount by 30% if you change rendering path from forward to deferred at rendering setting. 
Find File "UniversalRenderer" and change Rendering path from forward to deferred. Forward render is ok too but it's slower for complex scenes

Step 2 - Setup Shadows and other render setups. Find File "UniversalRP-HighQuality" 
    - Change shadow distance to 300 or higer
	- change number of shadow cascades to 4
	- Turn on "Opaque Texture" this will fix water translucency and distortion
	- Turn on "Depth Texture" this will fix water visibility at playmode
	- Optionaly use 1k or 2k shadow resolution. We used 2k.
	- Turn on HDR if its turned off

Step 3 Go to project settings: 
    - Player and set:  Color Space to Linear
    - Quality settings: Go to quality settings and: 
	     * use ultra level 
	     * turn turn off vsync
		 * lod bias should be around 1.5-2 and 1 for low end devices.
                        

Step 4 Find "Park_Demo" and open it.

Step 5 - HIT PLAY!:)

Step 6 -  Make note that unity often compile shaders even after you hit play for long time, so performance will rise up after unity end shader compilation
Wait a moment until it end. 

About scene construction:
		- There is post process profile: Manage post process by scene post process object.
		- Prefab wind manage wind speed and direction at the scene

