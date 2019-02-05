FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "4053"

COPY ./bin/Release/netcoreapp2.0/publish/ /app

# 9110 - Petabridge.Cmd
# 4053 - Akka.Cluster
EXPOSE 9110 4053

CMD ["dotnet", "Lighthouse.dll"]