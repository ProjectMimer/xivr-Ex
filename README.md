XIVR-Ex Dawntrail Source Code Release

→Updated to .Net 8 and Dalamud v10
→Updated for Dawntrail
→Implemented new rendering for Dawntrail

Readers of the chat already know, but after updating the mod for the next graphics update we ran into heavy performance issues.
At first we only were able to get 80 fps (40 fps in 3D) but after implementing all the other features like the headset res, FOV and so on the performance went down to 50 fps which is 25 fps in 3D.
Even with mono rendering 50 fps still wouldn't be enough for a good VR experience, and not to forget mono requires a higher resolution and would lower the fps even more on top of that.
For reference this was on a Ryzen 7950x3D with a 3090 and 6400mhz DDR5 RAM.

Thus we have decided that with the power needed for the new graphics and current hardware performance this mod is not feasible and we will stop working on it for now.
Though in the spirit of open source we have decided to release the source of all the changes needed for Dawntrail in case anyone else wants to give it a crack.
Be aware while all features are in and working in code they aren't polished or adjusted for the new  expansion and thus might not function.
