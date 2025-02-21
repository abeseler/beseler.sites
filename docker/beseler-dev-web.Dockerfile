FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY src/Beseler.ServiceDefaults src/Beseler.ServiceDefaults/
COPY src/BeselerDev.Web src/BeselerDev.Web/

RUN dotnet publish "src/BeselerDev.Web/BeselerDev.Web.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "BeselerDev.Web.dll"]