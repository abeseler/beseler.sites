docker buildx build `
    --file ./src/beseler-dev-web.Dockerfile `
    --tag abeseler/beseler-dev-web `
    --provenance=false ./src

#docker push abeseler/beseler-net-dbdeploy
docker image rm abeseler/beseler-dev-web

#docker buildx build -f ./docker/beseler-net-api.Dockerfile -t abeseler/beseler-net-api --provenance=false .
#docker push abeseler/beseler-net-dbdeploy
#docker image rm abeseler/beseler-net-api

#dotnet publish ./src/Beseler.Deploy/Beseler.Deploy.csproj /t:PublishContainer
#docker push abeseler/beseler-deploy
#docker image rm abeseler/beseler-deploy

#dotnet publish ./src/BeselerDev.Web/BeselerDev.Web.csproj /t:PublishContainer
#docker push abeseler/beseler-dev-web
#docker image rm abeseler/beseler-dev-web

#dotnet publish ./src/BeselerNet.Api/BeselerNet.Api.csproj /t:PublishContainer
#docker push abeseler/beseler-net-api
#docker image rm abeseler/beseler-net-api

#dotnet publish ./src/BeselerNet.Web/BeselerNet.Web.csproj /t:PublishContainer
#docker push abeseler/beseler-net-web
#docker image rm abeseler/beseler-net-web
