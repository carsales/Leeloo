namespace Leeloo

module Multipass =
    open Fake
    open Leeloo
    open FsFs
    open System.IO
    open System.Text.RegularExpressions
    
    let artefactDirectories (paths: LeelooPaths) = [ 
        paths.BuildPath
        paths.PackagingWorkPath
        paths.PackageOutputPath ]

    let packages packagesDir = 
        (Dirs.ofString packagesDir) <**> PackageInfo.FromDirectory 
        |> onlySome
        |> Seq.filter (fun pi -> pi.Frameworks |> List.isEmpty |> not)
        |> Seq.toList

    type NugetableProjectsArg       = { IsExcludedProject : string -> bool }        
    let defaultNugetableProjectsArg = { IsExcludedProject = LeelooDefaults.isExcludedProject; }

    let nugetableProjects (paths: LeelooPaths) (callback: NugetableProjectsArg -> NugetableProjectsArg) (interfaceProjectName: string) = 
        let testProj (name: string) = name.Contains "Tests"
    
        let config = callback <| defaultNugetableProjectsArg

        let isChild (name: string) 
            = [testProj; config.IsExcludedProject] 
           |> Seq.map    (fun f -> not << f)
           |> Seq.forall (fun f -> f name)

        let projectPattern = paths.SourcesPath @@ interfaceProjectName + ".*/"

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
        ; FrameworksToBuild: FrameworkVersion seq
        ; ShouldBuildForFramework: FrameworkVersion -> string -> bool
        ; SpecialisedReferences: string -> (string * string) list }
    let defaultCreateNugetForProjectArgs =
        { Version = "0.0.1"; NuspecTemplatePath = ""
        ; FrameworksToBuild = [V451]
        ; ShouldBuildForFramework = LeelooDefaults.shouldBuildForProject
        ; SpecialisedReferences = konst [] }

    let createNugetForProject (paths: LeelooPaths) (callback: CreateNugetForProjectArgs -> CreateNugetForProjectArgs) (name : string) = 
        let config = callback defaultCreateNugetForProjectArgs

        let workDir = paths.PackagingWorkPath @@ name

        CleanDir workDir

        let libDir = workDir @@ "lib"
        let nuspecFile = config.NuspecTemplatePath

        let dependencies = 
            let fileName = paths.BuildPath @@ name @@ "packages.config" 

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

                !! (paths.BuildPath @@ name @@ frameworkVersionName @@ name + ".dll") 
                ++ (paths.BuildPath @@ name @@ frameworkVersionName @@ name + ".pdb") 
                |> CopyTo outputDir)

        nuspecFile |> NuGet (fun p -> 
                          { p with Project = name;
                                   WorkingDir = workDir;
                                   Version = config.Version;
                                   Dependencies = dependencies @ config.SpecialisedReferences name;
                                   OutputPath = paths.PackageOutputPath })
                               
        Log "Created nuget package for " [name]
    
    type BuildForAllFrameworksArgs = 
        { ShouldBuildForFramework: FrameworkVersion -> string -> bool
        ; Frameworks: FrameworkVersion seq}
    let defaultBuildForAllFrameworksArgs = 
        { ShouldBuildForFramework = LeelooDefaults.shouldBuildForProject
        ; Frameworks = LeelooDefaults.frameworksToBuild }

    let buildForAllFrameworks (paths: LeelooPaths) (callback: BuildForAllFrameworksArgs -> BuildForAllFrameworksArgs) nugetableProjects =
        let config = callback defaultBuildForAllFrameworksArgs

        let createCopyOfSource projectName = 
            let sourceDir = paths.SourcesPath @@ projectName
            let buildDir  = paths.BuildPath   @@ projectName

            let sourceDi = new DirectoryInfo(sourceDir)
            let destDi   = new DirectoryInfo(buildDir)

            sprintf "Copying %s to %s" sourceDi.FullName destDi.FullName |> Log <| []

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
                framework.VersionsBelow
                |> Seq.map (fun v -> v.ToNugetPath.ToUpperInvariant())
                |> String.concat ";"

            let setFrameworkVersionForProject (projectName: string) =
                let message = "Updating project file " + projectName + " to use " + framework.ToFrameworkVersionFlag
                Log message []
                let csproj = paths.BuildPath @@ projectName @@ projectName + ".csproj"

                let fileContents = File.ReadAllText csproj
                let updatedContents = regex.Replace(fileContents, targetFramework)

                File.WriteAllText(csproj, updatedContents)

            let runNugetUpdate (projectName: string) =
                let packagesConfig = paths.BuildPath @@ projectName @@ "packages.config"
                let projectFile    = paths.BuildPath @@ projectName @@ projectName + ".csproj"
                NugetHelper.runNugetUpdate paths.PackagesPath packagesConfig projectFile

            Log "Building for " [|framework.ToFrameworkVersionFlag|]

            nugetableProjects 
            |> Seq.iter setFrameworkVersionForProject

            nugetableProjects
            |> Seq.iter runNugetUpdate

            nugetableProjects
            |> Seq.iter (fun projectName ->
                let buildDir = paths.BuildPath @@ projectName
                let csproj   = buildDir        @@ projectName + ".csproj"
                let buildDir = buildDir        @@ framework.ToNugetPath

                let flags = ["DefineConstants", definedFrameworkVersions]

                !! csproj 
                |> MSBuildReleaseExt buildDir flags "Build"
                |> Log "Built ")
        )    

