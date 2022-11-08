#!/bin/bash

xmllint --xpath "/Project/ItemGroup/*/@Include" ./Manito.csproj | sed -e "s|Include=||g" | sed -e "s|\"||g" | xargs -n 1 -P 4 dotnet add ../bot-core package