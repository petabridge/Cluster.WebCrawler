FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5212"

#Akka.Remote inbound listening endpoint
EXPOSE 5212 

COPY ./bin/Release/netcoreapp3.1/publish/ /app

# Install Petabridge.Cmd client
RUN dotnet tool install --global pbm 

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS app
WORKDIR /app

COPY --from=base /app /app

# copy .NET Core global tool
COPY --from=base /root/.dotnet /root/.dotnet/

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

CMD ["dotnet", "WebCrawler.TrackerService.dll"]
