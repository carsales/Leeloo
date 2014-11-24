namespace Leeloo
        
type FrameworkVersion = V2 | V35 | V4 | V45 | V451
    with 
        static member VersionsBelow = 
            function
            | V2   -> [V2]
            | V35  -> [V2; V35]
            | V4   -> [V2; V35; V4]
            | V45  -> [V2; V35; V4; V45]
            | V451 -> [V2; V35; V4; V45; V451]

        member x.ToFrameworkVersionFlag = 
            match x with
            | V2   -> "v2.0"
            | V35  -> "v3.5"
            | V4   -> "v4.0"
            | V45  -> "v4.5"
            | V451 -> "v4.5.1"

        member x.ToNugetPath = 
            match x with
            | V2   -> "net20"
            | V35  -> "net35"
            | V4   -> "net40"
            | V45  -> "net45"
            | V451 -> "net451"

        static member ParseNugetPath (s: string): FrameworkVersion Option =
            match s.ToLowerInvariant() with
            | "net20"       -> V2   |> Some
            | "net20-full"  -> V2   |> Some
            | "net35"       -> V35  |> Some
            | "net35-full"  -> V35  |> Some
            | "net40"       -> V4   |> Some
            | "net40-full"  -> V4   |> Some
            | "net45"       -> V45  |> Some
            | "net45-full"  -> V45  |> Some
            | "net451"      -> V451 |> Some
            | "net451-full" -> V451 |> Some
            | _ -> None

        static member ParseTargetFramework (s: string): FrameworkVersion Option =
            match s.ToLowerInvariant() with
            | "v2.0"   -> V2   |> Some
            | "v3.5"   -> V35  |> Some
            | "v4.0"   -> V4   |> Some
            | "v4.5"   -> V45  |> Some
            | "v4.5.1" -> V451 |> Some
            | _ -> None
    end

