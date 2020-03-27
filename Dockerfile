# Build & test container for Linux

# build with:
# docker build -t secana/cakeapp .

# Push to Docker Hub:
# $env:DOCKER_ID_USER="secana"
# docker login
# docker push secana/cakeapp

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine
RUN dotnet tool install -g Cake.Tool --version 0.37.0
ENV PATH="$PATH:/root/.dotnet/tools"