# Roulette Data Collector
This is an FFXIV Dalamud plugin that collects data on duties that you participate in.

## Why data?
I wanted an easy way to get statistics on Mentor roulettes.

Also a project to get practice with databases.

## Where data?
I do not plan to move the data anywhere. It wil be stored in a local database where the person collecting can do whatever they want with it.

**Remember to always follow FFXIV rules, do not use data to harrass other players in game or out of game.**

## What data?
Data the plugin currently gets
- Queue duration
- Duty name
- Duty duration
- Resolution (cleared or not)
- Number of wipes
- Party info
    - Player names
    - Class
    - Level (while synced)
    - Gear* you have to press the inspect button in the config window to inspect players and it is one-at-a-time so just keep pressing until remaining players is 0.

Data I want to get at some point
- Resolution (vote-abandon)
- Player lodestone id or integration with PlayerTrack such that data could be uniquely linked to players
- Flag which player is the one collecting data
- Calculate item level
- Flag which players leave the instance before a clear


## Thanks
Plugins that I used as learning reference while creating this one.
- [HimbeertoniRaidTool](https://github.com/Koenari/HimbeertoniRaidTool)
- [PlayerTrack](https://github.com/kalilistic/PlayerTrack)

And lots of help from random years old messages in the Dalamud discord. I'm sure that can't cause issues.

~~I never realized how obviously french the word roulette is until I had to write it this much~~
