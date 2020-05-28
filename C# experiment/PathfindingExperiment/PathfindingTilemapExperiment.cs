using Godot;
using System;

public class PathfindingTilemapExperiment : Node
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    private Navigation2D pathmaker;
    private TileMap floor;
    private Line2D visAid;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        visAid = GetNode("visualAid") as Line2D;

        floor = GetNode("Navigation2D/TileMap") as TileMap;

        pathmaker = GetNode("Navigation2D") as Navigation2D;
        Vector2 startPoint = pathmaker.GetClosestPoint(new Vector2(25,25));
        Vector2 endPoint = pathmaker.GetClosestPoint(new Vector2(350,25));

        Vector2[] demoPath = pathmaker.GetSimplePath(startPoint,endPoint, true);
        visAid.Points = pathmaker.GetSimplePath(startPoint,endPoint, true);
        GD.Print(demoPath.ToString());
        foreach (var point in demoPath)
        {
            GD.Print("raw points: " + point);
            GD.Print("tilepoints: " + floor.WorldToMap(point));
        }
    }

    public Vector2[] CalcPath(Vector2 start, Vector2 end)
    {
        bool optiChoice = true;
        visAid.Points = pathmaker.GetSimplePath(start,end, optiChoice);
        return visAid.Points;
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
