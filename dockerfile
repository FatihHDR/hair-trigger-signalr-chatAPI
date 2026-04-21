FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /App

COPY . ./

RUN dotnet restore src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj

RUN dotnet publish src/HairTrigger.Chat.Api/HairTrigger.Chat.Api.csproj -c Release -o out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "HairTrigger.Chat.Api.dll"]