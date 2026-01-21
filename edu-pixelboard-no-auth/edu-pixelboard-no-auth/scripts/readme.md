Scripts to set the pixelboard to a specific view by writing the values directly to redis.

## Usage

First you start all services with docker-compose.
Then you call one of the scripts in this directory and forward the output to `write_to_redis.sh`.

Examples:

```bash
./reset_board.sh | ./write_to_redis.sh
```

```bash
./day1.sh | ./write_to_redis.sh
```

## Requirements

Aside from `/bin/bash` and awk, the command `docker` must be available in the current shell.

Redis must be running in a docker container.
