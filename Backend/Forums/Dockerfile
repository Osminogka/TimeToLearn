FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./*.sln ./
COPY ./Forums.API/*.csproj ./Forums.API/
COPY ./Forums.DAL/*.csproj ./Forums.DAL/
COPY ./Forums.DL/*.csproj ./Forums.DL/
RUN dotnet restore

COPY ./ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Forums.API.dll", "http://+:80"]