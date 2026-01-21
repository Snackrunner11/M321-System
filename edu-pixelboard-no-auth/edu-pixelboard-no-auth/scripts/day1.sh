#!/bin/bash

# Theboard state required for day 1 and its exercises.

# basic checkboard as background
for x in $(seq 0 16); do
    for y in $(seq 0 16); do
        KEY="color:(${x}|${y})"
        if [ $((($x + $y) % 2)) -eq 0 ]; then
            COLOR='{"Red":200,"Green":200,"Blue":200}'  # White
        else
            COLOR='{"Red":100,"Green":100,"Blue":100}'  # Gray
        fi
        echo -e "SET \"$KEY\" '"$COLOR"'"
    done
done

# A coupld odd fields for the exercies

echo -e "SET \"color:(15|0)\" '{\"Red\":200,\"Green\":0,\"Blue\":0}'"
echo -e "SET \"color:(7|7)\" '{\"Red\":0,\"Green\":199,\"Blue\":0}'"
echo -e "SET \"color:(0|4)\" '{\"Red\":0,\"Green\":1,\"Blue\":200}'"