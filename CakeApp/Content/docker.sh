#!/bin/bash
# Usage: docker.sh path/to/the/*.csproj work_dir container_name

PROJECT_FILE=$1
WORKING_DIR=$3
CONTAINER_NAME=$4

echo "PROJECT_FILE: $PROJECT_FILE"
echo "WORKING_DIR: $WORKING_DIR"
echo "CONTAINER_NAME: $CONTAINER_NAME"

function getValueFromProjectFile {  
  VALUE=`grep -m 1 Version ${1} | cut -d"<" -f2 | cut -d">" -f2
`

  if [ ! "$VALUE" ]; then
    echo "Error: Cannot find \"${2}\" in ${1}" >&2;
    exit 1;
  else
    echo $VALUE ;
  fi; 
}

# Read the version value from project.json, for example 1.0.0-*
VERSION=`getValueFromProjectFile $PROJECT_FILE` || exit 1;

# Split the -* part of the version number
VERSION=$(echo $VERSION | cut -d"-" -f1)

echo "Extracted the version number: $VERSION from the $PROJECT_FILE"
echo "Will create docker container: $CONTAINER_NAME:$VERSION"

# Build a container with the application.
docker build -t $CONTAINER_NAME:$VERSION .
docker tag $CONTAINER_NAME:$VERSION $CONTAINER_NAME:latest
#docker push $CONTAINER_NAME
