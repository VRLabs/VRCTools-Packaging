FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["VRLabs.VRCTools.Packaging.Console/VRLabs.VRCTools.Packaging.Console.csproj", "VRLabs.VRCTools.Packaging.Console/"]
RUN dotnet restore "VRLabs.VRCTools.Packaging.Console/VRLabs.VRCTools.Packaging.Console.csproj"
COPY . .
WORKDIR "/src/VRLabs.VRCTools.Packaging.Console"
RUN dotnet build "VRLabs.VRCTools.Packaging.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VRLabs.VRCTools.Packaging.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final
COPY --from=publish /app/publish /app
ENTRYPOINT ["dotnet", "/app/VRLabs.VRCTools.Packaging.Console.dll"]
