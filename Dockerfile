FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG version
WORKDIR /src

COPY ["FamilySync.Services.Authentication/FamilySync.Services.Authentication.csproj", "FamilySync.Services.Authentication/"]
COPY ["NuGet.config", "FamilySync.Services.Authentication/"]

RUN dotnet restore "FamilySync.Services.Authentication/FamilySync.Services.Authentication.csproj"

COPY . .

RUN dotnet publish "FamilySync.Services.Authentication/FamilySync.Services.Authentication.csproj" -c Release -o out /p:Version=$version

FROM mcr.microsoft.com/dotnet/aspnet:7.0 
WORKDIR /app

EXPOSE 80
EXPOSE 443

COPY --from=build /src/out .
ENTRYPOINT ["dotnet", "FamilySync.Services.Authentication.dll"]