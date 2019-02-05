FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "16666"

#Akka.Remote inbound listening endpoint
EXPOSE 80
EXPOSE 16666 

COPY ./bin/Release/netcoreapp2.0/publish/ /app

CMD ["dotnet", "WebCrawler.Web.dll"]
