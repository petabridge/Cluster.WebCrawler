#I @"tools/FAKE/tools"
#r "FakeLib.dll"

open System
open System.IO
open System.Text

open Fake
open Fake.DotNetCli
open Fake.DocFxHelper

// Information about the project for Nuget and Assembly info files
let product = "Cluster.WebCrawler"
let configuration = "Release"

// Metadata used when signing packages and DLLs
let signingName = "My Library"
let signingDescription = "My REALLY COOL Library"
let signingUrl = "https://signing.is.cool/"

// Read release notes and version
let solutionFile = FindFirstMatchingFile "*.sln" __SOURCE_DIRECTORY__  // dynamically look up the solution
let buildNumber = environVarOrDefault "BUILD_NUMBER" "0"
let hasTeamCity = (not (buildNumber = "0")) // check if we have the TeamCity environment variable for build # set
let preReleaseVersionSuffix = "beta" + (if (not (buildNumber = "0")) then (buildNumber) else DateTime.UtcNow.Ticks.ToString())
let versionSuffix = 
    match (getBuildParam "nugetprerelease") with
    | "dev" -> preReleaseVersionSuffix
    | _ -> ""

let releaseNotes =
    File.ReadLines "./RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

// Directories
let toolsDir = __SOURCE_DIRECTORY__ @@ "tools"
let output = __SOURCE_DIRECTORY__  @@ "bin"
let outputTests = __SOURCE_DIRECTORY__ @@ "TestResults"
let outputPerfTests = __SOURCE_DIRECTORY__ @@ "PerfResults"
let outputNuGet = output @@ "nuget"

// Copied from original NugetCreate target
let nugetDir = output @@ "nuget"
let workingDir = output @@ "build"
let nugetExe = FullName @"./tools/nuget.exe"

Target "Clean" (fun _ ->
    ActivateFinalTarget "KillCreatedProcesses"

    CleanDir output
    CleanDir outputTests
    CleanDir outputPerfTests
    CleanDir outputNuGet
    CleanDir "docs/_site"
)

(*

    Creates an environment variable that can be read by other processes afterwards for deployments and such

*)
Target "SetReleaseVersion" (fun _ ->
    setEnvironVar "RELEASE_NUMBER" releaseNotes.AssemblyVersion 
)

Target "AssemblyInfo" (fun _ ->
    XmlPokeInnerText "./src/common.props" "//Project/PropertyGroup/VersionPrefix" releaseNotes.AssemblyVersion    
    XmlPokeInnerText "./src/common.props" "//Project/PropertyGroup/PackageReleaseNotes" (releaseNotes.Notes |> String.concat "\n")
)

Target "RestorePackages" (fun _ ->
    let customSource = getBuildParamOrDefault "customNuGetSource" ""

    if(hasBuildParam "customNuGetSource") then
        DotNetCli.Restore
            (fun p -> 
                { p with
                    Project = solutionFile
                    NoCache = false
                    AdditionalArgs = [sprintf "-s %s -s https://api.nuget.org/v3/index.json" customSource]})
    else
        DotNetCli.Restore
            (fun p -> 
                { p with
                    Project = solutionFile
                    NoCache = false })
)

Target "Build" (fun _ ->          
    DotNetCli.Build
        (fun p -> 
            { p with
                Project = solutionFile
                Configuration = configuration }) // "Rebuild"  
)


//--------------------------------------------------------------------------------
// Tests targets 
//--------------------------------------------------------------------------------
module internal ResultHandling =
    let (|OK|Failure|) = function
        | 0 -> OK
        | x -> Failure x

    let buildErrorMessage = function
        | OK -> None
        | Failure errorCode ->
            Some (sprintf "xUnit2 reported an error (Error Code %d)" errorCode)

    let failBuildWithMessage = function
        | DontFailBuild -> traceError
        | _ -> (fun m -> raise(FailedTestsException m))

    let failBuildIfXUnitReportedError errorLevel =
        buildErrorMessage
        >> Option.iter (failBuildWithMessage errorLevel)

