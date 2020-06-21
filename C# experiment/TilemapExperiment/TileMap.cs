using Godot;
using System;
using System.Collections.Generic;

public class TileMap : Godot.TileMap
{
    [Export]
    public Godot.Collections.Dictionary<int,string> nodeTileScenePaths;
    private Dictionary<int,PackedScene> loadedTiles = new Dictionary<int, PackedScene>(); //kept as an abstract node for now
    public override void _Ready()
    {
        //turn the strings into packed scenes
        foreach (int nodeTileID in nodeTileScenePaths.Keys)
        {
            loadedTiles.Add(nodeTileID,GD.Load<PackedScene>(nodeTileScenePaths[nodeTileID]));
        }
        
        //instantiate them where needed
        foreach (int nodeTileID in loadedTiles.Keys)
        {
            var usedCells = this.GetUsedCellsById(nodeTileID);
            foreach (Vector2 tileCoords in usedCells)
            {
                //probably do some check to ensure nothing is there but meh for now.
                this.SetCellv(tileCoords,nodeTileID);
                // var tileNode = loadedTiles[nodeTileID].Instance() as Area2D;
                // tileNode.Position = this.MapToWorld(tileCoords);
            }
        }
    }

    public new void SetCellv(Vector2 position, int tileID, bool flipX=false, bool flipY=false, bool transpose=false)
    {
        // add a terrible check to save problems for now, which probably is redundent but hey
        // if this tile is already there, then don't try to set it again.
        //if (this.GetCellv(position) == tileID) { return; } 
        base.SetCellv(position,tileID,flipX,flipY,transpose);
        if (loadedTiles.ContainsKey(tileID))
        {
            
            var tileNode = loadedTiles[tileID].Instance() as Area2D;
            tileNode.Position = this.MapToWorld(position);
            tileNode.Name = position.ToString();
            AddChild(tileNode);
        }
    }

    // A corrected map to world that returns the centerpoint, if it is set as the Point
    public new Vector2 MapToWorld(Vector2 mapPosition, bool ignoreHalfOfs=false)
    {
        switch (this.CellTileOrigin)
        {
            case TileOrigin.Center:
                return (base.MapToWorld(mapPosition,ignoreHalfOfs) + this.CellSize/2);
            case TileOrigin.BottomLeft:
                return (base.MapToWorld(mapPosition,ignoreHalfOfs) + new Vector2(0,this.CellSize.y)); //need to add just the y component
            default:
                return base.MapToWorld(mapPosition,ignoreHalfOfs);
        }
    }

    // Get them tiles
    // To make the whichNeighbour optional I need to make it null because C# is dumb
    public Dictionary<Vector2,int> getAdjacentTiles(Vector2 centerTile, Vector2[] neighbours=null) 
    {
        // using ??, a Null coalescing operator to skip an if statement
        neighbours = neighbours ?? new Vector2[] { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };
        Dictionary<Vector2,int> returnerDictionary = new Dictionary<Vector2, int>();
        foreach (Vector2 direction in neighbours)
        {
            returnerDictionary.Add(centerTile+direction, this.GetCellv(centerTile+direction));
        }
        return returnerDictionary;
    }
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
