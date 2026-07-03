FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT_PATH
WORKDIR /src
COPY . .
RUN dotnet publish "$PROJECT_PATH" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG APP_DLL
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV APP_DLL=$APP_DLL
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet $APP_DLL"]
