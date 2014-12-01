namespace Leeloo

[<AutoOpen>]
module Paths =
    open Fake

    type LeelooPaths(projectRoot: System.IO.DirectoryInfo) =

        do if(not projectRoot.Exists) 
            then new System.Exception(sprintf "Project root %s does not exist" projectRoot.FullName) |> raise        
    
        member val SourcesPath       = projectRoot.FullName @@ LeelooDefaults.sourcesPath             with get, set
        member val PackagesPath      = projectRoot.FullName @@ LeelooDefaults.installedPackagesPath   with get, set
        member val BuildPath         = projectRoot.FullName @@ LeelooDefaults.buildPath               with get, set
        member val PackagingWorkPath = projectRoot.FullName @@ LeelooDefaults.packagingWorkPath       with get, set
        member val PackageOutputPath = projectRoot.FullName @@ LeelooDefaults.packageOutputPath       with get, set
        member val NugetExePath      = projectRoot.FullName @@ LeelooDefaults.nugetExePath            with get, set

