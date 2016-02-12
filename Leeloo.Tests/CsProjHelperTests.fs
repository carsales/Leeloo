namespace Leeloo.Tests

open System
open Leeloo
open NUnit.Framework
open NuGet
open System.IO
open CsProjHelper
open System.Xml.Linq

[<AutoOpen>]
module Utils =
    let fi name = new FileInfo(name)
    let (@@) left right = Path.Combine([|left ; right|])
    
[<TestFixture>]
type ``Run tests over csproj helper``() = 
    let sampleProjectPath = fi @"..\..\..\Leeloo.TestProject\Leeloo.TestProject.csproj"
    let project = loadProj sampleProjectPath.FullName

    [<Test>]
    member t.``Can load the sample project file``() =
        Assert.That sampleProjectPath.Exists
        Assert.NotNull project

    [<Test>]
    member t.``Can load the framework version of the sample project``() =
        let version = frameworkVersion project
        Assert.That(version, Is.EqualTo FrameworkVersion.V46)

    [<Test>]
    member t.``Can find a reference to nunit in the sample project``() =
        let packageName = "NUnit"
        let _, hintpath = findReference project packageName

        let reference = sampleProjectPath.Directory.FullName 
                        @@ hintpath.Value |> fi

        Assert.That reference.Exists

    [<Test>]
    member t.``Package name where assembly differs``() =
        let packageName = "NUnit"
        let referenceName = "nunit.framework"
        let xelem = XElement.Parse("""<HintPath>..\..\packages\NUnit.2.6.3\lib\nunit.framework.dll</HintPath>""")

        Assert.That <| packageNameMatchesReference packageName (referenceName, xelem)

    [<Test>]
    member t.``Package name with version matches``() =
        let packageName = "Newtonsoft.Json"
        let referenceName = "Newtonsoft.Json"
        let xelem = XElement.Parse("""<HintPath>..\..\packages\Newtonsoft.Json.6.0.6\lib\net45\Newtonsoft.Json.dll</HintPath>""")

        Assert.That <| packageNameMatchesReference packageName (referenceName, xelem)

    [<Test>]
    member t.``Package name without version matches``() =
        let packageName = "NLog"
        let referenceName = "NLog"
        let xelem = XElement.Parse("""<HintPath>..\..\packages\NLog.3.2.0.0\lib\net45\NLog.dll</HintPath>""")

        Assert.That <| packageNameMatchesReference packageName (referenceName, xelem)

    [<Test>]
    member t.``Extract simple package name``() =
        let packageName = @"..\..\packages\NLog.3.2.0.0\lib\net45\NLog.dll"
        let expected = "NLog"
        
        let actual = extractPackageName packageName

        Assert.That(actual, Is.EqualTo(expected))

    [<Test>]
    member t.``Find all nuget references``() =
        let references = nugetReferences project 
                      |> Seq.map fst
                      |> Seq.toList

        Assert.That(references, Has.Length.EqualTo 6)

        Assert.That(references, Contains.Item "Bearded.Monads")
        Assert.That(references, Contains.Item "Csn.Logging")
        Assert.That(references, Contains.Item "Csn.Logging.NLog3")
        Assert.That(references, Contains.Item "NLog")
        Assert.That(references, Contains.Item "NLog.Extended")
        Assert.That(references, Contains.Item "NUnit")