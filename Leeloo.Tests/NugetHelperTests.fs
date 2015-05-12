namespace Leeloo.Tests

open Leeloo
open NUnit.Framework
open NuGet

[<TestFixture>]
type ``Run tests over nuget helper``() = 
    let packagesPath = @"..\..\..\packages"
    let repository = new LocalPackageRepository(packagesPath)
    let sampleProjectDirPath = @"..\..\..\Leeloo.TestProject"
    let sampleProjectPath = sampleProjectDirPath @@ "Leeloo.TestProject.csproj"
    let sampleProject = CsProjHelper.loadProj <| sampleProjectPath
    
    let forceFramework (mpackage: IPackageFile option) = 
        mpackage |> Option.bind (fun packageFile -> FrameworkVersion.FromTargetFramework packageFile.TargetFramework )
                 |> Option.get

    [<Test>]
    member t.``Assert that nunit was able to be found``() =    
        let nunit = repository.FindPackage "NUnit"
        Assert.NotNull nunit

    [<Test>]
    member t.``Assert that sample project was loaded``() =
        Assert.That (fi <| sampleProjectPath).Exists
        Assert.NotNull sampleProject

    [<Test>]
    member t.``Assert that all the versions of csn.logging were found``() =
        let csn_logging = repository.FindPackage "Csn.Logging"
        let availableVersions = NugetHelper.versionsAvailable csn_logging 
                                |> Seq.map snd
                                |> Seq.toList

        Assert.That(availableVersions, Has.Length.EqualTo 4)

        Assert.That(availableVersions, Contains.Item FrameworkVersion.V35)
        Assert.That(availableVersions, Contains.Item FrameworkVersion.V4)
        Assert.That(availableVersions, Contains.Item FrameworkVersion.V45)
        Assert.That(availableVersions, Contains.Item FrameworkVersion.V451)

    [<Test>]
    member t.``Can find version of csn.logging for various versions``() =
        let csn_logging = repository.FindPackage "Csn.Logging"
        let v451 = NugetHelper.referenceForVersion FrameworkVersion.V451 csn_logging |> forceFramework
        let v4   = NugetHelper.referenceForVersion FrameworkVersion.V4   csn_logging |> forceFramework
        let v35  = NugetHelper.referenceForVersion FrameworkVersion.V35  csn_logging |> forceFramework

        Assert.That(v451, Is.EqualTo FrameworkVersion.V451)
        Assert.That(v4, Is.EqualTo FrameworkVersion.V4)
        Assert.That(v35, Is.EqualTo FrameworkVersion.V35)
        ()

    [<Test>]
    member t.``Nunit returns the flat lib folder``() =
        let nunit = repository.FindPackage "NUnit"

        let asm451 = NugetHelper.referenceForVersion FrameworkVersion.V451 nunit |> Option.get
        let asm2   = NugetHelper.referenceForVersion FrameworkVersion.V2   nunit |> Option.get

        Assert.That(asm451.Path, Is.EqualTo(asm2.Path))