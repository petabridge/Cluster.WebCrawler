FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER__IP ""
ENV CLUSTER__PORT "16666"

#Akka.Remote inbound listening endpoint
EXPOSE 80
EXPOSE 16666 

COPY ./bin/Release/net6.0/publish/ /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS app
WORKDIR /app

COPY --from=base /app /app

# copy .NET Core global tool
COPY --from=base /root/.dotnet /root/.dotnet/

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

CMD ["dotnet", "WebCrawler.Web.dll"]
