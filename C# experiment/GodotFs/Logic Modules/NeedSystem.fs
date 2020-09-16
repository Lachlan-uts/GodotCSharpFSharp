module NeedSystem

type DecayDirection = Up | Down

type Need =
    {
        //NeedName: NeedName;
        CurrentValue: float;
        Maxi: float; // also the optimum value
        Mini: float; // also the worst value
        Decaying: bool;
        DecayDirection: DecayDirection; // a hint for the visual system
        ChangeRate: float;
    }

type Needs =
    | Rest of Need
    | Boredom of Need


// update method

// adjust methods

// evaluate method

let defaultRest =
    {
        //NeedName = Rest;
        CurrentValue = 6.0;
        Maxi = 10.0;
        Mini = 0.0;
        Decaying = true;
        DecayDirection = Down;
        ChangeRate = 0.1;
    }

let defaultBoredom =
    {
        //NeedName = Boredom;
        CurrentValue = 6.0;
        Maxi = 10.0;
        Mini = 0.0;
        Decaying = true;
        DecayDirection = Up;
        ChangeRate = 0.1;
    }

let defRest = Rest {
    CurrentValue = 6.0;
    Maxi = 10.0;
    Mini = 0.0;
    Decaying = true;
    DecayDirection = Down;
    ChangeRate = 0.1;
}

let defBoredom = Boredom {
    CurrentValue = 6.0;
    Maxi = 10.0;
    Mini = 0.0;
    Decaying = true;
    DecayDirection = Up;
    ChangeRate = 0.1;
}

let bar = defaultRest.ChangeRate

let getValue x =
    match x with
    | Rest x -> true
    | _ -> false

let defaultListOfNeeds = [
    defRest
    defBoredom
    ]

let blah = defaultListOfNeeds |> List.find getValue

type NeedName = Rest | Boredom

let defaultMapOfNeeds = Map.empty.Add(Rest,defaultRest).Add(Boredom,defaultBoredom)

let foo = Map.tryFind Rest defaultMapOfNeeds

let unwrap bar =
    match bar with
    | None -> None
    | _ -> bar.Value

