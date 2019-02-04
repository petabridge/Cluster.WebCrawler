FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "4053"

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY *.sln ./
COPY ./get-dockerip.sh ./get-dockerip.sh
COPY src/Lighthouse/Lighthouse.csproj src/Lighthouse/

RUN dotnet restore
COPY . .
WORKDIR /src/src/Lighthouse
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app ./
COPY --from=build /src/get-dockerip.sh ./get-dockerip.sh
ENTRYPOINT ["/bin/bash","get-dockerip.sh"]

# 9110 - Petabridge.Cmd
# 4053 - Akka.Cluster
EXPOSE 9110 4053

CMD ["dotnet", "Lighthouse.dll"]