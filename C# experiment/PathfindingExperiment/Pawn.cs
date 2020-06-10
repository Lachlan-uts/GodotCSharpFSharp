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
        MinimumValue,
        MaximumValue,
        //StepPoints, // Won't work as I want it to be an array or list
        DecayDirection,
        DecayRate
    }
    // The names of the various needs
    enum NeedNames
    {
        Rest,
        Bordom
    }

    private Dictionary<NeedNames, float> needStates;
    //  = new Dictionary<NeedNames, float>
    // {
    //     {NeedNames.Rest, 10.0f},
    //     {NeedNames.Bordom, 0.0f}
    // };
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
        needStates = new Dictionary<NeedNames, float>
        {
            {NeedNames.Rest, 10.0f},
            {NeedNames.Bordom, 0.0f}
        };

        needsCollections = new Dictionary<NeedNames, Dictionary<NeedTraits, float>>()
        {
            {
                NeedNames.Rest,
                new Dictionary<NeedTraits, float>
                {
                    {NeedTraits.MinimumValue, 0.0f},
                    {NeedTraits.MaximumValue, 10.0f},
                    {NeedTraits.DecayDirection, 1.0f},
                    {NeedTraits.DecayRate, 0.1f}
                }
            },
            {
                NeedNames.Bordom,
                new Dictionary<NeedTraits, float>
                {
                    {NeedTraits.MinimumValue, 0.0f},
                    {NeedTraits.MaximumValue, 10.0f},
                    {NeedTraits.DecayDirection, -1.0f},
                    {NeedTraits.DecayRate, 0.05f}
                }
        },
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
        DelayedReadyWorkAround();
        //destination = this.Position;
    }

    private async Task DelayedReadyWorkAround()
    {
        await ToSignal(pathProvider, "ready");
        WanderAround(pathProvider.CalcValidArea(new Rect2(this.Position + wanderOriginOffset, wanderRectSize))); //make sure you don't subtract a negative vector you silly billy
        CurrentPath = pathProvider.CalcPath(this.Position, currentDestination);
        if (CurrentPath[CurrentPath.Length - 1] != currentDestination)
        {
            currentDestination = CurrentPath[CurrentPath.Length - 1];
        }
        GD.Print(pathProvider.CalcPoint(currentDestination));
    }

    private void PathToDestination(Vector2 target)
    {
        if (currentDestination != target && currentDestination != this.Position)
        {
            CurrentPath = pathProvider.CalcPath(this.Position, currentDestination);
        }
    }

    private void WanderAround(Rect2 possibleArea)
    {
        needsCollections[NeedNames.Rest][NeedTraits.DecayRate] = 0.1f; //set the various need rates
        needsCollections[NeedNames.Bordom][NeedTraits.DecayDirection] = 1.0f; //set the various need rates
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
        currentDestination = pathProvider.GetValidTile(point, walkableTiles);
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
                WanderAround(pathProvider.CalcValidArea(new Rect2(this.Position + wanderOriginOffset, wanderRectSize)));
                CurrentPath = pathProvider.CalcPath(this.Position, currentDestination);
                //CurrentPath = new Vector2[0];
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (CurrentPath.Length > 1 && needStates[NeedNames.Rest] > 0)
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
                if (needStates[key] > needsCollections[key][NeedTraits.MinimumValue] && needStates[key] <= needsCollections[key][NeedTraits.MaximumValue]) // Make sure the need is within the range
                {
                    needStates[key] -= (needsCollections[key][NeedTraits.DecayRate] * needsCollections[key][NeedTraits.DecayDirection]); //adjust the need
                }
                GD.Print(key.ToString(), needStates[key].ToString());
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
        }
    }

}
