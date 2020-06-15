using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Pawn : Area2D
{
    [Export]
    private int needFrameSkip; // This is a magic number to dictate how many frames the loop should skip for doing the need calc
    private int frameCount;
    [Export]
    private int speed;

    [Export]
    private Vector2 velocity = new Vector2();

    [Export]
    private Vector2 currentDestination;

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
            this.currentPath = value;
            if (currentPath.Length > 1)
            {
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
        Bordom
    }

    //A tracker of the current targeted need/state?
    private NeedNames currentTargetNeed;

    private Dictionary<NeedNames, float> needStates;
    //  = new Dictionary<NeedNames, float>
    // {
    //     {NeedNames.Rest, 10.0f},
    //     {NeedNames.Bordom, 0.0f}
    // };

    // I'm thinking maybe I should make this a static reference
    // Store things like current decay direction within the states dictionary...
    private Dictionary<NeedNames, Dictionary<NeedTraits, float>> needsCollections;
    //  = new Dictionary<NeedNames, Dictionary<NeedTraits, float>>()
    // {
    //     {
    //         NeedNames.Rest,
    //         new Dictionary<NeedTraits, float>
    //         {
    //             {NeedTraits.MinimumValue, 0.0f},
    //             {NeedTraits.MaximumValue, 10.0f},
    //             {NeedTraits.DecayDirection, 1.0f},
    //             {NeedTraits.DecayRate, 0.1f}
    //         }
    //     },
    //     {
    //         NeedNames.Bordom,
    //         new Dictionary<NeedTraits, float>
    //         {
    //             {NeedTraits.MinimumValue, 0.0f},
    //             {NeedTraits.MaximumValue, 10.0f},
    //             {NeedTraits.DecayDirection, -1.0f},
    //             {NeedTraits.DecayRate, 0.05f}
    //         }
    //     },
    // };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        needFrameSkip = 5;
        frameCount = 0;

        // the target need
        currentTargetNeed = NeedNames.Bordom;

        needStates = new Dictionary<NeedNames, float>
        {
            {NeedNames.Rest, 9.0f},
            {NeedNames.Bordom, 1.0f}
        };
        needsCollections = new Dictionary<NeedNames, Dictionary<NeedTraits, float>>()
        {
            {
                NeedNames.Rest,
                new Dictionary<NeedTraits, float>
                {
                    {NeedTraits.OptimumValue, 10.0f}, // need a better name, 
                    {NeedTraits.WorstValue, 0.0f}, // Maybe depleted value?
                    {NeedTraits.DecayDirection, 1.0f}, // getting less rested/increasingly tired
                    {NeedTraits.DecayRate, 0.1f},
                    {NeedTraits.CurrentDirection, 1.0f} // Keep this sort of "fake" static
                }
            },
            {
                NeedNames.Bordom,
                new Dictionary<NeedTraits, float>
                {
                    {NeedTraits.OptimumValue, 0.0f},
                    {NeedTraits.WorstValue, 10.0f},
                    {NeedTraits.DecayDirection, -1.0f}, // getting increasingly bored
                    {NeedTraits.DecayRate, 0.2f},
                    {NeedTraits.CurrentDirection, -1.0f}
                }
            }
        };


        GD.Print("doing ready on " + this.Name);
        visAid = GetNode("visualAid") as Line2D;
        visAid.SetAsToplevel(true);
        if (GetParent() is PathfindingTilemapExperiment)
        {
            pathProvider = GetParent<PathfindingTilemapExperiment>();
        }
        else
        {
            GD.PrintErr("The parent was wrong!");
        }
        //hardcoded set for now until I provide some thing to do this.
        DelayedReadyWorkAround("ready");
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
        currentDestination = ChooseWanderDestination(pathProvider.CalcValidArea(new Rect2(this.Position + wanderOriginOffset, wanderRectSize))); //make sure you don't subtract a negative vector you silly billy
        CurrentPath = pathProvider.CalcPath(this.Position, currentDestination);
        if (CurrentPath[CurrentPath.Length - 1] != currentDestination)
        {
            currentDestination = CurrentPath[CurrentPath.Length - 1];
        }
        GD.Print(pathProvider.CalcPoint(currentDestination));
    }

    private NeedNames EvaluateStates(Dictionary<NeedNames, float> currentStates)
    {
        // I need to make all states comparable (fix the directional flips) and then evaluate them
        // Setup a random gen to allow for some deviation, to be decided on later.
        RandomNumberGenerator rand = new RandomNumberGenerator();
        rand.Randomize();

        // Find whichever need is further from it's optimum state and swap to it preferably.
        var targetNeed = currentTargetNeed; // Establish the current working need to give it advantage? Allow for some need stickyness
        foreach (NeedNames need in currentStates.Keys)
        {
            // I need to check distence from minimum (how close to being depleted it is) or "maximum" in the case of something like bordom
            // In short we're checking for distance from optimum value, or distance to worst value.
            // The further we are, the worse things are.
            if (need != targetNeed && 
                Math.Abs(needsCollections[need][NeedTraits.OptimumValue] - currentStates[need]) > Math.Abs(needsCollections[targetNeed][NeedTraits.OptimumValue] - currentStates[targetNeed])
                )
            {
                targetNeed = need;
            }
        }
        return targetNeed;
    }

    // There might be wisdom in changing this from void to a return type of state, to help keep track of things though unsure of how to do so currently.
    private void ChooseState() // I changed the name because the pawn might remain in a state upon evaluation
    {
        var chosenNeed = EvaluateStates(needStates);

        //Ideally I should use some sort of match function here but I'm too lazt atm
        if (chosenNeed == NeedNames.Rest)
        {
            GD.Print("choose rest");
            Rest();
        } else
        {
            GD.Print("choose walk");
            DelayedReadyWorkAround("ready");
        }
    }

    // This function/method should allow for dictating a rest state.
    private void Rest()
    {
        needsCollections[NeedNames.Rest][NeedTraits.CurrentDirection] = needsCollections[NeedNames.Rest][NeedTraits.DecayDirection] * -1.0f; // reverse the decay
        needsCollections[NeedNames.Bordom][NeedTraits.CurrentDirection] = needsCollections[NeedNames.Bordom][NeedTraits.DecayDirection]; // reverse the decay
        currentDestination = this.Position;
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
    }

    private Vector2 ChooseWanderDestination(Rect2 possibleArea)
    {
        needsCollections[NeedNames.Rest][NeedTraits.CurrentDirection] = needsCollections[NeedNames.Rest][NeedTraits.DecayDirection]; // Match state of decay
        needsCollections[NeedNames.Bordom][NeedTraits.CurrentDirection] = needsCollections[NeedNames.Bordom][NeedTraits.DecayDirection] * -1.0f; // Match state of decay
        RandomNumberGenerator rand = new RandomNumberGenerator();
        rand.Randomize();
        int[] walkableTiles = new int[] { 0 }; //hardcoded garbo for now, think more about it later
        Vector2 point;
        // point should now be somewhere within the possible area
        point = new Vector2(
            rand.RandfRange(possibleArea.Position.x, possibleArea.End.x)
            , rand.RandfRange(possibleArea.Position.y, possibleArea.End.y)
            );
        // point = new Vector2(
        //     (float)GD.RandRange(possibleArea.Position.x, possibleArea.End.x)
        //     ,(float)GD.RandRange(possibleArea.Position.y, possibleArea.End.y)
        //     );
        //currentDestination = pathProvider.GetValidTile(point, walkableTiles);
        return pathProvider.GetValidTile(point, walkableTiles);
    }
    private void WalkPath()
    {
        // GD.Print("point 0 before update", visAid.GetPointPosition(0).ToString());
        visAid.SetPointPosition(0, this.GlobalPosition);
        // GD.Print("point 0 after update", visAid.GetPointPosition(0).ToString());
        if (this.Position.DistanceTo(CurrentPath[currentPoint]) < 1)
        {
            if (this.Position.DistanceTo(currentDestination) > 1)
            {
                if (currentPoint < CurrentPath.Length - 1)
                {
                    currentPoint++;
                }
                visAid.RemovePoint(1);
            }
            else
            {
                //hardcode terrible loop thingy
                // currentDestination = ChooseWanderDestination(pathProvider.CalcValidArea(new Rect2(this.Position + wanderOriginOffset, wanderRectSize)));
                // CurrentPath = pathProvider.CalcPath(this.Position, currentDestination);
                //CurrentPath = new Vector2[0];
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (currentTargetNeed == NeedNames.Bordom && CurrentPath.Length > 1 && needStates[NeedNames.Rest] > 0)
        {
            //GD.Print("current point is: ", currentPoint.ToString());
            // GD.Print(currentPoint.ToString());
            velocity = this.Position.DirectionTo(currentPath[currentPoint]);

            //GD.Print(velocity);item
            this.Position += (velocity * delta * speed);
            WalkPath();
        }
        // int currentPoint 

        // if (currentPath[currentPath.Length-1] == destination)
        // if (currentDestination != this.Position)
        // {
        //     currentPath = pathProvider.CalcPath(this.Position, currentDestination);

        // }
        // if ((this.Position.DistanceSquaredTo(currentPath[0]) < 1))
        // {
        //     GD.Print("bah");
        // }
    }

    public override void _PhysicsProcess(float delta)
    {
        frameCount++;
        if (frameCount == needFrameSkip)
        {
            List<NeedNames> needStatesKeys = new List<NeedNames>(needStates.Keys);
            foreach (var key in needStatesKeys)
            {
                if  (needStates[key] > Math.Min(needsCollections[key][NeedTraits.OptimumValue],needsCollections[key][NeedTraits.WorstValue]) 
                        && needStates[key] < Math.Max(needsCollections[key][NeedTraits.OptimumValue],needsCollections[key][NeedTraits.WorstValue])
                    ) // Make sure the need is within the range
                {
                    // A need should decay via minus, as most needs are depleted during decay.
                    // So when I wish to decay I should use positive numbers and when I wish to restore I should use positive numbers.
                    needStates[key] -= (needsCollections[key][NeedTraits.DecayRate] * needsCollections[key][NeedTraits.CurrentDirection]); //adjust the need
                }
                GD.Print(key.ToString(),": ", needStates[key].ToString());
            }
            // for (int i = 0; i < needStates.Count; i++)
            // {
            //     needStates.Keys
            //     needStates[] += -0.1f;
            // }
            // foreach (var needKey in needStates.Keys)
            // {
            //     if (needStates[needKey] > needsCollections[needKey][NeedTraits.MinimumValue] && needStates[needKey] <= needsCollections[needKey][NeedTraits.MaximumValue]) // Make sure the need is within the range
            //     {
            //         needStates[needKey] += (needsCollections[needKey][NeedTraits.DecayRate] * needsCollections[needKey][NeedTraits.DecayDirection]); //adjust the need
            //     }
            //     GD.Print(needKey.ToString(), needStates[needKey].ToString());
            // }
            frameCount = 0;
            ChooseState();
        }
    }

}
