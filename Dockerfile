FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BackendAsp.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create the uploads directory
RUN mkdir -p wwwroot/uploads && chmod 777 wwwroot/uploads

# Port configuration
ENV PORT=8080
EXPOSE ${PORT}
ENV ASPNETCORE_URLS=http://+:${PORT}

# Single entrypoint
ENTRYPOINT ["dotnet", "BackendAsp.dll"] 