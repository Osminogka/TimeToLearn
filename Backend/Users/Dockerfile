FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./*.sln ./
COPY ./Users.API/*.csproj ./Users.API/
COPY ./Users.DAL/*.csproj ./Users.DAL/
COPY ./Users.DL/*.csproj ./Users.DL/
RUN dotnet restore

COPY ./ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Users.API.dll", "http://+:80"]