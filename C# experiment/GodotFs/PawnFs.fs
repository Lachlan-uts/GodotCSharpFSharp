namespace GodotFs

open Godot
open PathProvider

type PawnFs() as self =
    inherit Area2D()

    //let fsFun = GD.Print("called a FS made function")
    

    let pathProviderCS =
        lazy(
            self.GetParent().GetNode(new NodePath("Navigation2D"))
            :?> Navigation2D
            )

    //let rand = new RandomNumberGenerator()

    //let fsTest words = self.FsSignalFun(words)

    //let fTesting = fsTest "hello"

    //abstract member FsSignalFun: string -> unit
    //default this.FsSignalFun words = GD.Print(words)

    abstract member ChooseWanderDest: Rect2 -> int[] -> Vector2
    default this.ChooseWanderDest (pArea : Rect2) (validTiles : int[]) =
        GD.Print("doing it")
        PathProvider.getClosestValidTile pathProviderCS (PathProvider.getRandomPointFromArea pArea validTiles)

