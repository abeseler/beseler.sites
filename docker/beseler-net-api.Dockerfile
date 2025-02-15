FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY src/Beseler.ServiceDefaults src/Beseler.ServiceDefaults/
COPY src/BeselerNet.Shared src/BeselerNet.Shared/
COPY src/BeselerNet.Api src/BeselerNet.Api/

RUN dotnet publish "src/BeselerNet.Api/BeselerNet.Api.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "BeselerNet.Api.dll"]