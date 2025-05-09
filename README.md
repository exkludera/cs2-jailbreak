# Jailbreak
Often used with [CTBans](https://github.com/playpark/cs2-ctbans)

![CS2](https://img.shields.io/badge/Game-CS2-orange)
![CounterStrikeSharp](https://img.shields.io/badge/API-CounterStrikeSharp-blue)

### Core Features
- **Warden System**: Complete warden management with commands, laser pointer, and team control
- **Last Request (LR)**: Multiple LR types with stats tracking via SQL
- **Special Days**: Various special day events to keep gameplay fresh
- **Team Management**: CT queue system, team ratio enforcement, and swap functionality
- **Admin Controls**: Comprehensive admin commands for server management
- **CT Queue**: Queue system for CTs to join the game
- **CT Ban Integration**: Integration with CT Bans for banning CTs from the queue

## Installation

1. Download the latest release from the [releases tab](https://github.com/playpark/cs2-jailbreak/releases)
2. Extract the plugin into your CounterStrikeSharp plugins folder
3. Configure the plugin settings in `configs/plugins/Cs2Jailbreak/`

### Database Setup (Optional)
For LR stats tracking:
1. Create a MySQL database
2. Configure database credentials in the plugin config file

> [!IMPORTANT]
> This plugin is optimized for Linux servers. Windows support is limited with some features disabled.

## Commands

### Player Commands
- `!w` - Become warden
- `!uw` - Leave warden position
- `!wb` - Toggle block status
- `!color` - Set prisoner color
- `!laser_color` - Set laser color
- `!marker_color` - Set marker color
- `!lr` - Start a last request
- `!ctqueue` or `!ct` - Join CT queue
- `!leavequeue` - Leave CT queue
- `!listqueue` - View current queue

### Warden Commands
- `!wd` - Start a warday
- `!swap_guard <player>` - Swap a T to CT
- `!wsd` - Call a warden special day
- `!give_pardon` - Pardon a rebel
- `!give_freeday` - Give a T a freeday
- `!countdown` - Start a countdown
- `!countdown_abort` - Abort current countdown
- `!force_open` - Toggle doors

### Admin Commands
- `!rw` - Remove current warden
- `!swapguard` - Force swap a player to guard
- `!fire_guard` - Remove a player from guard
- `!sd` - Start a special day
- `!sd_ff` - Start a friendly fire special day
- `!sd_cancel` - Cancel current special day
- `!logs` - View warden logs

Admin commands require `@css/generic` permission.
Debug commands require `@jail/debug` permission.

## Available Special Days
- Dodgeball
- Friendly Fire
- Grenade
- Gun Game
- Headshot Only
- Hide and Seek
- Juggernaut
- Knife
- Scout Knife
- Spectre
- Tank
- Zombie
- And more!

## Last Request Types
- Knife Fight
- Gun Toss
- Dodgeball
- No Scope
- War
- Grenade
- Russian Roulette
- Scout Knife
- Headshot Only
- Shot for Shot
- Rebel (Free Kill)

## API Usage
For developers interested in integrating with this plugin, see [JailServiceTest](https://github.com/destoer/JailServiceTest) for example API usage.

## Recommended Plugins
- [CT Bans](https://github.com/playpark/cs2-ctbans)

## License
This project is licensed under the terms of the included LICENSE file.

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.
