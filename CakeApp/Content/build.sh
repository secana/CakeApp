#!/bin/bash
##########################################################################
# Bash bootstrapper for Cake with .Net Core.
##########################################################################

# Define directories.
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
TOOLS_DIR=$SCRIPT_DIR/tools
CAKE_VERSION=0.21.1
CAKE_FOLDER=$TOOLS_DIR/Cake.CoreCLR/$CAKE_VERSION/
CAKE_DLL=$CAKE_FOLDER/Cake.dll
ADDIN_PATH=$TOOLS_DIR/Addins

DOCKER_ADDIN_URI="https://www.nuget.org/api/v2/package/Cake.Docker/0.7.7"
DOCKER_ADDIN_PATH=$ADDIN_PATH/Core.Docker/lib/netstandard1.6/Cake.Docker.dll
DOCKER_NUPGK=$ADDIN_PATH/Cake.Docker.nupkg

# Define default arguments.
TARGET="Default"
CONFIGURATION="Release"
VERBOSITY="verbose"
DRYRUN=
SCRIPT_ARGUMENTS=()

# Parse arguments.
for i in "$@"; do
    case $1 in
        -t|--target) TARGET="$2"; shift ;;
        -c|--configuration) CONFIGURATION="$2"; shift ;;
        -v|--verbosity) VERBOSITY="$2"; shift ;;
        -d|--dryrun) DRYRUN="-dryrun" ;;
        --) shift; SCRIPT_ARGUMENTS+=("$@"); break ;;
        *) SCRIPT_ARGUMENTS+=("$1") ;;
    esac
    shift
done

# Make sure the tools folder exist.
if [ ! -d "$TOOLS_DIR" ]; then
    mkdir "$TOOLS_DIR"
fi

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

if ! [ -x "$(command -v dotnet)" ]; then
    (>&2 echo "Cannot find dotnet executable on this system!")
    exit 1
fi
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet --info

###########################################################################
# INSTALL CAKE
###########################################################################

if [ ! -f "$CAKE_DLL" ]; then
    ZIP_NAME=Cake.CoreCLR.$CAKE_VERSION.zip

    # Download the Cake.CoreCLR nuget package and save it as a *.zip file.
    if [ ! -f "$ZIP_NAME" ]; then
        wget -O $ZIP_NAME https://www.nuget.org/api/v2/package/Cake.CoreCLR/$CAKE_VERSION
    fi

    # Unpack the zip file into the tools folder.
    mkdir -p $CAKE_FOLDER
    unzip $ZIP_NAME -d $CAKE_FOLDER

    # Remove the zip
    rm -f $ZIP_NAME
fi

# Make sure that Cake has been installed.
# if [ ! -f "$CAKE_DLL" ]; then
#     echo "Could not find Cake.exe at '$CAKE_DLL'."
#     exit 1
# fi

###########################################################################
# ADD DOCKER PLUGIN
###########################################################################

if [ ! -f "$DOCKER_NUPGK" ]; then
    echo "Downloading Cake.Docker"
    wget -O $DOCKER_NUPGK $DOCKER_ADDIN_URI
    unzip $DOCKER_NUPGK -d $ADDIN_PATH/Cake.Docker
fi


###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Start Cake
dotnet "$CAKE_DLL" build.cake --verbosity=$VERBOSITY --configuration=$CONFIGURATION --target=$TARGET $DRYRUN "${SCRIPT_ARGUMENTS[@]}"