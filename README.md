# Cluster.WebCrawler
K8s, DevOps-ified version of the Akka.Cluster WebCrawler code sample.

## Building and Publishing Docker Containers
This solution uses .NET's built-in container support. The following container images are available:

- `webcrawler/web`: The web frontend service (ASP.NET Core)
- `webcrawler/tracker`: The tracker service (console application)
- `webcrawler/crawler`: The crawler service (console application)

When built in CI, the images will include additional metadata and tags based on the GitHub environment.

### Building Containers Locally
To build the containers locally, run:

```bash
# Build all containers
dotnet publish WebCrawler.sln --configuration Release /t:PublishContainer

# Or build individual containers
dotnet publish src/WebCrawler.Web/WebCrawler.Web.csproj --configuration Release /t:PublishContainer
dotnet publish src/WebCrawler.TrackerService/WebCrawler.TrackerService.csproj --configuration Release /t:PublishContainer
dotnet publish src/WebCrawler.CrawlService/WebCrawler.CrawlService.csproj --configuration Release /t:PublishContainer
```

The containers will be tagged with both `latest` and the current version number. In CI environments, additional tags will include the GitHub run number and commit SHA.

### Running The Seed Node Demo

Use `docker-compose` to run the seed-node based discovery demo:

```powershell
PS> docker-compose -f ./docker/docker-compose.yml up
```

From there, you can use [Petabridge.Cmd](https://cmd.petabridge.com/) to connect to Lighthouse to view the status of the cluster:

```powershell
PS> pbm 127.0.0.1:9110 cluster show
```

### Running The Config Discovery Demo

Use `docker-compose` to run the seed-node based discovery demo:

```powershell
PS> docker-compose -f ./docker/docker-compose-config.yml up
```

From there, you can use [Petabridge.Cmd](https://cmd.petabridge.com/) to connect to Lighthouse to view the status of the cluster:

```powershell
PS> pbm 127.0.0.1:9110 cluster show
```

### Running The Kubernetes Discovery Demo

Use the provided command script to run the demo:

```powershell
PS> ./k8s/deployAll.cmd
```

From there, you can use [Petabridge.Cmd](https://cmd.petabridge.com/) to connect to Lighthouse to view the status of the cluster:

```powershell
PS> pbm 127.0.0.1:9110 cluster show
```

To stop the cluster, use the provided command script:

```powershell
PS> ./k8s/destroyAll.cmd
```

To monitor the status of the cluster, use the provided command script:

```powershell
PS> ./k8s/status.cmd
```

To see the Kubernets cluster events, use the provided command script:

```powershell
PS> ./k8s/events.cmd
```
