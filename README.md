## Donate Water Backend API
This repository contains code for mobile app backend and website backend. 
It uses abp.io https://abp.io/community boiler plate code to start with
Then the api specific to donate water app and websites have been added

# Deploy
This code is docker based and the docker file which builds the apis can be found here donate-water/donatewater-backend-api/blob/main/src/IIASA.FieldSurvey.HttpApi.Host/Dockerfile
We also have a dockercompose file here - docker-compose.yml
A simple docker compose command 

``
docker-compose build
``

builds the image required for Backend APIs

This image can be pushed to Container registry and deployed in kubernetes or docker hub

# Data persistence
It depends on postgres database for persisting data, a connection string needs to be provided in the /src/IIASA.FieldSurvey.HttpApi.Host/appsettings.json file
