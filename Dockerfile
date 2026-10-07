FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
RUN dotnet publish --no-restore -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime

EXPOSE 8080 443
WORKDIR /app

RUN mkdir -p /home/ubuntu/.microsoft/usersecrets /home/ubuntu/.aspnet/DataProtection-Keys && \
    chown -R 1000:1000 /home/ubuntu

RUN mkdir /app/data && chown 1000:1000 /app/data

COPY --from=build --chown=1000:1000 /app/WebApp/out ./

ENTRYPOINT ["dotnet", "WebApp.dll"]
