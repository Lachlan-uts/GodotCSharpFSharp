module NeedSystem

type DecayDirection = Up | Down

type NeedName = Rest | Boredom

type Gaining = bool

type Need =
    {
        //NeedName: NeedName;
        CurrentValue: float;
        Maxi: float; // also the optimum value
        Mini: float; // also the worst value
        GainState: Gaining;
        DecayDirection: DecayDirection; // Only a hint for the visual system
        ChangeRate: float;
    }
// update method
// adjust methods
// evaluate method

let changeNeedDir state (need: Need) =
    match state with
    | x when need.GainState = x -> need
    | _ -> {need with GainState = state}

let changeNeedMag rate (need: Need) =
    {need with ChangeRate = rate}

let changeNeedDirAndMag state rate (need: Need) =
    changeNeedDir state need |> changeNeedMag rate

/// This is for a single need, will need adjustment for use with the map
let updateNeed (need: Need) =
    match need.GainState with
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

/// From here in the file I think I'll work on map (collection of need) functions

let updateNeedMap (needMap : Map<NeedName,Need>) =
    Map.map (fun (name : NeedName) need -> updateNeed need) needMap

let mapBounce needMap =
    Map.map (fun (name : NeedName) (need : Need) ->
        match need.CurrentValue with
        | x when x = need.Maxi ->
            {need with GainState = (not need.GainState)}
        | x when x = need.Mini ->
            {need with GainState = (not need.GainState)}
        | _ -> need
    ) needMap

/// This will return what the currently most important need is, given either a need or nothing.
let getPriorityNeed (needMap : Map<NeedName,Need>) currentNeed =
    match currentNeed with
    // When none, we can just use any need as a starting value
    | None -> Map.fold (fun x y z -> if x > z.CurrentValue then z.CurrentValue else x) (needMap |> Seq.head).Value.CurrentValue needMap 
    | _ -> Map.fold (fun x y z -> if x-2.0 > z.CurrentValue then z.CurrentValue else x) (needMap.TryFind(currentNeed.Value).Value.CurrentValue) needMap // Make the existing value slightly "stickier"

/// need a change task or task change needs or somesuch function now

// Defaults for a given "class" to use
let defaultRest =
    {
        CurrentValue = 6.0;
        Maxi = 10.0;
        Mini = 0.0;
        GainState = true;
        DecayDirection = Down;
        ChangeRate = 0.1;
    }

let defaultBoredom =
    {
        CurrentValue = 5.0;
        Maxi = 10.0;
        Mini = 0.0;
        GainState = true;
        DecayDirection = Up;
        ChangeRate = 0.1;
    }

let defaultMapOfNeeds = Map.empty.Add(Rest,defaultRest).Add(Boredom,defaultBoredom)

/// Going to start working on the task system now. I'm not sure if it should be a seperate module, cause it'd almost have to open this module regardless...
/// So I'm just going to start work here and then pull it out later if I decide it would look better like that
module TaskSystem =
    type TaskNames = Wander | Nap

    type TaskNeeds =
        {
            Gaining : NeedName list
            Decaying : NeedName list
        }

    type Task =
        | Started
        | InProgress
        | Completed

    // Now I think I'll make a list of all the tasks eventually but I'll start by declaring them discretely

    let wander =
        {
            Gaining = [Boredom;]
            Decaying = [Rest;]
        }
    let nap =
        {
            Gaining = [Rest;]
            Decaying = [Boredom;]
        }
    let chinwag =
        {
            Gaining = [Rest; Boredom;]
            Decaying = []
        }

    let changeTask needMap newTask =
        needMap

