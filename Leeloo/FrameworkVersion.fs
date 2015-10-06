namespace Leeloo
        
type FrameworkVersion = V2 | V35 | V4 | V45 | V451 | V452 | V46
    with 
        member x.VersionsBelowThisOne = 
            match x with
            | V2   -> [V2]
            | V35  -> [V2; V35]
            | V4   -> [V2; V35; V4]
            | V45  -> [V2; V35; V4; V45]
            | V451 -> [V2; V35; V4; V45; V451]
            | V452 -> [V2; V35; V4; V45; V451; V451]
            | V46  -> [V2; V35; V4; V45; V451; V451; V452]

        member x.ToFrameworkVersionFlag = 
            match x with
            | V2   -> "v2.0"
            | V35  -> "v3.5"
            | V4   -> "v4.0"
            | V45  -> "v4.5"
            | V451 -> "v4.5.1"
            | V452 -> "v4.5.2"
            | V46  -> "v4.6"

        member x.ToNugetPath = 
            match x with
            | V2   -> "net20"
            | V35  -> "net35"
            | V4   -> "net40"
            | V45  -> "net45"
            | V451 -> "net451"
            | V452 -> "net452"
            | V46  -> "net46"

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
            | "net452"      -> V452 |> Some
            | "net452-full" -> V452 |> Some
            | "net46"       -> V46  |> Some
            | "net46-full"  -> V46  |> Some
            | _ -> None

        static member FromTargetFramework (framework: System.Runtime.Versioning.FrameworkName): FrameworkVersion Option =
            match framework with 
            | null -> None
            | f -> 
                match f.FullName with
                | ".NETFramework,Version=v2.0"   -> V2   |> Some
                | ".NETFramework,Version=v3.5"   -> V35  |> Some
                | ".NETFramework,Version=v4.0"   -> V4   |> Some
                | ".NETFramework,Version=v4.5"   -> V45  |> Some
                | ".NETFramework,Version=v4.5.1" -> V451 |> Some
                | ".NETFramework,Version=v4.5.2" -> V452 |> Some
                | ".NETFramework,Version=v4.6"   -> V46  |> Some
                | _ -> None

        static member ParseTargetFramework (s: string): FrameworkVersion Option =
            match s.ToLowerInvariant() with
            | "v2.0"   -> V2   |> Some
            | "v3.5"   -> V35  |> Some
            | "v4.0"   -> V4   |> Some
            | "v4.5"   -> V45  |> Some
            | "v4.5.1" -> V451 |> Some
            | "v4.5.2" -> V452 |> Some
            | "v4.6"   -> V46  |> Some
            | _ -> None
    end

