namespace GodotFs
 
open Godot

type PawnFs() =
    inherit Node()
 
    override this._Ready() = 
        GD.Print("Hello from F#!")
        GD.Print("I think this works")