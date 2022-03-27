#!/bin/bash

docker-compose exec -T lightningd lightning-cli --rpc-file /root/.lightning/lightning-rpc --network regtest --lightning-dir /root/.lightning $@

