FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./*.sln ./
COPY ./Courses.API/*.csproj ./Courses.API/
COPY ./Courses.DAL/*.csproj ./Courses.DAL/
COPY ./Courses.DL/*.csproj ./Courses.DL/
COPY ./Courses.Tests/*.csproj ./Courses.Tests/
RUN dotnet restore

COPY ./ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Courses.API.dll", "http://+:80"]
