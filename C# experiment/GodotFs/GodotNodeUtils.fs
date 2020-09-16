module GodotNodeUtils

open Godot

//Credit for this goes to Lars Kokemohr whose tutorial helped a lot
type Node with
    /// <summary>
    /// enables nodes to be accessed far nicer
    /// </summary>
    member this.getNode<'a when 'a :> Node> (path : string) =
        lazy((this.GetNode(new NodePath(path))) :?> 'a)



type Navigation2D with
    /// <summary>
    /// words
    /// </summary>
    member this.saySomething<'a when 'a :> Navigation2D> (words : string) =
        this.GetParent() :?> 'a