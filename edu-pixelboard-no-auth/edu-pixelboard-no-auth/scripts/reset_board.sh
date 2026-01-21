#!/bin/bash

COLOR='{"Red":200,"Green":200,"Blue":200}'

for x in $(seq 0 16); do
    for y in $(seq 0 16); do
        KEY="color:(${x}|${y})"
        echo -e "SET \"$KEY\" '"$COLOR"'"
    done
done
