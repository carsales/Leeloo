#if INTERACTIVE
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__ + "\.."
#endif

#r "packages/Leeloo.{{Version}}/tools/FakeLib.dll"
#r "packages/Leeloo.{{Version}}/tools/Leeloo.dll"

open Fake
open Fake.AssemblyInfoFile
open Leeloo

new System.Exception("Edit build.fsx to remove this line and configure your specifics") |> raise
let version = "<Nuget version and SolutionInfo version>"
let interfaceProjectName = "<Your base *interface* project name>"

let buildFrameworks = [ V35 ; V451 ]

let testDir = "./test/"

let buildDir = Multipass.Defaults.buildPath
let srcDir = Multipass.Defaults.sourcesPath

let nuspecFileBase = "sample.nuspec"

let specialisedRefs = 
    function 
    | p when p = interfaceProjectName -> []
    | _ -> [ interfaceProjectName, version ]

let shouldBuildForFramework (version: FrameworkVersion) (project: string) =
    match project, version with
    | _ -> true

let nugetableProjects = interfaceProjectName |> Multipass.nugetableProjects id

Target "Clean" (fun _ -> 
    Multipass.artefactDirectories @ [ testDir ]
    |> List.iter CleanDir)

Target "UpdateSolutionInfo" (fun _ -> 
    let pathToSolutionInfo = buildDir @@ "SolutionInfo.cs"
    CreateCSharpAssemblyInfo pathToSolutionInfo
        [ Attribute.Version version; Attribute.FileVersion version ])

Target "Build" (fun _ -> 
    nugetableProjects |> Multipass.buildForAllFrameworks (fun a -> 
        { a with ShouldBuildForFramework = shouldBuildForFramework
                 Frameworks = buildFrameworks }))

Target "BuildTests" (fun _ -> 
    let testsPath = srcDir @@ "*.Tests" @@ "*.csproj"

    !! testsPath
    |> MSBuildRelease testDir "Build"
    |> Log "Build tests ")

Target "Test" (fun _ -> 
    let testPattern = testDir + "*.Tests.dll"

    !! testPattern 
    |> NUnitParallel(fun p -> 
                        { p with DisableShadowCopy = true;
                                 OutputFile = testDir + "TestResults.xml" }))

Target "Nuget" (fun _ -> 
    let packageBuilder = Multipass.createNugetForProject (fun a -> 
        {a with FrameworksToBuild = buildFrameworks
                NuspecTemplatePath = nuspecFileBase
                ShouldBuildForFramework = shouldBuildForFramework
                SpecialisedReferences = specialisedRefs
                Version = version})
        
    nugetableProjects |> Seq.iter packageBuilder)

Target "Default" (fun _ -> 
    trace <| "Built " + interfaceProjectName + " and children.")

"Clean" 
==> "UpdateSolutionInfo" 
==> "Build"
==> "BuildTests"
==> "Test"
==> "Nuget"
==> "Default"

RunTargetOrDefault "Default"