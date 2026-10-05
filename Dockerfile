FROM mcr.microsoft.com/dotnet/sdk:latest AS build
WORKDIR /app

COPY App/*.slnx .
# Copy csproj and restore as distinct layers
COPY App/Application/*.csproj ./Application/
COPY App/Application.Contracts/*.csproj ./Application.Contracts/
COPY App/Domain/*.csproj ./Domain/
COPY App/DTO/*.csproj ./DTO/
COPY App/Infrastructure.Contracts/*.csproj ./Infrastructure.Contracts/
COPY App/Infrastructure.EF/*.csproj ./Infrastructure.EF/
# WEB APP
COPY App/WebApp/*.csproj ./WebApp/

RUN dotnet restore

# Copy everything else and build app
## MAIN
COPY App/Application/. ./Application/
COPY App/Application.Contracts/. ./Application.Contracts/
COPY App/Domain/. ./Domain/
COPY App/DTO/. ./DTO/
COPY App/Infrastructure.Contracts/. ./Infrastructure.Contracts/
COPY App/Infrastructure.EF/. ./Infrastructure.EF/
# WEB APP 
COPY App/WebApp/. ./WebApp/

WORKDIR /app/WebApp
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime
EXPOSE 8080
WORKDIR /app
COPY --from=build /app/WebApp/out ./
ENTRYPOINT ["dotnet", "WebApp.dll"]
