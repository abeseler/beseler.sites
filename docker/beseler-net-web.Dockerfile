FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY src/Beseler.ServiceDefaults src/Beseler.ServiceDefaults/
COPY src/BeselerNet.Web src/BeselerNet.Web/

RUN dotnet publish "src/BeselerNet.Web/BeselerNet.Web.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "BeselerNet.Web.dll"]