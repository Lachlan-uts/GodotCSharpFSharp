using Godot;
using System;

public class BasicTileset : TileSet
{
    [Export]
    public string Name { get; set; }

    public BasicTileset(string name = "not given")
    {
        Name = name;
    }
}
