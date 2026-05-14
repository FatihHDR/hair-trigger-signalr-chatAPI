FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /App

COPY src/HairTrigger.Chat.Domain/HairTrigger.Chat.Domain.csproj src/HairTrigger.Chat.Domain/
COPY src/HairTrigger.Chat.Infrastructure/HairTrigger.Chat.Infrastructure.csproj src/HairTrigger.Chat.Infrastructure/
COPY src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj src/HairTrigger.Chat.Api/

RUN dotnet restore src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj

COPY . ./

RUN dotnet publish src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj -c Release -o out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "HairTrigger.Chat.Api.dll"]