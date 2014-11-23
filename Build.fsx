#if INTERACTIVE
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
#endif

#r "packages/FAKE.3.9.9/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile 

let buildDir = "./build"
let workDir = "./work"
let version = "0.1.0"

let dependencies = getDependencies "Fake.Multitargeting/packages.config"

Target "Clean" (fun _ -> 
    trace "Running clean"
    CleanDirs [ buildDir ; workDir ; workDir @@ "lib" ])

Target "Build" (fun _ ->
    [ Attribute.Version version ; Attribute.FileVersion version ]
    |> AssemblyInfoFile.CreateFSharpAssemblyInfo "./Fake.Multitargeting/AssemblyInfo.fs" 

    !! "./Fake.Multitargeting/Fake.Multitargeting.fsproj"
    |> MSBuildRelease buildDir "Build"
    |> Log "Building: ") 

Target "Nuget" (fun _ ->
    trace "Building package"

    buildDir @@ "Fake.Multitargeting.dll" |> CopyFile (workDir @@ "lib")

    "Fake.Multitargeting.nuspec" |> NuGet (fun p ->
        { p with Project = "Fake.Multitargeting"
                 Version = version
                 Dependencies = dependencies 
                 WorkingDir = workDir
                 OutputPath = "./"
                 ToolPath = "./packages/NuGet.CommandLine.2.8.3/tools/Nuget.exe" }))

Target "Default" (fun _ ->
    trace "Packaging done") 

"Clean" 
==> "Build"
==> "Nuget"
==> "Default"

RunTargetOrDefault "Default"