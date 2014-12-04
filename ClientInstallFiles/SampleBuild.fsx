#if INTERACTIVE
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
#endif

#I "../packages/Leeloo.{{Version}}/tools/"


#r "FakeLib.dll"
#r "Leeloo.dll"

open Fake
open Fake.AssemblyInfoFile
open Leeloo

new System.Exception("Edit build.fsx to remove this line and configure your specifics") |> raise
let version = "<Nuget version and SolutionInfo version>"

let nuspecFileBase = "sample.nuspec"

let projectRoot = ".."
let paths = new LeelooPaths(new System.IO.DirectoryInfo(projectRoot))

let buildFrameworks = [ V35 ; V451 ]

let testDir = "./test/"

(* Use this to override which projects are built for frameworks *)
let shouldBuildForFramework (version: FrameworkVersion) (project: string) =
    match project, version with
//    | "ProjectName", v -> v >= V35 (* Builds "ProjectName" for any framework above 3.5 *)
    | _ -> true

(*

    You must also select (by uncommenting) one of the following options:

        A) Single project - just take multiple passes over a single project.

        B) Single Interface, Many Implementations - use a convention based approach
            to easily build a base project which contains contracts and self contained logic
            and implementations for various technologies. This pattern is useful in that enables
            many packages to reference some common contract, and the application can then just provide 
            the implementation.
*)


(* A) Single project *)
//(* Set your project name here *)
//let nugetableProjects = ["<Your project name>"]
//
//(* Use this to include dlls and pdbs from base libraries, e.g. a business or infrastructure layer. *)
//let additionalBuildArtefacts = [
////    "SomeSharedLibrary" (* Will include the SomeSharedLibrary.dll and SomeSharedLibrary.pdb in the lib directory for the nuget package *)
//]
//
//(* Overrides for adding additional nuget references. Useful when projects are built as nuget packages *)
//let specialisedRefs: MultipassTypes.SpecialisedReferencesMap = 
//    function 
////    | "ProjectName" -> ["ChildProjectName", version] (* When building "ProjectName", an additional nuget reference will be added for ChildProjectName *)
//    | _ -> []


(* B) Build using interface+implementations convention *)
//(* Set your interface name here *)
//let interfaceProjectName = "<Your base *interface* project name>"
//
//(* 
//    Helper to determin any "derived" projects - the id can be replaced with a callback to change the parameters.
//
//    As an example, with the following projects
//
//    Logging              *
//    Logging.Tests        
//    Logging.NLog         *
//    Logging.Log4Net      *
//
//    All but the .Tests project will be returned for building        
//*)
//let nugetableProjects = interfaceProjectName |> InterfaceAndTechnologyPattern.projectsByConvention paths id
//
//(* Use this to include dlls and pdbs from base libraries, e.g. a business or infrastructure layer. *)
//let additionalBuildArtefacts = [
////    "SomeSharedLibrary" (* Will include the SomeSharedLibrary.dll and SomeSharedLibrary.pdb in the lib directory for the nuget package *)
//]
//
//(* This sets the interface as a nuget dependency for everything other than the interface itself *)
//let specialisedRefs: MultipassTypes.SpecialisedReferencesMap = 
//    function 
//    | p when p = interfaceProjectName -> []
////    | "ProjectName" -> ["ChildProjectName", version] (* When building "ProjectName", an additional nuget reference will be added for ChildProjectName *)
//    | _ -> [ interfaceProjectName, version ]


Target "Clean" (fun _ -> 
    Multipass.artefactDirectories paths @ [ testDir ]
    |> List.iter CleanDir)

Target "UpdateSolutionInfo" (fun _ -> 
    let pathToSolutionInfo = paths.BuildPath @@ "SolutionInfo.cs"
    CreateCSharpAssemblyInfo pathToSolutionInfo
        [ Attribute.Version version; Attribute.FileVersion version ])

Target "Build" (fun _ -> 
    nugetableProjects |> Multipass.copyAndBuildForAllFrameworks paths (fun a -> 
        { a with ShouldBuildForFramework = shouldBuildForFramework
                 Frameworks = buildFrameworks }))

Target "BuildTests" (fun _ -> 
    paths.SourcesPath |> CopyDir testDir <| konst true

    let testProjects = testDir @@ "*.Tests" @@ "*.csproj"

    !! testProjects
    |> MSBuildRelease paths.BuildPath "Build"
    |> Log "Build tests ")

Target "Test" (fun _ -> 
    let testPattern = paths.BuildPath + "*.Tests.dll"

    !! testPattern 
    |> NUnitParallel(fun p -> 
                        { p with DisableShadowCopy = true;
                                 OutputFile = paths.BuildPath @@ "TestResults.xml" }))

Target "Nuget" (fun _ -> 
    let packageBuilder = Multipass.createNugetForProject paths (fun a -> 
        {a with FrameworksToBuild = buildFrameworks
                NuspecTemplatePath = nuspecFileBase                
                ShouldBuildForFramework = shouldBuildForFramework
                SpecialisedReferences = specialisedRefs
                IncludeOutputFromProjectsNamed = additionalBuildArtefacts
                Version = version})
        
    nugetableProjects |> Seq.iter packageBuilder)

Target "Default" (fun _ -> 
    trace <| "Builtd complete.")

"Clean" 
==> "UpdateSolutionInfo" 
==> "Build"
==> "BuildTests"
==> "Test"
==> "Nuget"
==> "Default"

RunTargetOrDefault "Default"