Target "RunTests" (fun _ ->
    let projects = 
        match (isWindows) with 
        | true -> !! "./src/**/*.Tests.csproj"
        | _ -> !! "./src/**/*.Tests.csproj" // if you need to filter specs for Linux vs. Windows, do it here

    let runSingleProject project =
        let arguments =
            match (hasTeamCity) with
            | true -> (sprintf "test -c Release --no-build --logger:\"console;verbosity=normal\" --results-directory %s -- -parallel none -teamcity" (outputTests))
            | false -> (sprintf "test -c Release --no-build --logger:\"console;verbosity=normal\" --results-directory %s -- -parallel none" (outputTests))

        let result = ExecProcess(fun info ->
            info.FileName <- "dotnet"
            info.WorkingDirectory <- (Directory.GetParent project).FullName
            info.Arguments <- arguments) (TimeSpan.FromMinutes 30.0) 
        
        ResultHandling.failBuildIfXUnitReportedError TestRunnerErrorLevel.DontFailBuild result  

    projects |> Seq.iter (log)
    projects |> Seq.iter (runSingleProject)
)

Target "NBench" <| fun _ ->
    let projects = 
        match (isWindows) with 
        | true -> !! "./src/**/*.Tests.Performance.csproj"
        | _ -> !! "./src/**/*.Tests.Performance.csproj" // if you need to filter specs for Linux vs. Windows, do it here


    let runSingleProject project =
        let arguments =
            match (hasTeamCity) with
            | true -> (sprintf "nbench --nobuild --teamcity --concurrent true --trace true --output %s" (outputPerfTests))
            | false -> (sprintf "nbench --nobuild --concurrent true --trace true --output %s" (outputPerfTests))

        let result = ExecProcess(fun info ->
            info.FileName <- "dotnet"
            info.WorkingDirectory <- (Directory.GetParent project).FullName
            info.Arguments <- arguments) (TimeSpan.FromMinutes 30.0) 
        
        ResultHandling.failBuildIfXUnitReportedError TestRunnerErrorLevel.DontFailBuild result
    
    projects |> Seq.iter runSingleProject


//--------------------------------------------------------------------------------
// Code signing targets
//--------------------------------------------------------------------------------
Target "SignPackages" (fun _ ->
    let canSign = hasBuildParam "SignClientSecret" && hasBuildParam "SignClientUser"
    if(canSign) then
        log "Signing information is available."
        
        let assemblies = !! (outputNuGet @@ "*.nupkg")

        let signPath =
            let globalTool = tryFindFileOnPath "SignClient.exe"
            match globalTool with
                | Some t -> t
                | None -> if isWindows then findToolInSubPath "SignClient.exe" "tools/signclient"
                          elif isMacOS then findToolInSubPath "SignClient" "tools/signclient"
                          else findToolInSubPath "SignClient" "tools/signclient"

        let signAssembly assembly =
            let args = StringBuilder()
                    |> append "sign"
                    |> append "--config"
                    |> append (__SOURCE_DIRECTORY__ @@ "appsettings.json") 
                    |> append "-i"
                    |> append assembly
                    |> append "-r"
                    |> append (getBuildParam "SignClientUser")
                    |> append "-s"
                    |> append (getBuildParam "SignClientSecret")
                    |> append "-n"
                    |> append signingName
                    |> append "-d"
                    |> append signingDescription
                    |> append "-u"
                    |> append signingUrl
                    |> toText

            let result = ExecProcess(fun info -> 
                info.FileName <- signPath
                info.WorkingDirectory <- __SOURCE_DIRECTORY__
                info.Arguments <- args) (System.TimeSpan.FromMinutes 5.0) (* Reasonably long-running task. *)
            if result <> 0 then failwithf "SignClient failed.%s" args

        assemblies |> Seq.iter (signAssembly)
    else
        log "SignClientSecret not available. Skipping signing"
)

