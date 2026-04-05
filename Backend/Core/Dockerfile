FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./*.sln ./
COPY ./Core.API/*.csproj ./Core.API/
COPY ./Core.DAL/*.csproj ./Core.DAL/
COPY ./Core.DL/*.csproj ./Core.DL/
COPY ./Core.Tests/*.csproj ./Core.Tests/
RUN dotnet restore

COPY ./ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Core.API.dll", "http://+:80"]