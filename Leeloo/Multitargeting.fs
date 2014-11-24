namespace Leeloo

module Multitargeting =
    open Fake
    open Leeloo
    open FsFs
    open System.IO
    open System.Text.RegularExpressions

    module Defaults =
        let sourcesPath           = "./src"
        let installedPackagesPath = "./packages"

        let buildPath             = "./build"
        let packagingWorkPath     = "./work"
        let packageOutputPath     = "./nupkgs"     

        let frameworksToBuild = [V451]

        let isExcludedProject = konst false
        let shouldBuildForProject _ _  = true
    
    let artefactDirectories = [ Defaults.buildPath ; Defaults.packagingWorkPath ; Defaults.packageOutputPath ]

    let packages packagesDir = 
        (Dirs.ofString packagesDir) <**> PackageInfo.FromDirectory 
        |> onlySome
        |> Seq.filter (fun pi -> pi.Frameworks |> List.isEmpty |> not)
        |> Seq.toList

    type NugetableProjectsArg = 
        { IsExcludedProject : string -> bool
        ; SourceDirectory: string }        
    let defaultNugetableProjectsArg = { IsExcludedProject = Defaults.isExcludedProject; SourceDirectory = Defaults.sourcesPath}
    let nugetableProjects (callback: NugetableProjectsArg -> NugetableProjectsArg) (interfaceProjectName: string) = 
        let testProj (name: string) = name.Contains "Tests"
    
        let config = callback defaultNugetableProjectsArg

        let isChild (name: string) 
            = [testProj; config.IsExcludedProject] 
           |> Seq.map    (fun f -> not << f)
           |> Seq.forall (fun f -> f name)

        let projectPattern = config.SourceDirectory @@ interfaceProjectName + ".*/"

        let projects = !! projectPattern
                       |> Seq.filter isChild   
                       |> Seq.map (fun dir -> (directoryInfo dir).Name)
                       |> Seq.toList

        trace << (+) "Will build packages: " 
              << String.concat "; " 
              <| projects

        projects

    type CreateNugetForProjectArgs = 
        { Version: string
        ; NuspecTemplatePath: string
        ; PackagingWorkingDirectory: string
        ; BuildDirectory: string
        ; PackageOutputDirectory: string
        ; FrameworksToBuild: FrameworkVersion seq
        ; ShouldBuildForFramework: FrameworkVersion -> string -> bool
        ; SpecialisedReferences: string -> (string * string) list
        }
    let defaultCreateNugetForProjectArgs =
        { Version = ""; NuspecTemplatePath = ""
        ; PackagingWorkingDirectory = Defaults.packagingWorkPath; BuildDirectory = Defaults.buildPath; PackageOutputDirectory = Defaults.packageOutputPath
        ; FrameworksToBuild = [V451]
        ; ShouldBuildForFramework = (fun _ _ -> true)
        ; SpecialisedReferences = konst [] }
    let createNugetForProject (callback: CreateNugetForProjectArgs -> CreateNugetForProjectArgs) (name : string) = 
        let config = callback defaultCreateNugetForProjectArgs

        let workDir = config.PackagingWorkingDirectory @@ name

        CleanDir workDir

        let libDir = workDir @@ "lib"
        let nuspecFile = config.NuspecTemplatePath

        let dependencies = 
            let fileName = "src" @@ name @@ "packages.config" 

            if fileExists fileName 
            then getDependencies fileName
            else []

        dependencies
        |> Seq.map (fun (x, y) -> x + " v" + y)
        |> Log "Found deps: "

        config.FrameworksToBuild |> Seq.iter (fun (frameworkVersion: FrameworkVersion) -> 
            if config.ShouldBuildForFramework frameworkVersion name
            then 
                let frameworkVersionName = frameworkVersion.ToNugetPath
                let outputDir = libDir @@ frameworkVersionName

                !! (config.BuildDirectory @@ name @@ frameworkVersionName @@ name + ".dll") 
                ++ (config.BuildDirectory @@ name @@ frameworkVersionName @@ name + ".pdb") 
                |> CopyTo outputDir)

        nuspecFile |> NuGet (fun p -> 
                          { p with Project = name;
                                   WorkingDir = workDir;
                                   Version = config.Version;
                                   Dependencies = dependencies @ config.SpecialisedReferences name;
                                   OutputPath = config.PackageOutputDirectory })
                               
        Log "Created nuget package for " [name]
    
    type BuildForAllFrameworksArgs = 
        { ShouldBuildForFramework: FrameworkVersion -> string -> bool
        ; Frameworks: FrameworkVersion seq
        ; BuildDir: string
        ; SourceDir: string
        ; PackagesDir: string}
    let defaultBuildForAllFrameworksArgs = 
        { BuildDir = Defaults.buildPath; PackagesDir = Defaults.installedPackagesPath; SourceDir = Defaults.sourcesPath
        ; ShouldBuildForFramework = Defaults.shouldBuildForProject; Frameworks = Defaults.frameworksToBuild }
    let buildForAllFrameworks (callback: BuildForAllFrameworksArgs -> BuildForAllFrameworksArgs) nugetableProjects =
        let config = callback defaultBuildForAllFrameworksArgs

        let createCopyOfSource projectName = 
            let sourceDir = config.SourceDir @@ projectName
            let buildDir  = config.BuildDir  @@ projectName

            sourceDir |> CopyDir buildDir <| konst true

        nugetableProjects |> Seq.iter createCopyOfSource
    
        let buildFrameworkVersion (s: string) = 
            "<TargetFrameworkVersion>" + s + "</TargetFrameworkVersion>"
        let regex = new Regex(buildFrameworkVersion "[^<]+")
        
        config.Frameworks |> Seq.iter (fun (framework: FrameworkVersion) ->
            let nugetableProjects = nugetableProjects |> Seq.filter (config.ShouldBuildForFramework framework)

            let targetFramework = framework.ToFrameworkVersionFlag 
                               |> buildFrameworkVersion

            let definedFrameworkVersions = 
                FrameworkVersion.VersionsBelow framework
                |> Seq.map (fun v -> v.ToNugetPath.ToUpperInvariant())
                |> String.concat ";"

            let setFrameworkVersionForProject (projectName: string) =
                let message = "Updating project file " + projectName + " to use " + framework.ToFrameworkVersionFlag
                Log message []
                let csproj = config.BuildDir @@ projectName @@ projectName + ".csproj"

                let fileContents = File.ReadAllText csproj
                let updatedContents = regex.Replace(fileContents, targetFramework)

                File.WriteAllText(csproj, updatedContents)

            let runNugetUpdate (projectName: string) =
                let packageFile = "./packages.config"

                Log "Updating nuget references from " [packageFile]

                let commandArgs = [ "update"; packageFile
                                  ; "-NonInteractive"; "-RepositoryPath"; config.PackagesDir] 
                                  |> String.concat " "
            
                let args = 
                    { Program = ".nuget/Nuget.exe"
                      WorkingDirectory = config.BuildDir @@ projectName
                      CommandLine = commandArgs
                      Args = []}

                Log "Running" [ args.ToString() ]

                asyncShellExec args

            Log "Building for " [|framework.ToFrameworkVersionFlag|]

            nugetableProjects 
            |> Seq.iter setFrameworkVersionForProject

            nugetableProjects
            |> Seq.map runNugetUpdate
            |> (Seq.toArray >> Async.Parallel >> Async.RunSynchronously)
            |> ignore

            nugetableProjects
            |> Seq.iter (fun projectName ->
                let buildDir = config.BuildDir @@ projectName
                let csproj = buildDir          @@ projectName + ".csproj"
                let buildDir = buildDir        @@ framework.ToNugetPath

                let flags = ["DefineConstants", definedFrameworkVersions]

                !! csproj 
                |> MSBuildReleaseExt buildDir flags "Build"
                |> Log "Built ")
        )    

