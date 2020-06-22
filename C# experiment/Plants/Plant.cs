using Godot;
using System;
using System.Collections.Generic;

// Found this idea online to shuffle the growth pattern, should make it look slightly more natural.
static class MyExtensions
{
    private static Random rng = new Random(); 
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

interface IGrowable
{
    void FooBar();
}

interface IReactiveTile
{
    void CheckIfNeighbourChanged();
}
interface IBasicTile
{
    int ID { get; set; }
}
public class Plant : Area2D, IBasicTile
{
    private TileMap world; // the tilemap which should be the parent.

    private Dictionary<Vector2, int> adjacentTiles;
    //public int id;
    public int ID { get; set; }
    public Vector2 tileMapPos;
    private float growth = 0.0f;
    [Export]
    private int growthRate = 1;
    public override void _Ready()
    {
        //GD.Print(this.GetType());
        base._Ready();
        AddToGroup("Trees");

        if (GetParent() is TileMap)
        {
            world = GetParent<TileMap>();
        }
        else { GD.Print("Not child to tilemap"); }

        //assign the map values
        tileMapPos = world.WorldToMap(this.Position);
        ID = world.GetCellv(tileMapPos);
        adjacentTiles = world.getAdjacentTiles(tileMapPos); // Get all the adjacent tiles
    }

    public void UpdateAdjacentTile(Vector2 pos, int id)
    {
        adjacentTiles[pos] = id;
    }

    private Vector2 GetGrowTarget(Vector2 centerPoint)
    {
        List<Vector2> adjTileKeys = new List<Vector2>(adjacentTiles.Keys);
        adjTileKeys.Shuffle();
        foreach (Vector2 pos in adjTileKeys)
        {
            if (adjacentTiles[pos] == 0) { return pos; }
        }
        return centerPoint;
    }

    public bool AttemptGrowth()
    {
        Vector2 target = GetGrowTarget(tileMapPos);
        if (target != tileMapPos)
        {
            // Change tile stuff

            adjacentTiles[target] = ID;
            if (world.GetCellv(target) != ID)
            {
                world.SetCellv(target, ID);
            }
            return true;
        }
        return false;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (growth > 100 || !this.adjacentTiles.ContainsValue(0)) { growth = 0; } //just make the growth cycle forever atm
        // only grow while you have somewhere to grow to, I know it's dumb, maybe rename to spread?
        if (this.adjacentTiles.ContainsValue(0)) { growth += (growthRate * delta); }
        if (growth > 50 && this.adjacentTiles.ContainsValue(0)) //the only currently valid option
        {
            var result = this.AttemptGrowth();
            if (result) { growth = 0; } //shrink on growth for some strange reason.
        }
    }
}
