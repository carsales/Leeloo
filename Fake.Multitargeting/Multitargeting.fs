namespace Fake.Multitargeting

module Multitargeting =
    open Fake
    open Fake.Multitargeting
    open FsFs
    open System.IO
    open System.Text.RegularExpressions
    
    let packages packagesDir = 
        (Dirs.ofString packagesDir) <**> PackageInfo.FromDirectory 
        |> onlySome
        |> Seq.filter (fun pi -> pi.Frameworks |> List.isEmpty |> not)
        |> Seq.toList

    let nugetableProjects isExcludedProject srcDir interfaceProjectName = 
        let testProj (name: string) = name.Contains "Tests"
    
        let isChild (name: string) 
            = [testProj; isExcludedProject] 
           |> Seq.map    (fun f -> not << f)
           |> Seq.forall (fun f -> f name)

        let projectPattern = srcDir @@ interfaceProjectName + ".*/"

        let projects = !! projectPattern
                       |> Seq.filter isChild   
                       |> Seq.map (fun dir -> (directoryInfo dir).Name)
                       |> Seq.toList

        trace << (+) "Will build packages: " 
              << String.concat "; " 
              <| projects

        projects

    let createNugetForProject packagingWorkDir nuspecFileBase buildFrameworks shouldBuildForFramework buildDir version specialisedRefs packageOutputDirectory (name : string) = 
        let workDir = packagingWorkDir @@ name

        CleanDir workDir

        let libDir = workDir @@ "lib"
        let nuspecFile = nuspecFileBase        

        let dependencies = 
            let fileName = "src" @@ name @@ "packages.config" 

            if fileExists fileName 
            then getDependencies fileName
            else []

        dependencies
        |> Seq.map (fun (x, y) -> x + " v" + y)
        |> Log "Found deps: "

        buildFrameworks |> Seq.iter (fun (frameworkVersion: FrameworkVersion) -> 
            if shouldBuildForFramework name frameworkVersion
            then 
                let frameworkVersionName = frameworkVersion.ToNugetPath
                let outputDir = libDir @@ frameworkVersionName

                !! (buildDir @@ name @@ frameworkVersionName @@ name + ".dll") 
                ++ (buildDir @@ name @@ frameworkVersionName @@ name + ".pdb") 
                |> CopyTo outputDir)

        nuspecFile |> NuGet (fun p -> 
                          { p with Project = name;
                                   WorkingDir = workDir;
                                   Version = version;
                                   Dependencies = dependencies @ specialisedRefs name;
                                   OutputPath = packageOutputDirectory })
                               
        Log "Created nuget package for " [name]
    
    let buildForAllFrameworks shouldBuildForFramework frameworks buildDir sourceDir packagesDir nugetableProjects =
        let createCopyOfSource projectName = 
            let sourceDir = sourceDir @@ projectName
            let buildDir = buildDir @@ projectName

            sourceDir |> CopyDir buildDir <| fun _ -> true

        nugetableProjects |> Seq.iter createCopyOfSource
    
        let buildFrameworkVersion (s: string) = 
            "<TargetFrameworkVersion>" + s + "</TargetFrameworkVersion>"
        let regex = new Regex(buildFrameworkVersion "[^<]+")
        
        frameworks |> Seq.iter (fun (framework: FrameworkVersion) ->
            let shouldBuildForFramework = flip shouldBuildForFramework <| framework

            let nugetableProjects = nugetableProjects |> Seq.filter shouldBuildForFramework

            let targetFramework = framework.ToFrameworkVersionFlag 
                               |> buildFrameworkVersion

            let definedFrameworkVersions = 
                FrameworkVersion.VersionsBelow framework
                |> Seq.map (fun v -> v.ToNugetPath.ToUpperInvariant())
                |> String.concat ";"

            let setFrameworkVersionForProject (projectName: string) =
                let message = "Updating project file " + projectName + " to use " + framework.ToFrameworkVersionFlag
                Log message []
                let csproj = buildDir @@ projectName @@ projectName + ".csproj"

                let fileContents = File.ReadAllText csproj
                let updatedContents = regex.Replace(fileContents, targetFramework)

                File.WriteAllText(csproj, updatedContents)

            let runNugetUpdate (projectName: string) =
                let packageFile = "./packages.config"

                Log "Updating nuget references from " [packageFile]

                let commandArgs = [ "update"; packageFile
                                  ; "-NonInteractive"; "-RepositoryPath"; packagesDir] 
                                  |> String.concat " "
            
                let args = 
                    { Program = ".nuget/Nuget.exe"
                      WorkingDirectory = buildDir @@ projectName
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
                let buildDir = buildDir @@ projectName
                let csproj = buildDir @@ projectName + ".csproj"
                let buildDir = buildDir @@ framework.ToNugetPath

                let flags = ["DefineConstants", definedFrameworkVersions]

                !! csproj 
                |> MSBuildReleaseExt buildDir flags "Build"
                |> Log "Built ")
        )    

