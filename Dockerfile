FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/Trellochocero.Api.csproj", "backend/"]
RUN dotnet restore "backend/Trellochocero.Api.csproj"
COPY . .
WORKDIR "/src/backend"
RUN dotnet build "Trellochocero.Api.csproj" -c Release -o /app/build
RUN dotnet publish "Trellochocero.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Trellochocero.Api.dll"]
