# Outline of stages.
1. Tilemap: (probably going to be mostly done just within the godot editor, at least initially)
2. Pathfinding: I want to implement my own version at some point but should I experiment with using the inbuilt A\* version first? dijkstra
   algorithm?
3. Procedural tilemap generation (impliment a basic algorithm in both C# and F#)
4. Basic A.I: Thinking of doing a simple finite state machine, but I think each language might have preferential implimentations, Should I
   aim for just one common implimentation or maybe one showing of the strengths of each
5. Emergent systems: Using the rules and type systems of F# I think I can far more easily design a series of emergent mechanics.

I want to at first express each of these stages as verbal rules, then describe how I tackle them under each language.

## Stage 1: Tilemap - tick
Decided to try and use as much of the inbuilt tilemap system as possible, for tasks such as movement and collision. However for the more
game design elements, such as position and generation, do that externally. Therefore at this point I can do without writing out design rules
for the tiles.

I'm just going to go for a simple system of tiles for floor, walls and doors, adding additional complexity later as needed, as time allows.

## Stage 2: Pathfinding - tickish
I'm going to quickly experiment with the inbuilt pathfinding system, though I'll probably resort to a basic A\* algorithm of my own, we'll
see. I might also skip this stage somewhat to instead go straight to stage 3 as I think it'll be more meaningful.

## Stage 3: Procedural content - bypassing for now
This will be where more meaningful work on language specific systems can be performed. I'm going to try and generate some basic dungeon of
tiles. Start basic and add complexity as time allows. Maybe utilise Godots inbuilt noise system?

## Stage 4: Basic A.I
Have a couple of primary "needs/wants" and evaluate them every so often. Then have tasks that the A.I will pursue based on those concerns.
However each action should send some sort of signal on completion (and maybe start for exclusive actions?) which other A.I can receive
and also use to modify behaviour. I'm thinking to begin with just having 2 discrete forms of need, one to move (standing as probably a
placeholder for other needs) and one to rest. Then I can get it to walk about and rest.
### Needs/wants system
I think I want to make every need/want some sort of bar that either grows or shrinks. As it passes through different barriers different
events could be triggered. Different needs/wants should decay up or down, e.g sleep/rest should decay down and boredom should decay up.
Ideally I want to set min and max values, and the steps, maybe as points in an array (C# or GD collection?). Do I want a dictionary or list
for holding the different needs? I'm thinking a dictionary which can be iterated over or target specific elements. I think I should be able
to use an enum to help search for the targeted one.


# Game design stuffs
## Themes
1. Sandbox: Strong compelling given the focus on emergent systems, just have a degree of directional freedom lends itself well to this.
2. Lets try this again: A repeat mechanic could be very doable in FP land and pretty cool.
3. Violence replaced with non-violence: Focus on different problem solving and objective.
