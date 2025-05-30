# Cluster.WebCrawler
K8s, .NET Aspire-d, DevOps-ified version of the Akka.Cluster WebCrawler code sample.

## Running Locally In Container-less Aspire

The solution contains a local development Aspire support. To run the Aspire distributed application, load the solution into your IDE and launch the `WebCrawler.AppHost: http` or the `WebCrawler.AppHost: https` launch profile.

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

First, build the containers locally:

```bash
# Build all containers
dotnet publish WebCrawler.sln --configuration Release /t:PublishContainer
```

Then use `docker-compose` to run the seed-node based discovery demo:

```bash
# Start the cluster
docker compose -f ./docker/docker-compose.yml up -d

# View the logs
docker compose -f ./docker/docker-compose.yml logs -f
```

The web interface will be available at `http://localhost:8080`.

From there, you can use [Petabridge.Cmd](https://cmd.petabridge.com/) to connect to the cluster and view its status:

```bash
pbm 127.0.0.1:9110 cluster show
```

To stop the cluster:

```bash
docker compose -f ./docker/docker-compose.yml down
```

### Scaling the Solution

The WebCrawler solution supports dynamic scaling of both the crawler and tracker services. You can scale these services up or down while the cluster is running:

```bash
# Scale up the crawler service to 5 instances
docker compose -f ./docker/docker-compose.yml up -d --scale webcrawler.crawlservice=5

# Scale up the tracker service to 4 instances
docker compose -f ./docker/docker-compose.yml up -d --scale webcrawler.trackerservice=4

# Scale down both services
docker compose -f ./docker/docker-compose.yml up -d --scale webcrawler.crawlservice=2 --scale webcrawler.trackerservice=2
```

You can monitor the cluster's health and member status using [Petabridge.Cmd](https://cmd.petabridge.com/) after scaling:

```bash
pbm 127.0.0.1:9110 cluster show
```

To see the Kubernets cluster events, use the provided command script:

```powershell
PS> ./k8s/events.cmd
```
