# Build & test container for Linux

# build with:
# docker build -t secana/cakeapp .

# Push to Docker Hub:
# $env:DOCKER_ID_USER="secana"
# docker login
# docker push secana/cakeapp

FROM microsoft/dotnet:2.1.401-sdk-alpine
RUN dotnet tool install -g Cake.Tool --version 0.30.0
ENV PATH="$PATH:/root/.dotnet/tools"