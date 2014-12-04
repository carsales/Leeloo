namespace Leeloo

module CsProjHelper =
    open System.Linq
    open System.Xml.Linq

    type CsProjRepresentation = XDocument       
    let loadProj (fileName: string): CsProjRepresentation = XDocument.Load fileName
    
    let xn = XName.op_Implicit
    let ns = let ns = XNamespace.op_Implicit "http://schemas.microsoft.com/developer/msbuild/2003"
             fun s -> ns + s

    let frameworkVersion (projectDoc: CsProjRepresentation) =
        projectDoc.Descendants(ns "TargetFrameworkVersion").First().Value 
        |> FrameworkVersion.ParseTargetFramework
        |> Option.get

    let findReference (doc: CsProjRepresentation) packageName =
        let includesPackageName (name: string) (elem: XElement) =
            elem.Attribute(xn "Include").Value.ToUpperInvariant().Contains(name.ToUpperInvariant()) 

        let ref = doc.Descendants(ns "Reference").First(fun elem -> includesPackageName packageName elem)
        let hintPath = ref.Element(ns "HintPath")

        ref.Attribute(xn "Include").Value, hintPath
    
    let projectReferences (doc: CsProjRepresentation) =
        let projRefNode         = ns "ProjectReference"
        let nameNode            = ns "Name"
        let value (n: XElement) = n.Value

        doc.Descendants(projRefNode).Descendants(nameNode).Select(value)

