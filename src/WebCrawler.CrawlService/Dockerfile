FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5213"

#Akka.Remote inbound listening endpoint
EXPOSE 5213 

COPY ./bin/Release/netcoreapp2.1/publish/ /app


CMD ["dotnet", "WebCrawler.CrawlService.dll"]
