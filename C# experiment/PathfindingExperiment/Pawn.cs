using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Pawn : Area2D
{
    //Signals first?
    // State Restult Signal
    [Signal]
    public delegate void TaskExit(bool exitState);

    //Iv'e decided it's better/more readable to have 2 signals, one to add to the buffer and one to remove.
    [Signal]
    public delegate void TaskBufferAdd(string[] tasks);
    [Signal]
    public delegate void TaskBufferRemove(string[] tasks);

    [Export]
    private int needFrameSkip; // This is a magic number to dictate how many frames the loop should skip for doing the need calc
    private int frameCount;
    [Export]
    private int speed;

    [Export]
    private Vector2 velocity = new Vector2();

    [Export]
    private Vector2 targetDestination; //Where the pawn is currently trying to reach. When equal to the pawns current position, it has no movement goal.

    private PathfindingTilemapExperiment pathProvider;

    private Line2D visAid;

    //these 2 are used to step through a path.
    private int currentPoint;
    private Vector2[] currentPath;
    public Vector2[] CurrentPath
    {
        get
        {
            return this.currentPath;
        }
        set
        {
            //this currently has no security check for being passed an empty array...
            this.currentPath = value; //assign the private value
            if (currentPath.Length > 1)
            {
                targetDestination = currentPath[currentPath.Length-1];
                currentPoint = 1;
                foreach (Vector2 point in currentPath)
                {
                    GD.Print("recieved path points: ", point.ToString());
                    //visAid.AddPoint(point + this.GlobalPosition);
                }
                visAid.Points = value;
                foreach (Vector2 point in visAid.Points)
                {
                    GD.Print("drawing path points: ", point.ToString());
                }
            }
            else
            {
                targetDestination = this.Position;
                currentPoint = 0;
                visAid.ClearPoints();
            }
        }
    }

    //used to calc random wander
    [Export]
    private Vector2 wanderOriginOffset;
    [Export]
    private Vector2 wanderRectSize;

    // Want to devise how the need system works, which should be the primary system to triger states.
    // The common characteristics of the needs
    enum NeedTraits
    {
        // Name, // Unecessary I think
        OptimumValue,
        WorstValue,
        //StepPoints, // Won't work as I want it to be an array or list
        DecayDirection,
        DecayRate,
        CurrentDirection
    }
    // The names of the various needs
    enum NeedNames
    {
        Rest,
        Boredom
    }

    //A tracker of the current targeted need/state?
    private NeedNames currentTargetNeed = NeedNames.Boredom;

    private Dictionary<NeedNames, float> needStates
     = new Dictionary<NeedNames, float>
    {
        {NeedNames.Rest, 5.0f},
        {NeedNames.Boredom, 5.0f}
    };

    // I'm thinking maybe I should make this a static reference
    // Store things like current decay direction within the states dictionary...
    private Dictionary<NeedNames, Dictionary<NeedTraits, float>> needsCollections
     = new Dictionary<NeedNames, Dictionary<NeedTraits, float>>()
    {
        {
            NeedNames.Rest,
            new Dictionary<NeedTraits, float>
            {
                {NeedTraits.OptimumValue, 0.0f},
                {NeedTraits.WorstValue, 10.0f},
                {NeedTraits.DecayDirection, -1.0f}, //Going to change things to use +=, so worsens going down
                {NeedTraits.DecayRate, 0.1f},
                {NeedTraits.CurrentDirection, 1.0f} // Keep this sort of "fake" static                    
            }
        },
        {
            NeedNames.Boredom,
            new Dictionary<NeedTraits, float>
            {
                {NeedTraits.OptimumValue, 0.0f},
                {NeedTraits.WorstValue, 10.0f},
                {NeedTraits.DecayDirection, 1.0f}, //Going to change things to use +=, so worsens going up
                {NeedTraits.DecayRate, 0.05f},
                {NeedTraits.CurrentDirection, 1.0f} // So for now since I'm just defaulting to the rest action I want to make this positive
            }
        },
    };


    [Export]
    private List<string> actionBuffer = new List<string>();

    private Vector2 targetTilePos = new Vector2();
    
    public override void _Ready()
    {
        //var actionForm = (name: "string", blocking: true, target)
        base._Ready(); // Ensures the basic ready has completed
        AddToGroup("Pawns"); //hardcoded group and might not even be the best solution, but should do for now.

        // Quick hack to scatter initial values for demo purposes
        if (true)
        {
            RandomNumberGenerator rand = new RandomNumberGenerator();
            rand.Randomize();
            List<NeedNames> needStatesKeys = new List<NeedNames>(needStates.Keys);
            foreach (var key in needStatesKeys)
            {
                needStates[key] += rand.RandfRange(-2.0f,2.0f);
            }
        }
        this.Connect("TaskExit", this, "ChooseTask");
        needFrameSkip = 60;
        frameCount = 0;

        GD.Print("doing ready on " + this.Name);
        visAid = GetNode("visualAid") as Line2D;
        visAid.SetAsToplevel(true);
        if (GetParent() is PathfindingTilemapExperiment)
        {
            pathProvider = GetParent<PathfindingTilemapExperiment>(); // I should really break up path provider into 2 parts. A path provider and a map interactor.
        }
        else
        {
            GD.PrintErr("The parent was wrong!");
        }
        //hardcoded set for now until I provide some thing to do this.
        Task task = DelayedReadyWorkAround("ready");
        //destination = this.Position;
    }

    private async Task DelayedReadyWorkAround(string signal)
    {
        // A hack to enable reuse of this function.
        // I CAN FEEL YOUR JUDGEMENT PLZ FORGIVE!
        if (signal == "ready")
        {
            await ToSignal(pathProvider, signal);
        }
        
        Vector2 targetPosition = ChooseWanderDestination(pathProvider.CalcValidArea(new Rect2(this.Position + wanderOriginOffset, wanderRectSize))); //make sure you don't subtract a negative vector you silly billy
        //CurrentPath = pathProvider.CalcPath(this.Position, targetDestination);
        CurrentPath = pathProvider.CalcPath(this.Position, targetPosition);
        if (CurrentPath[CurrentPath.Length - 1] != targetDestination) // I believe unecessary now
        {
            targetDestination = CurrentPath[CurrentPath.Length - 1];
        }
        GD.Print(pathProvider.CalcPoint(targetDestination));

        // The below forces it to only ever move to the closest wall tile.
        var tileChangeTest = ChangeTile(1,new Rect2(this.Position + wanderOriginOffset, wanderRectSize));
        //EmitSignal("TaskExit", false);
    }

    // Find which, if any need is the most critical
    private NeedNames EvaluateNeeds(Dictionary<NeedNames, float> currentStates, bool isInTaskCurrently=false)
    {
        var taskStickyness = 2.0f; // "Magic" number for how sticky an existing task just is in general
        // I need to make all states comparable (fix the directional flips) and then evaluate them
        // Setup a random gen to allow for some deviation, to be decided on later.
        RandomNumberGenerator rand = new RandomNumberGenerator();
        rand.Randomize();

        // Find whichever need is further from it's optimum state and swap to it preferably.
        var targetNeed = currentTargetNeed; // Establish the current working need to give it advantage? Allow for some need stickyness
        foreach (NeedNames need in currentStates.Keys)
        {
            //going to break up the comparison into a bunch of prior actions to help with reading and tweaking.
            if (need == targetNeed) {continue;}
            float evaluatedNeedScore = Math.Abs(needsCollections[targetNeed][NeedTraits.OptimumValue] - currentStates[targetNeed]); // First get the base value
            if (isInTaskCurrently) {evaluatedNeedScore -= taskStickyness;} // Then increase value if currently 
            if (true) {evaluatedNeedScore -= rand.RandfRange(0.0f,1.0f);} // Currently always add random spin but can be changed in future.
            if (Math.Abs(needsCollections[need][NeedTraits.OptimumValue] - currentStates[need]) < evaluatedNeedScore)
            {
                targetNeed = need;
            }

            // I need to check distence from minimum (how close to being depleted it is) or "maximum" in the case of something like bordom
            // In short we're checking for distance from optimum value, or distance to worst value.
            // The further we are, the worse things are.
            // if (need != targetNeed && 
            //     Math.Abs(needsCollections[need][NeedTraits.OptimumValue] - currentStates[need]) > (Math.Abs(needsCollections[targetNeed][NeedTraits.OptimumValue] - currentStates[targetNeed]) + rand.RandfRange(0.0f,10.0f))
            //     )
            // {
            //     targetNeed = need;
            // }
        }
        return targetNeed;
    }

    // I should have another method or fix/extend the one below to from a given need, find out which is the most relevent job.
    // There might be wisdom in changing this from void to a return type of state or task, to help keep track of things though unsure of how to do so currently.
    private void ChooseTask(bool isInTaskCurrently=false) // I changed the name because the pawn might remain in a given task/state upon evaluation
    {
        var chosenNeed = EvaluateNeeds(needStates, isInTaskCurrently);

        //Ideally I should use some sort of match function here but I'm too lazt atm
        if (chosenNeed == NeedNames.Rest)
        {
            GD.Print("choose rest");
            currentTargetNeed = NeedNames.Rest;
            var taskResult = Rest();
        } else
        {
            GD.Print("choose walk");
            currentTargetNeed = NeedNames.Boredom;
            var taskResult = Wander();
            Task task = DelayedReadyWorkAround("blah");
        }
    }

    // This function/method should allow for dictating a rest state.
    // So I'm not an expert and still trying to solve this but I think it would be better if I had some sort of return value for these.
    private bool Rest()
    {
        needsCollections[NeedNames.Rest][NeedTraits.CurrentDirection] = -1.0f; // Is growing
        needsCollections[NeedNames.Boredom][NeedTraits.CurrentDirection] = 1.0f; // Is decaying
        //currentDestination = this.Position;
        // This rotation method only works if you only trigger rest on changing from a different state!
        // DERP


        // I maybe should pull these changes/declarations into a separatChooseState()e function, but I'm not sure how to make it shorter. :/
        // I could maybe make these declarations dependent but that could have mistakes...
        // List<NeedNames> needStatesKeys = new List<NeedNames>(needStates.Keys);
        // foreach (var key in needStatesKeys)
        // {
        //     needsCollections[key][NeedTraits.DecayDirection] = needsCollections[key][NeedTraits.DecayDirection] * -1.0f; //This should invert the current states
        // }
        // needsCollections[NeedNames.Rest][NeedTraits.DecayDirection] = 1.0f; //set the various need rates
        // needsCollections[NeedNames.Bordom][NeedTraits.DecayDirection] = 1.0f; //set the various need rates
        return true;
    }

    // I'm thinking rather than having a return type, cause it to emmit a signal at the end?
    private bool Wander()
    {
        needsCollections[NeedNames.Rest][NeedTraits.CurrentDirection] = 1.0f; // Is decaying
        needsCollections[NeedNames.Boredom][NeedTraits.CurrentDirection] = -1.0f; // Is growing... (growing is a bad term)

        // First get an random destination
        Vector2 targetPosition = ChooseWanderDestination(pathProvider.CalcValidArea(new Rect2(this.Position + wanderOriginOffset, wanderRectSize))); //make sure you don't subtract a negative vector you silly billy
        // Second, calculate the path to walk from that destination
        //CurrentPath = pathProvider.CalcPath(this.Position, targetPosition);
        // There probably should be a third, to let you know it finished executing. Maybe I should make these states/tasks return true or false?
        //I'm going to experiment with that now
        return true;
    }

    private bool RandomConverse() // Just going to make this select a target at random for now.
    {
        Godot.Collections.Array pawns = GetTree().GetNodesInGroup("Pawns"); //get a list of all pawns
        pawns.Remove(this); //remove self from said list
        // Make the just the first pawn left, if there is at least one left, the conversation target.

        return true;

        Area2D EvaluateConverseOptions(string[] groups)
        {
            return new Area2D();
        }
    }

    // Silent again for this recording, at least to begin with.
    // I'm currently trying to work out how best to make plants.
    // Now I know I want interaction so it makes sense for them to be individual nodes. At the same time they, sensibly exist very linked to the tilemap.
    // So I'm currently going to do some testing with enabling pawns to edit the tilemap and see what that tells me.
    // I think I'll probably end up with some sort of hybrid system, where the tilemap changes cause a node/scene to be instanced at a location.
    //Assume you're always working out relative to self for now
    private bool ChangeTile(int tileID, Rect2 prematureOptimisation)
    {
        Vector2 targetPosition = GetNearestTilePosition(); //Find nearest tile
        if (targetPosition != targetTilePos) { targetTilePos = targetPosition; } //make it the target, if not already
        CurrentPath = pathProvider.CalcPath(this.Position, targetPosition); // Path to it.

        return true;
        // My brain is already thinking about optomisation problems, such as just searching for all tiles of a given type isn't exactly efficient but bleh, just do naive for now.
        // Just select a target cell first.
        Vector2 GetNearestTilePosition()
        {
            var targetDestination = pathProvider.GetClosestTargetTile(this.Position,tileID,5);
            return targetDestination;
        }
    }

    //Setting the needs rates should not be within an "evaluation" method, only those which actually cause an "action"
    // This function takes a random area and returns a valid point within it.
    private Vector2 ChooseWanderDestination(Rect2 possibleArea)
    {
        RandomNumberGenerator rand = new RandomNumberGenerator();
        rand.Randomize();
        int[] walkableTiles = new int[] { 0 }; //hardcoded garbo for now, think more about it later
        // point should now be somewhere within the possible area
        Vector2 point = new Vector2
            (
            rand.RandfRange(possibleArea.Position.x, possibleArea.End.x)
            , rand.RandfRange(possibleArea.Position.y, possibleArea.End.y)
            );
        return pathProvider.GetValidTile(point, walkableTiles);
    }

    // Here's a question, should I remove the idea of target destination and just use the last element in the path array? nvm
    // Walking a path shouldn't care about any specific task as many could include it, just if it current has a path to walk.
    private void WalkPath()
    {
        // GD.Print("point 0 before update", visAid.GetPointPosition(0).ToString());
        visAid.SetPointPosition(0, this.GlobalPosition); //Set path line origin to current point.
        // GD.Print("point 0 after update", visAid.GetPointPosition(0).ToString());
        if (this.Position.DistanceTo(CurrentPath[currentPoint]) < 1) // When you basically reach your current target point in a path
        {
            // This whole inner block of the parent if statement will only trigger when reaching a given point
            if (this.Position.DistanceTo(CurrentPath[CurrentPath.Length - 1]) > 1) // If the pawn has yet to reach its final destination 
            {
                if (currentPoint < CurrentPath.Length - 1) // Check to see how far along the path you are
                {
                    currentPoint++;
                }
                visAid.RemovePoint(1);
            }
            else //happens when you reach the final point.
            {
                bool tileChanged = pathProvider.ChangeTile(targetTilePos,0);
                CurrentPath = new Vector2[0];
                //EmitSignal("TaskExit", true);
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        //This movement function shouldn't care about state, though maybe care about needs? Clearly it should also be interuptable eventually
        // Should probably pull at least part of this logic into WalkPath() (or equivelent) method.
        if (CurrentPath.Length > 1 && needStates[NeedNames.Rest] > 0)
        {
            //GD.Print("current point is: ", currentPoint.ToString());
            // GD.Print(currentPoint.ToString());
            velocity = this.Position.DirectionTo(currentPath[currentPoint]);

            //GD.Print(velocity);item
            this.Position += (velocity * delta * speed);
            WalkPath();
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        frameCount++;
        if (frameCount == needFrameSkip)
        {
            //I'm going to end the recording here for now, I need to take a quick walk to come up with a better idea, cause this is just causing too many headaches...

            // So this I think is where the problem is. I just need to make sure it works correctly.
            List<NeedNames> needStatesKeys = new List<NeedNames>(needStates.Keys);
            foreach (var key in needStatesKeys)
            {
                GD.Print(key.ToString()
                    , ":"
                    ,needsCollections[key][NeedTraits.CurrentDirection].ToString()
                    ,","
                    ,needsCollections[key][NeedTraits.DecayDirection].ToString()
                    ," - "
                    ,needStates[key].ToString());
                needStates[key] += adjustNeed(key, needsCollections[key][NeedTraits.CurrentDirection] == needsCollections[key][NeedTraits.DecayDirection]);
            }
            frameCount = 0;
            EmitSignal("TaskExit", false); //Run the Task Evaluator
            //ChooseTask();
        }
    }

    //nvm can't help myself
    // IT WORKS!
    // right, except due to annoying float problems it still ends up slightly off.
    // I think I'll need to move everything to decimal or even just ints with larger ranges
    private float adjustNeed(NeedNames need, bool worsening)
    {
        if (worsening)
        {
            if (needsCollections[need][NeedTraits.OptimumValue] > needsCollections[need][NeedTraits.WorstValue]) // if worsening is downwards
            {
                if (needStates[need] > needsCollections[need][NeedTraits.WorstValue])
                {
                    return needsCollections[need][NeedTraits.DecayRate] * -1.0f; // Provide a negative number to worsen down.
                } else {return 0.0f;}
                
            } else // if worsening is upwards
            {
                if (needStates[need] < needsCollections[need][NeedTraits.WorstValue])
                {
                    return needsCollections[need][NeedTraits.DecayRate];
                } else {return 0.0f;}
            }
        } else
        {
            if (needsCollections[need][NeedTraits.OptimumValue] < needsCollections[need][NeedTraits.WorstValue]) // if improving is downwards
            {
                if (needStates[need] > needsCollections[need][NeedTraits.OptimumValue])
                {
                    return needsCollections[need][NeedTraits.DecayRate] * -1.0f; // Provide a negative number to improve down to.
                } else {return 0.0f;}
                
            } else // if improving is upwards
            {
                if (needStates[need] < needsCollections[need][NeedTraits.OptimumValue])
                {
                    return needsCollections[need][NeedTraits.DecayRate]; // Provide a negative number to worsen down.
                } else {return 0.0f;}
                
            }
        }
    }

}
