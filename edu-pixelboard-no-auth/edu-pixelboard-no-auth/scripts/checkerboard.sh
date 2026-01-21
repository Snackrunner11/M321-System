#!/bin/bash

for x in $(seq 0 16); do
    for y in $(seq 0 16); do
        KEY="color:(${x}|${y})"
        if [ $((($x + $y) % 2)) -eq 0 ]; then
            COLOR='{"Red":255,"Green":255,"Blue":255}'  # White
        else
            COLOR='{"Red":0,"Green":0,"Blue":0}'        # Black
        fi
        echo -e "SET \"$KEY\" '"$COLOR"'"
    done
done
