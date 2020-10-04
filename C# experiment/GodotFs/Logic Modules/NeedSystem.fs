module NeedSystem

type DecayDirection = Up | Down

type NeedName = Rest | Boredom

//type Gaining = bool

type Need =
    {
        //NeedName: NeedName;
        CurrentValue: float;
        Maxi: float; // also the optimum value
        Mini: float; // also the worst value
        GainState: bool;
        DecayDirection: DecayDirection; // Only a hint for the visual system
        ChangeRate: float;
    }
// update method
// adjust methods
// evaluate method

let changeNeedDirOpt newDir (need : Need Option)=
    match need with
        | None -> None
        | Some x -> Some {x with GainState=newDir}

let changeNeedDir state (need: Need) =
    match state with
    | x when need.GainState = x -> need
    | _ -> {need with GainState = state}

let changeNeedMag rate (need: Need) =
    {need with ChangeRate = rate}

let changeNeedDirAndMag state rate (need: Need) =
    changeNeedDir state need |> changeNeedMag rate

/// This is for a single need, will need adjustment for use with the map
/// This provides a "tick" for incrementing a needs value.
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
    | None -> 
        let increTup = (needMap |> Seq.head) |> (fun x -> (x.Key, x.Value.CurrentValue))
        //Map.fold (fun (x1,x2) y z -> if x2 > z.CurrentValue then (x1,z.CurrentValue)else x) increTup needMap 
        Map.fold (fun x y z -> match x with
                                | (x1, _) when x1 = y -> x
                                | (x1, x2) when x2 > z.CurrentValue -> (x1,z.CurrentValue)
                                | _ -> x
                    ) increTup needMap
    | Some a -> 
        Map.fold (fun x y z -> match x with
                                | (x1, _) when x1 = y -> x
                                | (x1, x2) when x2 > z.CurrentValue -> (x1,z.CurrentValue)
                                | _ -> x
                    ) (a, (needMap.TryFind(a).Value.CurrentValue)) needMap
    //| _ -> Map.fold (fun x y z -> if x-2.0 > z.CurrentValue then z.CurrentValue else x) (needMap.TryFind(currentNeed.Value).Value.CurrentValue) needMap // Make the existing value slightly "stickier"



/// need a change task or task change needs or somesuch function now

// Defaults for a given "class" to use
let defaultRest =
    {
        CurrentValue = 8.0;
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

    type TaskNames = Wander | Nap | Chinwag

    type TaskNeeds =
        {
            Gaining : NeedName list
            Decaying : NeedName list
        }

    type Task =
        | Started
        | InProgress
        | Completed

    // Could be used to build a persistent list of tasks
    type TaskQ =
        | TaskN of TaskNames
        | Completed
        | Aborted

    // Now I think I'll make a list of all the tasks eventually but I'll start by declaring them discretely

    // I don't know if this is poor form or not idiomatic
    // but due to lists always being ordered, even when their values arn't comparable
    // for this priority need, I just just grab the head of the list.

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

    let defaultMapOfTasks = Map.empty.Add(Wander,wander).Add(Nap,nap).Add(Chinwag,chinwag)

    let taskToList taskNeeds =
        List.map (fun x -> (x, true)) taskNeeds.Gaining |> List.append (List.map (fun x -> (x,false)) taskNeeds.Decaying)


    let getTaskNameFromPriorityNeed tMap need =
        Map.tryFindKey (fun k v -> if v.Gaining.Head = need then true else false) tMap

    // Returns a single Task from a given need
    let getTaskNeedsFromPriorityNeed tMap need =
        // this map now only contains tasks with the given need as the head of the gaining list
        //(Map.filter (fun k v -> v.Gaining.Head = need) tMap)
        Map.tryPick (fun k v -> if v.Gaining.Head = need then Some v else None) tMap

    let change key f map =
        Map.tryFind key map
        |> f
        |> function
            | Some v -> Map.add key v map
            | None -> Map.remove key map

    let applyTaskToNeeds needMap tupList =
        List.fold (fun nm (k,v) -> change k (changeNeedDirOpt v) nm) needMap tupList
        //List.fold (fun nm (k,v) -> if Map.containsKey k nm then Map.add k v nm else nm) needMap tupList

    let updateNeedsWithTask task needMap =
        match task with
        | Some x -> taskToList x |> applyTaskToNeeds needMap
        | None -> needMap
        

    let changeTask needMap newTask =
        //Using a task I need to operate on the needs, making them gain or decay
        //I think I'll try to do it in stages.
        needMap

        //let sNeedList = List.sortBy (fun (_,y) -> -y) ((Map.map (fun k v -> v.CurrentValue) defaultMapOfNeeds) |> Map.toList)

        //so I realised like 5 minutes after the last recording that putting a function in brackets makes it a partial function, so the pipe then works correctly!
        //like below
        //(Map.map (fun k v -> v.CurrentValue) defaultMapOfNeeds) |> Map.toList |> (List.sortBy (fun (_,y) -> y))

