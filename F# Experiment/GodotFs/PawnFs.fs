namespace GodotFs

module FsSignalFunctions =
    open Godot

    let fsSignalFun (words: string) = GD.Print(words)

open Godot
//open FsSignals

// I need to build a few things
(*
1. I need the need system, which has multiple sub parts
2. I need the movement system
3. I need the task system

Of these the movement seems hopefully the easiest so I think I'll start there.
*)




type PawnFs() as self =
    inherit Area2D()
    
    [<Export>]
    let speed = 200

    let lineVisualer =
        lazy(
            self.GetNode(new NodePath("VisualAid"))
            :?> Line2D
            )
    

    let fsFun = GD.Print("caught the emit")
    //static member fsFun = GD.Print("member attempt")

    let methodPrinter (x: string []) = x |> Array.map (fun i -> GD.Print(i))

    override this._Ready() =
        let methodArray = self.GetMethodList()
        let methodArray2 = self.GetMethodList
        let completeMethod = methodArray2()
        let array = [|0;1;2|]
        //let test = methodPrinter methodArray
        GD.Print(methodArray)
        //Collections.Array[0]
        //let methodList = Collections.List.ofArray methodArray
        //let mapAttempt = methodList |> Array.map (fun i -> GD.Print(i))

        GD.Print("Hello from F#!")
        GD.Print("I think this works")
        let cRes = self.Connect("MySignal", self, "testFun")
        let fRes = self.Connect("MySignal", self, "fsFun")
        GD.Print("About to emit")
        self.EmitSignal("MySignal")
        