#!/bin/bash

# Find the Redis container ID
REDIS_CONTAINER_ID=`docker ps | awk '/redis/{print $1}'`

# Check if no Redis container is found
if [ -z "$REDIS_CONTAINER_ID" ]; then
    echo "Error: No Redis container found."
    exit 1
fi

# Check if multiple Redis containers are found
if [ $(echo "$REDIS_CONTAINER_ID" | wc -w) -gt 1 ]; then
    echo "Error: Multiple Redis containers found. Please ensure only one Redis container is running."
    exit 1
fi

# stdin is forwarded to redis cli
docker exec -i ${REDIS_CONTAINER_ID} redis-cli

echo "Commands forwarded to Redis."