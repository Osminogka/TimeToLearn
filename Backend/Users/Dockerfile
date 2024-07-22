FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY Backend/*.sln ./
COPY Backend/Users.API/*.csproj ./Users/
COPY Backend/Users.DAL/*.csproj ./Users.DAL/
COPY Backend/Users.DL/*.csproj ./Users.DL/
COPY Backend/Users.Tests/*.csproj ./Users.Tests/
RUN dotnet restore

COPY Backend/ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "webapi.dll", "http://+:80"]