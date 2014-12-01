open NuGet
open NuGet.Common
open System
open Fake

Environment.CurrentDirectory <- "d:/dropbox/dev/csn.logging/leeloo/build"

let projectName = "Csn.Logging"
let packageRepo = PackageRepositoryFactory.Default.CreateRepository "https://packages.nuget.org/api/v2"

let packagePathResolver = new DefaultPackagePathResolver("../../packages")
let projectSystem = new MSBuildProjectSystem(projectName @@ projectName + ".csproj")
let localRepo = new LocalPackageRepository("../../packages")
let projectManager = new ProjectManager(packageRepo, packagePathResolver, projectSystem, localRepo)