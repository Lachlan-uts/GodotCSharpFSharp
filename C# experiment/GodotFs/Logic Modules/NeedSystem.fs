module NeedSystem

type DecayDirection = Up | Down

type NeedName = Rest | Boredom

type Need =
    {
        //NeedName: NeedName;
        CurrentValue: float;
        Maxi: float; // also the optimum value
        Mini: float; // also the worst value
        Gaining: bool;
        DecayDirection: DecayDirection; // Only a hint for the visual system
        ChangeRate: float;
    }
// update method
// adjust methods
// evaluate method

let changeNeedDir state (need: Need) =
    match state with
    | x when need.Gaining = x -> need
    | _ -> {need with Gaining = state}

let changeNeedMag rate (need: Need) =
    {need with ChangeRate = rate}

let changeNeedDirAndMag state rate (need: Need) =
    changeNeedDir state need |> changeNeedMag rate

/// This is for a single need, will need adjustment for use with the map
let updateNeed (need: Need) =
    match need.Gaining with
    | true when need.CurrentValue <> need.Maxi ->
        let newValue = need.CurrentValue + need.ChangeRate
        if newValue > need.Maxi then
            {need with CurrentValue = need.Maxi}
        else  
            {need with CurrentValue = newValue}
    | false when need.CurrentValue <> need.Mini ->
        let newValue = need.CurrentValue - need.ChangeRate
        if newValue < need.Mini then
            {need with CurrentValue = need.Mini}
        else  
            {need with CurrentValue = newValue}
    | _ -> need


let updateNeedMap (needMap : Map<NeedName,Need>) =
    Map.map (fun (name : NeedName) need -> updateNeed need) needMap

let mapBounce needMap =
    Map.map (fun (name : NeedName) (need : Need) ->
        match need.CurrentValue with
        | x when x = need.Maxi ->
            {need with Gaining = (not need.Gaining)}
        | x when x = need.Mini ->
            {need with Gaining = (not need.Gaining)}
        | _ -> need
    ) needMap

// Defaults for a given "class" to use
let defaultRest =
    {
        CurrentValue = 6.0;
        Maxi = 10.0;
        Mini = 0.0;
        Gaining = true;
        DecayDirection = Down;
        ChangeRate = 0.1;
    }

let defaultBoredom =
    {
        CurrentValue = 6.0;
        Maxi = 10.0;
        Mini = 0.0;
        Gaining = true;
        DecayDirection = Up;
        ChangeRate = 0.1;
    }

let defaultMapOfNeeds = Map.empty.Add(Rest,defaultRest).Add(Boredom,defaultBoredom)




let foo = Map.tryFind Rest defaultMapOfNeeds
let unwrap bar =
    match bar with
    | None -> None
    | _ -> bar.Value

