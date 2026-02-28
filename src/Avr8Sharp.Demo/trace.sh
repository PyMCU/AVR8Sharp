#!/bin/bash

set -x
set -e

rm -f *.nettrace
dotnet publish -c Release -r osx-x64 --self-contained true
dotnet-trace collect -- bin/Release/net8.0/osx-x64/publish/Avr8SharpPerf $@
NETTRACE=$(find . -name '*.nettrace' -type f)
dotnet-trace convert $NETTRACE --format Speedscope
rm $NETTRACE