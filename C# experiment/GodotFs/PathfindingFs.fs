module PathProvider
open Godot

let randy = new RandomNumberGenerator()
randy.Randomize()

let getClosestValidTile (nav2D : Lazy<Navigation2D>) point =
    nav2D.Value.GetClosestPoint(point)


let getRandomPointFromArea (possibleArea : Rect2) (validTiles : int[]) =
    randy.Randomize()
    new Vector2(
        randy.RandfRange(possibleArea.Position.x, possibleArea.End.x),
        randy.RandfRange(possibleArea.Position.y, possibleArea.End.y)
    )

