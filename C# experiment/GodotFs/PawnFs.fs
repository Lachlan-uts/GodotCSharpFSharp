namespace GodotFs

open Godot
open PathProvider
open GodotNodeUtils
open NeedSystem

// Porting enums from C# to F# to enable F# calling interopt
type NeedNames = Rest = 0 | Boredom = 1
type NeedTraits = OptimumValue = 0 | WorstValue = 1 | DecayDirection = 2 | DecayRate = 3 | CurrentDirection = 4

type PawnFs() as self =
    inherit Area2D()

    let startingNeeds = NeedSystem.defaultMapOfNeeds

    let mutable mutNeedMap = startingNeeds

    let pathProviderNode = self.getNode "../Navigation2D"

    let pathProviderCS =
        lazy(
            self.GetParent().GetNode(new NodePath("Navigation2D"))
            :?> Navigation2D
    )

    //let testVec = new Vector2(0.0f, 0.0f)

    //let wot = pathProviderNode.Value.saySomething

    // Currently only being called once every 60 frames or once per second.
    override this._PhysicsProcess(delta) =
        mutNeedMap <- updateNeedMap mutNeedMap
        mutNeedMap <- mapBounce mutNeedMap
        Map.iter (fun (name : NeedName) (need : NeedSystem.Need ) -> GD.Print(need.CurrentValue.ToString())) mutNeedMap

    // Abstract (C# callable) Methods/funct ions

    //abstract member UpdateNeedTest: string -> unit
    //default this.UpdateNeedTest _ =
        ////mutRest <- updateNeed mutRest
        //GD.Print(mutRest.CurrentValue.ToString())

    //abstract memeber ChangeTaskAdjustNeeds: Need
        
    abstract member ChooseWanderDest: Rect2 -> int[] -> Vector2
    default this.ChooseWanderDest (pArea : Rect2) (validTiles : int[]) =
        PathProvider.getClosestValidTile pathProviderNode (PathProvider.getRandomPointFromArea pArea validTiles)