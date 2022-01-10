﻿FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["./DramaMeter/DramaMeter.csproj", "DramaMeter/"]
RUN dotnet restore "DramaMeter/DramaMeter.csproj"
COPY . .
WORKDIR "/src/DramaMeter"
RUN dotnet build "DramaMeter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DramaMeter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DramaMeter.dll"]