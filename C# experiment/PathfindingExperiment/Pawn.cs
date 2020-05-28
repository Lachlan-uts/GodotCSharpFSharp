using Godot;
using System;

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
    private Vector2 destination;

    private PathfindingTilemapExperiment pathProvider;
    private Vector2[] currentPath;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (GetParent() is PathfindingTilemapExperiment)
        {
            pathProvider = GetParent<PathfindingTilemapExperiment>();
        } else
        {
            GD.PrintErr("The parent was wrong!");
        }
        //destination = this.Position;
    }



 // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        // int currentPoint

        // if (currentPath[currentPath.Length-1] == destination)
        if (destination != this.Position)
        {
            currentPath = pathProvider.CalcPath(this.Position, destination);
            
        }
        if ((this.Position.DistanceSquaredTo(currentPath[0]) < 1))
        {
            GD.Print("bah");
        }
        velocity = this.Position.DirectionTo(currentPath[1]);

        GD.Print(velocity);
        GD.Print(currentPath[0]);
        this.Position += (velocity * delta * speed);
    }

}
