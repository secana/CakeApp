# Build & test container for Linux

# build with:
# docker build -t secana/cakeapp .

# Push to Docker Hub:
# $env:DOCKER_ID_USER="secana"
# docker login
# docker push secana/cakeapp

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine3.9
RUN dotnet tool install -g Cake.Tool --version 0.33.0
ENV PATH="$PATH:/root/.dotnet/tools"