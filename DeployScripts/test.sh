#!/bin/bash

assertFileExists() {
  if [ ! -f "$1" ]; then
      error "$1 does not exist."
      exit 1
  fi
}

#
# SCRIPT
#

assertFileExists "../Build/index.html"
rm -rf "./webgl"
mkdir -p "./webgl"
cp -a "../Build/." "./webgl"

docker-compose up
