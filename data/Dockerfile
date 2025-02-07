FROM abeseler/dbdeploy:latest
COPY ./data /app/Migrations
ENV Deploy__Command=update
ENV Deploy__StartingFile=migrations.json
ENV Deploy__DatabaseProvider=postgres