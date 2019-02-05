FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5212"

#Akka.Remote inbound listening endpoint
EXPOSE 5212 

COPY ./bin/Release/netcoreapp2.0/publish/ /app

CMD ["dotnet", "WebCrawler.TrackerService.dll"]
