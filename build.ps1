docker build -f ./data/Dockerfile -t abeseler/beseler-net-dbdeploy .
docker push abeseler/beseler-net-dbdeploy
docker image rm abeseler/beseler-net-dbdeploy

dotnet publish ./src/Beseler.Deploy/Beseler.Deploy.csproj /t:PublishContainer
docker push abeseler/beseler-deploy
docker image rm abeseler/beseler-deploy

#dotnet publish ./src/BeselerDev.Web/BeselerDev.Web.csproj /t:PublishContainer
#docker push abeseler/beseler-dev-web
#docker image rm abeseler/beseler-dev-web

dotnet publish ./src/BeselerNet.Api/BeselerNet.Api.csproj /t:PublishContainer
docker push abeseler/beseler-net-api
docker image rm abeseler/beseler-net-api

dotnet publish ./src/BeselerNet.Web/BeselerNet.Web.csproj /t:PublishContainer
docker push abeseler/beseler-net-web
docker image rm abeseler/beseler-net-web
