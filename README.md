#

## Create migrations

```shell
dotnet ef migrations add Initial -p OutboxEfCore
```

## Create database

```shell
 dotnet ef database update -p OutboxEfCore
```
