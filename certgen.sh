# certgen.sh <password>
# Generate a development certificate from a temporary docker container at ./cert/WebApp.pfx. 
mkdir -p ./cert
sudo docker run --name certgen -v ./cert:/app_cert mcr.microsoft.com/dotnet/sdk:10.0 sh -c "dotnet dev-certs https -ep /app_cert/WebApp.pfx -p $1 && chown 1000:1000 /app_cert/WebApp.pfx"
sudo docker rm -f certgen