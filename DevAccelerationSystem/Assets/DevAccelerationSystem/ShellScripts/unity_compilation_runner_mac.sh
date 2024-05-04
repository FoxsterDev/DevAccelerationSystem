#!/bin/bash

# Set default Unity version
DEFAULT_UNITY_VERSION="2022.3.13f1"
EXECUTE_METHOD="DevAccelerationSystem.ProjectCompilationCheck.BatchModeRunner.Run"

# Check minimum number of arguments
if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <project_path> [compile_config_name] [unity_version]"
    exit 1
fi

# User input arguments
PROJECT_PATH=$1
CONFIG_NAME=$2
UNITY_VERSION=${3:-$DEFAULT_UNITY_VERSION}

# Unity executable path (adjust according to your Unity versions and installation)
UNITY_PATH="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
DEFAULT_DIRECTORY_PATH="$PROJECT_PATH/Library/ProjectCompilationCheckOutput"

# Define the directory path $(pwd) is the current directory
if [ -z "$3" ]; then
   DIRECTORY_PATH="$DEFAULT_DIRECTORY_PATH"
else
   DIRECTORY_PATH="$DEFAULT_DIRECTORY_PATH/$UNITY_VERSION"
fi

# Log file location
DEFAULT_LOG_PATH="$DIRECTORY_PATH/UnityCompilation.log"
COMPILATION_OUTPUT_PATH="$DIRECTORY_PATH/CompilationOutput.json"

# Check if the directory exists
# if [ -d "$DEFAULT_DIRECTORY_PATH" ]; then
  #   echo "Directory exists. Removing..."
   #  rm -rf "$DEFAULT_DIRECTORY_PATH"
# fi

if [ -d "$DIRECTORY_PATH" ]; then
    echo "Directory exists. Removing..."
    rm -rf "$DIRECTORY_PATH"
fi

echo "Creating output directory..."
mkdir -p "$DIRECTORY_PATH"

# Check if the log file already exists and remove it if it does
if [ -f "$DEFAULT_LOG_PATH" ]; then
    echo "Removing existing log file."
    rm "$DEFAULT_LOG_PATH"
fi

if [ -f "$COMPILATION_OUTPUT_PATH" ]; then
    echo "Removing existing output file."
     rm "$COMPILATION_OUTPUT_PATH"
fi

echo "Starting Unity project compilation for a config: $CONFIG_NAME with Unity version: $UNITY_VERSION at $(date)" | tee $DEFAULT_LOG_PATH
 
# Run Unity in batch mode without graphics and perform the build
"$UNITY_PATH" -batchmode -nographics -quit -ignorecompileerrors -silent-crashes -force-free \
              -logFile $DEFAULT_LOG_PATH \
              -projectPath $PROJECT_PATH \
              -executeMethod $EXECUTE_METHOD \
              -configName $CONFIG_NAME \
              -compilationOutput $COMPILATION_OUTPUT_PATH
              
# Check if the compilation succeeded
if [ $? -eq 0 ]; then
    echo "Compilation successful. Output at: $COMPILATION_OUTPUT_PATH" | tee -a $DEFAULT_LOG_PATH
else
    echo "Compilation failed. Output at: $COMPILATION_OUTPUT_PATH . Check log at: $DEFAULT_LOG_PATH" | tee -a $DEFAULT_LOG_PATH
    exit 1
fi