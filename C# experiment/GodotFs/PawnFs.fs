namespace GodotFs

open Godot
open PathProvider
open GodotNodeUtils

// Porting enums from C# to F# to enable F# calling interopt
type NeedNames = Rest = 0 | Boredom = 1
type NeedTraits = OptimumValue = 0 | WorstValue = 1 | DecayDirection = 2 | DecayRate = 3 | CurrentDirection = 4

// Building the custom types for a purely F# need system
type DecayDirection = Up | Down
type Need =
    {
        CurrentValue: float;
        Maxi: float;
        Mini: float;
        Decaying: bool;
        DecayDirection: DecayDirection;
        ChangeRate: float;

    }

type PawnFs() as self =
    inherit Area2D()

    let rest =
        {
        CurrentValue = 6.0;
        Maxi = 10.0;
        Mini = 0.0;
        Decaying = true;
        DecayDirection = Down;
        ChangeRate = 0.1;
        }
    let mutable mutRest = rest

    let pathProviderNode = self.getNode "../Navigation2D"

    let pathProviderCS =
        lazy(
            self.GetParent().GetNode(new NodePath("Navigation2D"))
            :?> Navigation2D
    )

    //let testVec = new Vector2(0.0f, 0.0f)

    //let wot = pathProviderNode.Value.saySomething

    let changeNeedDir state (need: Need) =
        match state with
        | x when need.Decaying = x -> need
        | _ -> {need with Decaying = state}

    let changeNeedMag rate (need: Need) =
        {need with ChangeRate = rate}

    let changeNeedDirAndMag state rate (need: Need) =
        changeNeedDir state need |> changeNeedMag rate

    let updateNeed (need: Need) =
        match (need.DecayDirection, need.Decaying) with
        | (Up,true) | (Down,false) ->
            let newValue = need.CurrentValue + need.ChangeRate
            if newValue > need.Maxi then
                {need with CurrentValue = need.Maxi}
            else  
                {need with CurrentValue = newValue}
        | _ ->
            let newValue = need.CurrentValue - need.ChangeRate
            if newValue < need.Mini then
                {need with CurrentValue = need.Mini}
            else  
                {need with CurrentValue = newValue}


    let bounce (need: Need) =
        match (need.CurrentValue, need.DecayDirection, need.Decaying) with
        | (x, Up, true) | (x, Down, false) when x = need.Maxi ->
            changeNeedDir (not need.Decaying) need
        | (x, Up, false) | (x, Down, true) when x = need.Mini ->
            changeNeedDir (not need.Decaying) need
        | _ -> need


    // Currently only being called once every 60 frames or once per second.
    override this._PhysicsProcess(delta) =
        mutRest <- updateNeed mutRest
        mutRest <- bounce mutRest
        GD.Print(mutRest.CurrentValue.ToString())


    // Abstract (C# callable) Methods/functions

    //abstract member UpdateNeedTest: string -> unit
    //default this.UpdateNeedTest _ =
        ////mutRest <- updateNeed mutRest
        //GD.Print(mutRest.CurrentValue.ToString())

    //abstract memeber ChangeTaskAdjustNeeds: Need
        
    abstract member ChooseWanderDest: Rect2 -> int[] -> Vector2
    default this.ChooseWanderDest (pArea : Rect2) (validTiles : int[]) =
        PathProvider.getClosestValidTile pathProviderNode (PathProvider.getRandomPointFromArea pArea validTiles)