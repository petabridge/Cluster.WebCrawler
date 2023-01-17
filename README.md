# Cluster.WebCrawler
K8s, DevOps-ified version of the Akka.Cluster WebCrawler code sample.

## How To Run This Sample
First, we need to build the Docker images:

### Windows

```powershell
PS> ./build.cmd Docker
```

### Linux or OS X

```powershell
PS> ./build.sh Docker
```

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
