# Build & test container for Linux

# build with:
# docker build -t secana/cakeapp .

# Push to Docker Hub:
# $env:DOCKER_ID_USER="secana"
# docker login
# docker push secana/cakeapp

FROM microsoft/dotnet:2.0.5-sdk-2.1.4
RUN apt update && apt upgrade -y
RUN apt install dos2unix unzip -y

