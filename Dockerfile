# Build & test container for Linux

# build with:
# docker build -t secana/cakeapp .

# Push to Docker Hub:
# $env:DOCKER_ID_USER="secana"
# docker login
# docker push secana/cakeapp

FROM microsoft/dotnet:2.1.401-sdk-alpine3.7
RUN apk update && apk upgrade
RUN apk add dos2unix unzip bash

