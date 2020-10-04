module PathProvider
open Godot

let randy = 
    lazy(
        new RandomNumberGenerator()
        :?> RandomNumberGenerator
        )


let getClosestValidTile (nav2D : Lazy<Navigation2D>) point =
    nav2D.Value.GetClosestPoint(point)

/// <summary>
/// gets a random point in area
/// yoo
/// </summary>
let getRandomPointFromArea (possibleArea : Rect2) (validTiles : int[]) =
    randy.Value.Randomize()
    //GD.Print("what the random seed is: ")
    //GD.Print(randy.Value.Seed.ToString())
    new Vector2(
        randy.Value.RandfRange(possibleArea.Position.x, possibleArea.End.x),
        randy.Value.RandfRange(possibleArea.Position.y, possibleArea.End.y)
    )

let private fixDestination (nav2D : Lazy<Navigation2D>) targetVec =
    //if getClosestValidTile nav2D targetVec - targetVec
    if targetVec <> (getClosestValidTile nav2D targetVec) then
        new Vector2(targetVec.x+0.01f,targetVec.y)
    else
        targetVec

let calcPath (nav2D : Lazy<Navigation2D>) start destination =
    let dest = fixDestination nav2D destination
    nav2D.Value.GetSimplePath(start, destination, true)