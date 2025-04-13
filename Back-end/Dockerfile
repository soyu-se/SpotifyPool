# Use the ASP.NET Core image for the runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use Alpine SDK image for build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy files from context to container
COPY ["SpotifyPool/SpotifyPool.csproj", "SpotifyPool/"]
COPY ["BusinessLogicLayer.DependencyInjection/BusinessLogicLayer.DependencyInjection.csproj", "BusinessLogicLayer.DependencyInjection/"]
COPY ["BusinessLogicLayer.Implement/BusinessLogicLayer.Implement.csproj", "BusinessLogicLayer.Implement/"]
COPY ["SetupLayer.Enum/SetupLayer.Enum.csproj", "SetupLayer.Enum/"]
COPY ["BusinessLogicLayer.Interface/BusinessLogicLayer.Interface.csproj", "BusinessLogicLayer.Interface/"]
COPY ["BusinessLogicLayer.Mapper/BusinessLogicLayer.Mapper.csproj", "BusinessLogicLayer.Mapper/"]
COPY ["BusinessLogicLayer.ModelView/BusinessLogicLayer.ModelView.csproj", "BusinessLogicLayer.ModelView/"]
COPY ["DataAccessLayer.Repository/DataAccessLayer.Repository.csproj", "DataAccessLayer.Repository/"]
COPY ["SetupLayer.Setting/SetupLayer.Setting.csproj", "SetupLayer.Setting/"]
COPY ["Commons/Utility.csproj", "Commons/"]
COPY ["DataAccessLayer.Implement/DataAccessLayer.Implement.csproj", "DataAccessLayer.Implement/"]
COPY ["DataAccessLayer.Interface/DataAccessLayer.Interface.csproj", "DataAccessLayer.Interface/"]

# Restore dependencies
RUN dotnet restore "SpotifyPool/SpotifyPool.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "SpotifyPool/SpotifyPool.csproj" -c Release -o /app/build

# Publish phase
FROM build AS publish
RUN dotnet publish "SpotifyPool/SpotifyPool.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final phase to run the application
FROM base AS final
WORKDIR /app

# Install Libgdiplus to support System.Drawing.common and OnnxRuntime
RUN apt-get update && apt-get install -y libgdiplus libstdc++6 libc6 && rm -rf /var/lib/apt/lists/*

# Copy model ONNX into TrainingModel folder
COPY ["SpotifyPool/TrainingModel/audio_features_model.onnx", "/app"]

# Set environmental variables for the application to find onnxruntime
ENV LD_LIBRARY_PATH=/usr/local/lib/onnxruntime:${LD_LIBRARY_PATH:-}
ENV ASPNETCORE_URLS=http://+:8080

# Copy the published files to the container
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SpotifyPool.dll"]