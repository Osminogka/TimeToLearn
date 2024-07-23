FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./*.sln ./
COPY ./Authentication.API/*.csproj ./Authentication.API/
COPY ./Authentication.DAL/*.csproj ./Authentication.DAL/
COPY ./Authentication.DL/*.csproj ./Authentication.DL/
COPY ./Authentication.Tests/*.csproj ./Authentication.Tests/
RUN dotnet restore

COPY ./ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "Authentication.API.dll"]