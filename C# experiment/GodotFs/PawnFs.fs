namespace GodotFs

open Godot
open PathProvider

type DecayDirection = Up | Down

//going to start drafting out the need type stuff
type Need =
    {
        CurrentValue: float;
        Max: float;
        Min: float;
        Decaying: bool;
        DecayDirection: DecayDirection;
        ChangeRate: float;

    }

type PawnFs() as self =
    inherit Area2D()

    let rest =
        { CurrentValue = 6.0;
        Max = 10.0;
        Min = 0.0;
        Decaying = true;
        DecayDirection = Down;
        ChangeRate = 0.1;
        }

    //let fsFun = GD.Print("called a FS made function")
    

    let pathProviderCS =
        lazy(
            self.GetParent().GetNode(new NodePath("Navigation2D"))
            :?> Navigation2D
    )
    

    let doChange decDir state changeRate =
        match (decDir, state) with
        | (Up,true) | (Down,false) ->
            (+) changeRate
        | _ ->
            (-) changeRate

    let allowValueChange (need : Need) =
        match (need.DecayDirection, need.Decaying) with
        | (Up,true) | (Down,false) ->
            doChange need.DecayDirection need.Decaying need.ChangeRate need.CurrentValue <= need.Max
        | _ ->
            doChange need.DecayDirection need.Decaying need.ChangeRate need.CurrentValue >= need.Min

    //let getTarget need dir =
        //match dir with
        //| Up -> need.WorstValue

    let updateNeed (need: Need) =
        match (need.DecayDirection, need.Decaying) with
        | (Up,true) | (Down,false) ->
            let newValue = need.CurrentValue + need.ChangeRate
            if newValue > need.Max then
                {need with CurrentValue = need.Max}
            else  
                {need with CurrentValue = newValue}
        | _ ->
            let newValue = need.CurrentValue - need.ChangeRate
            if newValue < need.Min then
                {need with CurrentValue = need.Max}
            else  
                {need with CurrentValue = newValue}

    //let update msg (need: Need) =
        //let { currentValue = cv } = need
        //match need with
        //| {DecayDirection=Down} when cv > {WorstValue} ->
        //    let newCv =
        //        cv + (need.DecayRate)
        //| {DecayDirection=Up} when cv < {WorstValue} ->
        //| cv when need. 
        //| Equip item ->
        //    let allowEquip =
        //        getWeight p.inventory + (snd item) <= p.maxWeight

        //    if allowEquip then
        //        { model with
        //              player =
        //                  { p with
        //                        inventory = item :: p.inventory } }
        //    else
        //        need
        //| _ -> need

    //let rand = new RandomNumberGenerator()

    //let fsTest words = self.FsSignalFun(words)

    //let fTesting = fsTest "hello"

    //abstract member FsSignalFun: string -> unit
    //default this.FsSignalFun words = GD.Print(words)

    //default this.UpdateTest =
        //let blah = updateNeed rest
        //GD.Print(rest.CurrentValue.ToString())



    abstract member ChooseWanderDest: Rect2 -> int[] -> Vector2
    default this.ChooseWanderDest (pArea : Rect2) (validTiles : int[]) =
        GD.Print("doing it")
        PathProvider.getClosestValidTile pathProviderCS (PathProvider.getRandomPointFromArea pArea validTiles)