//--------------------------------------------------------------------------------
// Nuget targets 
//--------------------------------------------------------------------------------

let overrideVersionSuffix (project:string) =
    match project with
    | _ -> versionSuffix // add additional matches to publish different versions for different projects in solution
Target "CreateNuget" (fun _ ->    
    let projects = !! "src/**/*.csproj" 
                   -- "src/**/*Tests.csproj" // Don't publish unit tests
                   -- "src/**/*Tests*.csproj"

    let runSingleProject project =
        DotNetCli.Pack
            (fun p -> 
                { p with
                    Project = project
                    Configuration = configuration
                    AdditionalArgs = ["--include-symbols --no-build"]
                    VersionSuffix = overrideVersionSuffix project
                    OutputPath = outputNuGet })

    projects |> Seq.iter (runSingleProject)
)

Target "PublishNuget" (fun _ ->
    let projects = !! "./bin/nuget/*.nupkg" -- "./bin/nuget/*.symbols.nupkg"
    let apiKey = getBuildParamOrDefault "nugetkey" ""
    let source = getBuildParamOrDefault "nugetpublishurl" ""
    let symbolSource = getBuildParamOrDefault "symbolspublishurl" ""
    let shouldPublishSymbolsPackages = not (symbolSource = "")

    if (not (source = "") && not (apiKey = "") && shouldPublishSymbolsPackages) then
        let runSingleProject project =
            DotNetCli.RunCommand
                (fun p -> 
                    { p with 
                        TimeOut = TimeSpan.FromMinutes 10. })
                (sprintf "nuget push %s --api-key %s --source %s --symbol-source %s" project apiKey source symbolSource)

        projects |> Seq.iter (runSingleProject)
    else if (not (source = "") && not (apiKey = "") && not shouldPublishSymbolsPackages) then
        let runSingleProject project =
            DotNetCli.RunCommand
                (fun p -> 
                    { p with 
                        TimeOut = TimeSpan.FromMinutes 10. })
                (sprintf "nuget push %s --api-key %s --source %s" project apiKey source)

        projects |> Seq.iter (runSingleProject)
)

//--------------------------------------------------------------------------------
// Documentation 
//--------------------------------------------------------------------------------  
Target "DocFx" (fun _ ->
    DotNetCli.Restore (fun p -> { p with Project = solutionFile })
    DotNetCli.Build (fun p -> { p with Project = solutionFile; Configuration = configuration })

    let docsPath = "./docs"

    DocFx (fun p -> 
                { p with 
                    Timeout = TimeSpan.FromMinutes 30.0; 
                    WorkingDirectory  = docsPath; 
                    DocFxJson = docsPath @@ "docfx.json" })
)

//--------------------------------------------------------------------------------
// Docker Targets
//-------------------------------------------------------------------------------- 
Target "PublishCode" (fun _ ->    
    let projects = !! "src/**/*.csproj" 
                   -- "src/WebCrawler.Shared/*.csproj"
                   -- "src/WebCrawler.Shared.*/*.csproj"
                   -- "src/**/*Tests.csproj" // Don't publish unit tests
                   -- "src/**/*Tests*.csproj"

    let runSingleProject project =
        DotNetCli.Publish
            (fun p -> 
                { p with
                    Project = project
                    Configuration = configuration
                    VersionSuffix = overrideVersionSuffix project
                    })

    projects |> Seq.iter (runSingleProject)
)

let mapDockerImageName (projectName:string) =
    match projectName with
    | "Lighthouse" -> Some("webcrawler.lighthouse")
    | "WebCrawler.CrawlService" -> Some("webcrawler.crawlservice")
    | "WebCrawler.TrackerService" -> Some("webcrawler.trackerservice")
    | "WebCrawler.Web" -> Some("webcrawler.web")
    | _ -> None

