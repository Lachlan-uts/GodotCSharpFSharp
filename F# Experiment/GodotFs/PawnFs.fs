namespace GodotFs
 
open Godot

type PawnFs() as this =
    inherit Area2D()

    [<Export>]
    let speed = 200
 
    override this._Ready() = 
        GD.Print("Hello from F#!")
        GD.Print("I think this works")