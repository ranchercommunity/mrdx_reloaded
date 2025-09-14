# Infinite Freezer - v0.1.2

* Allows for save files to have unlimited freezer space.
* Manually change/create new freezers using Left and Right DPAD on the revive or freeze menus.
* Freezers can be shared with other users of the mod (be sure to not overwrite your existing data)!



**Please keep in mind while this mod has been tested and has not encountered issues, it does modify your freezer!**

**Especially for initial uses, you may want to make a backup of your saves, or at minimum, copy your save to**

**another slot and only utilize the mod on that slot until additional users have tried the mod.**



### Freezer File Notes

* Freezer data is saved to the MR2DX save folder.
* Freezers can be renamed by altering bytes 12-28 of the freezer files.
* The file contains a 528 byte header, followed by 528 blocks for each of the 20 freezer slots.
* Freezer files can be transferred or backed up. Be sure to not overwrite existing freezer data (the numerical ID at the end of freezer files indicates the freezer group).
* For VS Mode, only the 'active' freezer is the one that is uploaded.

 

### Latest Updates

##### v0.1.1 - v0.1.2

* v0.1.2 - Hotfix for freezer helper text not updating.
* Fixed an issue where deleting or selling monsters was not updating the freezer properly.
* Fixed an issue where freezer groups could not be changed when trying to delete a monster.
* 'Fixed' an issue where combining monsters across multiple freezer groups could cause crashes or combine the wrong monsters.



*Note: The above fix prevents the swapping of freezer groups when combining. A warning message is displayed as well.*

*As such, when combining monsters, the monsters must be in the same freezer group.*



##### v0.1.0

* Initial Release



### Planned Updates

* Backend improvements for ease of development.
* Customizable prompts for changing the active freezer group.
