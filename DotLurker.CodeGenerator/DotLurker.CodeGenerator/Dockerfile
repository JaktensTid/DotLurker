FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["../DotLurker.CodeGenerator/DotLurker.CodeGenerator/DotLurker.CodeGenerator.csproj", "../DotLurker.CodeGenerator/DotLurker.CodeGenerator/"]
RUN dotnet restore "../DotLurker.CodeGenerator/DotLurker.CodeGenerator/DotLurker.CodeGenerator.csproj"
COPY . .
WORKDIR "/src/../DotLurker.CodeGenerator/DotLurker.CodeGenerator"
RUN dotnet build "DotLurker.CodeGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotLurker.CodeGenerator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotLurker.CodeGenerator.dll"]
