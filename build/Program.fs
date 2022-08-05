open System
open System.Collections.Generic
open Fake
open Fake.Core
open Fake.IO
open System.Threading


let (</>) x y = System.IO.Path.Combine(x, y);


let run workingDir fileName args =
    printfn $"CWD: %s{workingDir}"
    let fileName, args =
        if Environment.isUnix
        then fileName, args else "cmd", ("/C " + fileName + " " + args)

    CreateProcess.fromRawCommandLine fileName args
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.withTimeout TimeSpan.MaxValue
    |> CreateProcess.ensureExitCodeWithMessage $"'%s{workingDir}> %s{fileName} %s{args}' task failed"
    |> Proc.run
    |> ignore

let dotnet = "dotnet"
let tool = "tool"
let restore = "restore"
let packRelease = "pack -c Release"

open System.IO
open System.Linq

/// Recursively tries to find the parent of a file starting from a directory
let rec findParent (directory: string) (fileToFind: string) = 
    let path = if Directory.Exists(directory) then directory else Directory.GetParent(directory).FullName
    let files = Directory.GetFiles(path)
    if files.Any(fun file -> Path.GetFileName(file).ToLower() = fileToFind.ToLower()) 
    then path 
    else findParent (DirectoryInfo(path).Parent.FullName) fileToFind
    
let cwd = findParent __SOURCE_DIRECTORY__ "Giraffe.SerilogExtensions.sln"

let src = cwd </> "src" </> "Giraffe.SerilogExtensions"
let tests = cwd </> "tests" </> "Giraffe.SerilogExtensions.Tests"

let clean projectPath =
    Shell.cleanDirs [
      projectPath </> "bin"
      projectPath </> "obj"
    ]

let publish projectPath = 
    clean projectPath
    run projectPath dotnet packRelease
    let nugetKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"
    let nupkg = System.IO.Directory.GetFiles(projectPath </> "bin" </> "Release") |> Seq.head
    let pushCmd = $"nuget push %s{nupkg} -s nuget.org -k %s{nugetKey}"
    run projectPath dotnet pushCmd

let build() = 
    clean src
    run src dotnet "build"

let test() = 
    clean tests
    run tests dotnet "run"

[<EntryPoint>]
let main(args: string[]) =
    run cwd dotnet "tool restore"
    match args with
    | [||] -> build()
    | [| "build" |] -> build()
    | [| "test" |] -> test()
    | [| "publish" |] -> publish src
    | otherwise -> printfn $"Unknown args %A{otherwise}"
    0