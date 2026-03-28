FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["BE_API.csproj", "./"]
RUN dotnet restore "BE_API.csproj"

COPY . .
RUN dotnet publish "BE_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5280
EXPOSE 5280

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BE_API.dll"]
