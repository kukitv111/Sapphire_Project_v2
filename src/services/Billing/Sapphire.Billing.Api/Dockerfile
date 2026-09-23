FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY src ./src
RUN dotnet publish src/services/Billing/Sapphire.Billing.Api/Sapphire.Billing.Api.csproj -c Release -o /app --no-self-contained
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sapphire.Billing.Api.dll"]
