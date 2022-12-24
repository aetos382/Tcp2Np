# Tcp2Np
TCP ソケットと Windows の名前付きパイプをリレーするだけのアプリです。

## Usage
`Tcp2Np {endpoint} {pipename}`

## Example
たとえば、Docker コンテナーに `socat` と `openssh-client` を入れた上で、
```
# export SSH_AGENT_SOCK=/tmp/ssh-agent-sock
# socat unix-listen:$SSH_AGENT_SOCK,fork tcp-connect:host.docker.internal:50001 &
```

とやっておいて、Windows 上で

```
Tcp2Np 0.0.0.0:50001 openssh-ssh-agent
```

とやると、Docker コンテナから Windows 上の SSH Agent に認証要求を転送することができたりするかもしれません。知らんけど。

## Disclaimer
無保証です。何が起きても知りません。

## License
[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)