Target "BuildDockerImages" (fun _ ->
    let projects = !! "src/**/*.csproj" 
                   -- "src/**/*Tests.csproj" // Don't publish unit tests
                   -- "src/**/*Tests*.csproj"

    let remoteRegistryUrl = getBuildParamOrDefault "remoteRegistry" ""

    let buildDockerImage imageName projectPath =
        
        let args = 
            if(hasBuildParam "remoteRegistry") then
                StringBuilder()
                    |> append "build"
                    |> append "-t"
                    |> append (imageName + ":" + releaseNotes.AssemblyVersion) 
                    |> append "-t"
                    |> append (imageName + ":latest") 
                    |> append "-t"
                    |> append (remoteRegistryUrl + "/" + imageName + ":" + releaseNotes.AssemblyVersion) 
                    |> append "-t"
                    |> append (remoteRegistryUrl + "/" + imageName + ":latest") 
                    |> append "."
                    |> toText
            else
                StringBuilder()
                    |> append "build"
                    |> append "-t"
                    |> append (imageName + ":" + releaseNotes.AssemblyVersion) 
                    |> append "-t"
                    |> append (imageName + ":latest") 
                    |> append "."
                    |> toText

        ExecProcess(fun info -> 
                info.FileName <- "docker"
                info.WorkingDirectory <- Path.GetDirectoryName projectPath
                info.Arguments <- args) (System.TimeSpan.FromMinutes 5.0) (* Reasonably long-running task. *)

    let runSingleProject project =
        let projectName = Path.GetFileNameWithoutExtension project
        let imageName = mapDockerImageName projectName
        let result = match imageName with
                        | None -> 0
                        | Some(name) -> buildDockerImage name project
        if result <> 0 then failwithf "docker build failed. %s" project

    projects |> Seq.iter (runSingleProject)
)

//--------------------------------------------------------------------------------
// Cleanup
//--------------------------------------------------------------------------------

FinalTarget "KillCreatedProcesses" (fun _ ->
    log "Shutting down dotnet build-server"
    let result = ExecProcess(fun info -> 
            info.FileName <- "dotnet"
            info.WorkingDirectory <- __SOURCE_DIRECTORY__
            info.Arguments <- "build-server shutdown") (System.TimeSpan.FromMinutes 2.0)
    if result <> 0 then failwithf "dotnet build-server shutdown failed"
)

//--------------------------------------------------------------------------------
// Help 
//--------------------------------------------------------------------------------

Target "Help" <| fun _ ->
    List.iter printfn [
      "usage:"
      "./build.ps1 [target]"
      ""
      " Targets for building:"
      " * Build         Builds"
      " * Nuget         Create and optionally publish nugets packages"
      " * SignPackages  Signs all NuGet packages, provided that the following arguments are passed into the script: SignClientSecret={secret} and SignClientUser={username}"
      " * RunTests      Runs tests"
      " * All           Builds, run tests, creates and optionally publish nuget packages"
      " * DocFx         Creates a DocFx-based website for this solution"
      ""
      " Other Targets"
      " * Help       Display this help" 
      ""]

//--------------------------------------------------------------------------------
//  Target dependencies
//--------------------------------------------------------------------------------

Target "BuildRelease" DoNothing
Target "All" DoNothing
Target "Nuget" DoNothing
Target "Docker" DoNothing
Target "PrepareDeploy" DoNothing

// build dependencies
"Clean" ==> "RestorePackages" ==> "SetReleaseVersion" ==> "AssemblyInfo" ==> "Build" ==> "BuildRelease"

// tests dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "RunTests"

// nuget dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "CreateNuget"
"CreateNuget" ==> "SignPackages" ==> "PublishNuget" ==> "Nuget"

// docs
"Clean" ==> "RestorePackages" ==> "BuildRelease" ==> "Docfx"

// Docker
"BuildRelease" ==> "PublishCode" ==> "BuildDockerImages" ==> "Docker"

// all
"BuildRelease" ==> "All"
"RunTests" ==> "All"
"NBench" ==> "All"
"Nuget" ==> "All"

// Deploy
"Docker" ==> "PrepareDeploy"

RunTargetOrDefault "Help"