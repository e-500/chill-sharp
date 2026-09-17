FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . ./
RUN dotnet publish ChillSharp.Examples/BloggingApiService/ChillSharp.Examples.BloggingApiService.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:8080
ENV CHILLSHARP_DB_PATH=/data/blogging.db

VOLUME ["/data"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChillSharp.Examples.BloggingApiService.dll"]
