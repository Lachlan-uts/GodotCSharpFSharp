using Godot;
using System;
using System.Collections.Generic;

public class PathfindingTilemapExperiment : Node
{
    private Navigation2D pathmaker;
    private TileMap floor;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("doing ready on " + this.Name);
        floor = GetNode("Navigation2D/TileMap") as TileMap;

        pathmaker = GetNode("Navigation2D") as Navigation2D;
        Vector2 startPoint = pathmaker.GetClosestPoint(new Vector2(25,25));
        Vector2 endPoint = pathmaker.GetClosestPoint(new Vector2(350,25));

        //var difDemoPath = pathmaker.GetSimplePath(startPoint,endPoint, true) as Godot.Collections.Array<Vector2>;
        Vector2[] demoPath = pathmaker.GetSimplePath(startPoint,endPoint, true);
        GD.Print(demoPath.ToString());
        foreach (var point in demoPath)
        {
            GD.Print("raw points: " + point);
            GD.Print("tilepoints: " + floor.WorldToMap(point));
        }
        base._Ready();
    }

    public Vector2[] CalcPath(Vector2 start, Vector2 end)
    {
        if (end != pathmaker.GetClosestPoint(end))
        {
            end.x = end.x + 0.01f;
        }
        bool optiChoice = true;
        return pathmaker.GetSimplePath(start,end, optiChoice);
    }

    public Vector2 CalcPoint(Vector2 point)
    {
        return pathmaker.GetClosestPoint(point);
    }

    public Rect2 CalcValidArea(Rect2 rect)
    {
        //cell size is in 16 atm
        int cellSize = 16;
        //convert tilespace to worldspace
        Rect2 worldTileSpace = new Rect2(floor.GetUsedRect().Position*cellSize,floor.GetUsedRect().Size*cellSize);
        return worldTileSpace.Clip(rect);
    }

    public Vector2 GetValidTile(Vector2 targetPoint, int[] validTiles)
    {
        return pathmaker.GetClosestPoint(targetPoint);
    }

    //If I make this also return a path then I'd need another walk path method potentially...
    public Vector2 GetClosestTargetTile(Vector2 start, int targetTileID, int maxRange)
    {
        var possiblePoints = GetTargetTilesInArea(floor.WorldToMap(start),targetTileID,maxRange);
        // possiblePoints.Sort();
        Vector2 targetTile = possiblePoints[0];
        foreach (Vector2 tile in possiblePoints)
        {
            if ((tile-floor.WorldToMap(start)).LengthSquared() < targetTile.LengthSquared())
            {
                targetTile = tile;
            }
        }
        return floor.MapToWorld(targetTile);
        
    }

    public List<Vector2> GetTargetTilesInArea(Vector2 centerPoint, int targetTileID, int maxRange)
    {
        var listOfTiles = floor.GetUsedCellsById(targetTileID); //This returns an array of vector 2's
        
        List<Vector2> validTiles = new List<Vector2>();

        // Godot.Collections.Array<Vector2> validTiles = new Godot.Collections.Array<Vector2>();
        foreach (Vector2 point in floor.GetUsedCellsById(targetTileID))
        {
            if (centerPoint.DistanceTo(point) < (float)maxRange)
            {
                validTiles.Add(point);
            }
        }

        return validTiles;
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
