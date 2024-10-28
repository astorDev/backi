## Background Service

```sh
dotnet run --timer=Threading
```

```sh
dotnet run --timer=System --logTickError=true
```

```sh
dotnet run --timer=Periodic --logTickError=true
```

```sh
dotnet run --timer=Safe
```

## Singletons

```sh
dotnet run
```

```sh
curl localhost:5106/system
```

## API-Managed timer

```sh
dotnet run
```

```sh
curl localhost:5106/start
```

```sh
curl localhost:5106/stop
```