using Godot;
using System;
using System.Threading.Tasks;

public class Pawn : Area2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
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
            } else
            {
                currentPoint = 0;
                visAid.ClearPoints();
            }
        }
    }
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("doing ready on " + this.Name);
        visAid = GetNode("visualAid") as Line2D;
        visAid.SetAsToplevel(true);
        if (GetParent() is PathfindingTilemapExperiment)
        {
            pathProvider = GetParent<PathfindingTilemapExperiment>();
        } else
        {
            GD.PrintErr("The parent was wrong!");
        }
        //hardcoded set for now until I provide some thing to do this.
        WorkAround();
        //destination = this.Position;
    }

    private async Task WorkAround()
    {
        await ToSignal(pathProvider, "ready");
        CurrentPath = pathProvider.CalcPath(this.Position, currentDestination);
        if (CurrentPath[CurrentPath.Length-1] != currentDestination)
        {
            currentDestination = CurrentPath[CurrentPath.Length-1];
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

    private void WalkPath()
    {
        // GD.Print("point 0 before update", visAid.GetPointPosition(0).ToString());
        visAid.SetPointPosition(0,this.GlobalPosition);
        // GD.Print("point 0 after update", visAid.GetPointPosition(0).ToString());
        if (this.Position.DistanceTo(CurrentPath[currentPoint]) < 1)
        {
            if (this.Position.DistanceTo(currentDestination) > 1)
            {
                if (currentPoint < CurrentPath.Length-1)
                {
                    currentPoint++;
                }
                visAid.RemovePoint(1);
            } else
            {
                CurrentPath = new Vector2[0];
            }
        }
    }

 // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (CurrentPath.Length > 1)
        {
            //GD.Print("current point is: ", currentPoint.ToString());
            GD.Print(currentPoint.ToString());
            velocity = this.Position.DirectionTo(currentPath[currentPoint]);

            //GD.Print(velocity);
            //GD.Print(currentPath[currentPoint]);
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

}
