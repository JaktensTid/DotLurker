FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["../DotLurker.Web/DotLurker.Web/DotLurker.Web.csproj", "../DotLurker.Web/DotLurker.Web/"]
RUN dotnet restore "../DotLurker.Web/DotLurker.Web/DotLurker.Web.csproj"
COPY . .
WORKDIR "/src/../DotLurker.Web/DotLurker.Web"
RUN dotnet build "DotLurker.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotLurker.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotLurker.Web.dll"]
