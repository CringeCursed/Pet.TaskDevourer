# Pet.TaskDevourer

Task manager (WPF .NET 8) + local ASP.NET Core Web API (SQLite + EF Core)

## 🟢 Run 

### Simple script (dev. build)
After a normal build (`dotnet build -c Debug`), double‑click `start.cmd` in repo root. It will:
* Check if something already listens on port 5005
* Start API exe
* Start WPF app

## 📦 Publish 

```
dotnet publish Server/Pet.TaskDevourer.Api/Pet.TaskDevourer.Api.csproj -c Release -o publish/api
dotnet publish Pet.TaskDevourer.csproj -c Release -o publish/app
```

Run manually:
```
publish/api/Pet.TaskDevourer.Api.exe
publish/app/Pet.TaskDevourer.exe
```

## 🛠 Diagnostics
* `startup.log` appended with initialization + CRUD events
* If both API + local load fail → error MessageBox
