FROM mcr.microsoft.com/dotnet/sdk:5.0 AS ne

WORKDIR /app
COPY Strider.Shelter/Strider.Shelter.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish Strider.Shelter/Strider.Shelter.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app
COPY --from=ne /app/out .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
EXPOSE 8133
ENTRYPOINT ["dotnet", "Strider.Shelter.dll"]