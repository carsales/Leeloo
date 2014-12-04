namespace Leeloo

/// Module to handle do multiframework builds and nuget packaging of csprojs
module Multipass =
    open Fake
    open Leeloo
    open System.IO
    open System.Text.RegularExpressions
    open MultipassTypes

    let private targetFrameworkCsprojRegex = new Regex("<TargetFrameworkVersion>[^<]+</TargetFrameworkVersion>", RegexOptions.Compiled)    
    
    /// Directories that are appropriate to clean out
    let artefactDirectories (paths: LeelooPaths) = [ 
        paths.BuildPath
        paths.PackagingWorkPath
        paths.PackageOutputPath ]
        
    /// Copies all directories from the source path (e.g. "src") to the leeloo build path (e.g. "leeloo/build")
    let copySourcesToBuild (paths: LeelooPaths) = paths.SourcesPath |> CopyDir paths.BuildPath <| konst true

    let projectAndProjectReferences (paths: LeelooPaths) (projectName: string) =
        let projectFile = paths.BuildPath @@ projectName @@ projectName + ".csproj"

        let transitiveProjectReferences = 
            CsProjHelper.loadProj projectFile 
            |> CsProjHelper.projectReferences 
            |> Seq.toList
            
        sprintf "Transitive references for %s: " projectFile |> Log <| transitiveProjectReferences 
        projectName::transitiveProjectReferences
     
    /// XML pokes the project file to set nuget framework versions for csprojs in projectsToBuild
    let buildNugetableProjectsForFramework (paths: LeelooPaths) projectsToBuild (framework: FrameworkVersion) =

        let targetFramework = "<TargetFrameworkVersion>" + framework.ToFrameworkVersionFlag + "</TargetFrameworkVersion>"
        
        let setFrameworkVersionForProject (projectName: string) =
            "Updating project file " + projectName + " to use " + framework.ToFrameworkVersionFlag
            |> Log <| []

            let csproj = paths.BuildPath @@ projectName @@ projectName + ".csproj"
            
            let fileContents = File.ReadAllText csproj
            let updatedContents = targetFrameworkCsprojRegex.Replace(fileContents, targetFramework)
            File.WriteAllText(csproj, updatedContents)

        let runNugetUpdate (projectName: string) =
            let packagesConfig = paths.BuildPath @@ projectName @@ "packages.config"
            let projectFile    = paths.BuildPath @@ projectName @@ projectName + ".csproj"

            projectFile |> NugetHelper.runNugetUpdate paths.PackagesPath packagesConfig

        Log "Building for " [|framework.ToFrameworkVersionFlag|]

        let projectsToUpdate = projectsToBuild 
                               |> Seq.collect (projectAndProjectReferences paths) 
                               |> Seq.distinct 
                               |> Seq.toList

        projectsToUpdate |> Seq.iter setFrameworkVersionForProject
        projectsToUpdate |> Seq.iter runNugetUpdate

        projectsToBuild |> Seq.iter (fun projectName ->

            let buildDir = paths.BuildPath @@ projectName
            let csproj   = buildDir        @@ projectName + ".csproj"
            let buildDir = buildDir        @@ framework.ToNugetPath

            let flags = ["DefineConstants", framework.VersionsBelowThisOne
                                            |> Seq.map (fun v -> v.ToNugetPath.ToUpperInvariant())
                                            |> String.concat ";" 
                        ]

            !! csproj 
            |> MSBuildReleaseExt buildDir flags "Build"
            |> Log "Built ")

    /// Creates a nuget package for the specified project
    let createNugetForProject (paths: LeelooPaths) (callback: CreateNugetCallback) (name : string) = 
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

                let outputPath = paths.BuildPath @@ name @@ frameworkVersionName

                !! (outputPath @@ name + ".dll") 
                ++ (outputPath @@ name + ".pdb")
                |> CopyTo outputDir
                
                (* Copy all additional files *)
                config.IncludeOutputFromProjectsNamed |> Seq.iter (fun fileName -> 
                    !! (outputPath @@ fileName + ".dll")
                    ++ (outputPath @@ fileName + ".pdb")
                    |> CopyTo outputDir))

        nuspecFile |> NuGet (fun p -> 
                          { p with Project = name;
                                   WorkingDir = workDir;
                                   Version = config.Version;
                                   Dependencies = dependencies @ config.SpecialisedReferences name;
                                   OutputPath = paths.PackageOutputPath })
                               
        Log "Created nuget package for " [name]

    /// One function to copy them all in and in that directory build them
    let copyAndBuildForAllFrameworks (paths: LeelooPaths) (callback: BuildForAllCallback) nugetableProjects =
        let config = callback defaultBuildForAllFrameworksArgs

        copySourcesToBuild paths

        let projectsToBuild projects framework = config.ShouldBuildForFramework framework |> flip Seq.filter projects 

        let (>>=) f g a = g (f a) a
        let runner = projectsToBuild nugetableProjects >>= buildNugetableProjectsForFramework paths

        config.Frameworks |> Seq.iter runner

