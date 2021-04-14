# Cluster.WebCrawler
K8s, DevOps-ified version of the Akka.Cluster WebCrawler code sample.

## How to Run this Sample
First, we need to build the Docker images:

### Windows

```
PS> ./build.cmd Docker
```

### Linux or OS X

```
PS> ./build.sh Docker
```

And then use `docker-compose` to spin them up:

```
PS> docker-compose up -d
```

From there, you can use [Petabridge.Cmd](https://cmd.petabridge.com/) to connect to Lighthouse to view the status of the cluster:

```
PS> pbm 127.0.0.1:9110 cluster show
```
