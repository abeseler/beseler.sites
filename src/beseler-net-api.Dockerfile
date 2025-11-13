FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

COPY Beseler.ServiceDefaults Beseler.ServiceDefaults/
COPY BeselerNet.Shared BeselerNet.Shared/
COPY BeselerNet.Api BeselerNet.Api/

RUN dotnet publish "BeselerNet.Api/BeselerNet.Api.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "BeselerNet.Api.dll"]