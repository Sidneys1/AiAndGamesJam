# ANTS

Today, you make the decisions!

## Gameplay

Each level contains a randomly generated playing field, goal and, potentially, adversaries, and is generally more
difficult than the last.

You as the player coordinate Ant's queue of Jobs. Each Job has a priority (e.g., "x2") and idling Ants will randomly
select a Job from the list. Higher priority Jobs have a higher chance of being selected. To create Jobs, see the
Controls section below.

Anthills can be given tasks such as creating more Ants, or stockpiling Food.
To change an Anthill's task, select it and see the (C)reate and (S)tockpile controls below.
Creating more Ants consumes 20 food every 3 seconds as long as the Anthill has at least 25 Food stored.

### Sandbox Mode

Unfortunately I was unable to finish this game fully in time. Notable planned features that didn't make it are:

* More game goals than just "number of ants" and "stockpile of food".
* Enemy Ant colonies with complex AI.
* The ability to build new Anthills and other structures.

For now I've worked this around into more of a "sandbox" that you can enable with F5. Sandbox disables the Win/Lose
conditions and allows you to freely place certain objects, as well as spawn in more Ants. You can spawn Fire Ants
(enemies), however they won't do anything besides attack your anthills.

## Controls

|                Key | Description                                                                                     |
| -----------------: | :---------------------------------------------------------------------------------------------- |
|                ESC | Exit the game.                                                                                  |
|              Enter | Skip cutscenes.                                                                                 |
|                 F1 | Enable debug overlay.                                                                           |
|  Media-Previous/F2 | Previous music track.                                                                           |
|      Media-Next/F3 | Next music track.                                                                               |
| Media-PlayPause/F4 | Play/Pause music track.                                                                         |
|                F11 | Toggle fullscreen.                                                                              |
|                Tab | Select next Anthill or Ant.                                                                     |
|                  H | Select next Ant(h)ill.                                                                          |
|                  A | Select next (A)nt.                                                                              |
|                  I | Select next (i)dle Ant.                                                                         |
|                  F | Select next (F)ood item.                                                                        |
|                  G | Create Job to (G)ather Food. If no Food item is selected, Ants will gather the nearest to them. |
|                  D | Create Job to (d)istribute Food.                                                                |
|                Del | Delete selected Job.                                                                            |
|                  C | Task selected Anthill to (C)reate more Ants.                                                    |
|                  S | Task selected Anthill to (S)tockpile Food.                                                      |
|         Left Mouse | Select under the cursor (including Jobs in the menu).                                           |

### Additional Sandbox Controls

|         Key | Description                             |
| ----------: | :-------------------------------------- |
|          F5 | Enable Sandbox mode.                    |
|          F6 | Hold down to spawn Ants.                |
|          F7 | Hold down to spawn Fire Ants (enemies). |
| Mouse Wheel | Change pallette selection.              |
| Right Mouse | Place Pallette selection.               |
