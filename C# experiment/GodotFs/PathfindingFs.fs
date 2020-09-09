module PathProvider
open Godot

let randy = 
    lazy(
        new RandomNumberGenerator()
        :?> RandomNumberGenerator
    )
    
let getClosestValidTile (nav2D : Lazy<Navigation2D>) point =
    nav2D.Value.GetClosestPoint(point)


let getRandomPointFromArea (possibleArea : Rect2) (validTiles : int[]) =
    randy.Value.Randomize()
    //GD.Print("what the random seed is: ")
    //GD.Print(randy.Value.Seed.ToString())
    new Vector2(
        randy.Value.RandfRange(possibleArea.Position.x, possibleArea.End.x),
        randy.Value.RandfRange(possibleArea.Position.y, possibleArea.End.y)
    )

