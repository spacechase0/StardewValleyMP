**Makeshift Multiplayer** is an open-source [Stardew Valley](http://stardewvalley.net/) mod which
adds multiplayer to the game while we wait for official multiplayer.

## Install
1. [Install the latest version of SMAPI](https://smapi.io/).
2. [Install this mod from Nexus mods](http://www.nexusmods.com/stardewvalley/mods/501/).
3. Run the game using SMAPI.

Here's how to set up a multiplayer game using Hamachi (but feel free to use port forwarding or
another VPN if you want). All players should follow these instructions.

1. Run the game once to generate a `config.json` in the mod folder.
2. Install [Hamachi](https://www.vpn.net).
3. **main player only:** Click 'create new network' in Hamachi, and note down the exact "Network ID Name" and "password".
4. **main player only:** Find the "IP" on the right side of the power button in the Hamachi menu.
5. Open `Stardew Valley/Mods/StardewValleyMP/config.json` in a text editor, find `"DefaultIP": "YOUR IP HERE"`, and change `YOUR IP HERE` to the host's IP from step 4.
6. **clients only:** In Hamachi, connect to the host using the "Network ID Name" and "password" from step 3.
7. Make sure Hamachi is running, and then launch the game with SMAPI.
8. **main player only:** After pressing 'Listen', wait for all of your players to connect, and press 'Start'.

## Compatibility
Makeshift Multiplayer is compatible with Stardew Valley 1.2 on Linux/Mac/Windows. If any of the
players are on Linux/Mac, make sure everyone opens the `config.json` file in a text editor and
sets `Compress` to `false`.

## FAQs
### Is this official multiplayer?
Nope. That's expected in Stardew Valley 1.3, sometime in 2018.

### What platforms is this mod compatible with?
It's compatible with Linux + Mac + Windows, and you can play crossplatform. 

### Can I use other mods in multiplayer?
Visual mods should be fine (including Lookup Anything, portrait mods, etc), though only you will
see them. Mods which change the game data can cause problems (e.g. Automate, Chests Anywhere, CJB
Cheats Menu, etc).

### What's shared?
The following is shared (based on the host):
* most places (including outside, town, inside shops, and the library);
* money;
* time / pausing;
* carpenter (Robin can only build one thing at a time; she may not actually appear, but it'll still be built);
* community center bundles.

Each player has a separate version of...
* mine & skull cavern levels;
* player stats;
* blacksmith;
* backpack upgrade level;
* inventory;
* inside house;
* relationships;
* events (e.g. each farmer plays the egg hunt minigame separately);
* stardrops;
* quests from Pierre's noticeboard;
* cutscenes & heart events.

Some story quests are shared and some aren't.

## See also
* [My site](http://spacechase0.com/mods/stardew-valley/makeshift-multiplayer/)
* [Nexus](http://www.nexusmods.com/stardewvalley/mods/501/)
* [Chucklefish forums](http://community.playstarbound.com/resources/makeshift-multiplayer.3796/)
