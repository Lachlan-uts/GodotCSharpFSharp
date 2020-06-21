using Godot;
using System;
using System.Collections.Generic;

public class Plant : Area2D
{
    private TileMap world; // the tilemap which should be the parent.

    private Dictionary<Vector2,int> adjacentTiles;
    public int id;
    public Vector2 tileMapPos;
    private float growth = 0.0f;
    [Export]
    private int growthRate = 1;
    public override void _Ready()
    {
        base._Ready();
        AddToGroup("Trees");

        if (GetParent() is TileMap)
        {
            world = GetParent<TileMap>();
        } else { GD.Print("Not child to tilemap"); }
        
        //assign the map values
        tileMapPos = world.WorldToMap(this.Position);
        id = world.GetCellv(tileMapPos);
        adjacentTiles = world.getAdjacentTiles(tileMapPos); // Get all the adjacent tiles
    }

    private Vector2 GetGrowTarget(Vector2 centerPoint)
    {
        foreach (Vector2 pos in adjacentTiles.Keys)
        {
            if (adjacentTiles[pos] == 0) {return pos;}
        }
        return centerPoint;
    }

    public bool AttemptGrowth()
    {
        Vector2 target = GetGrowTarget(tileMapPos);
        if (target != tileMapPos)
        {
            // Change tile stuff
            
            adjacentTiles[target] = 2;
            if (world.GetCellv(target) != 2)
            {
                world.SetCellv(target,2);
            }
            return true;
        }
        return false;
    }

 // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (growth > 100 ) { growth = 0; } //just make the growth cycle forever atm
        growth += (growthRate * delta);
        
        if (growth > 50 && this.adjacentTiles.ContainsValue(0)) //the only currently valid option
        {
            var result = this.AttemptGrowth();
            if (result) { growth = 0; } //shrink on growth for some strange reason.
        }
    }
}
