#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["src/IIASA.FieldSurvey.HttpApi.Host/IIASA.FieldSurvey.HttpApi.Host.csproj", "src/IIASA.FieldSurvey.HttpApi.Host/"]
COPY ["src/IIASA.FieldSurvey.EntityFrameworkCore/IIASA.FieldSurvey.EntityFrameworkCore.csproj", "src/IIASA.FieldSurvey.EntityFrameworkCore/"]
COPY ["src/IIASA.FieldSurvey.Domain/IIASA.FieldSurvey.Domain.csproj", "src/IIASA.FieldSurvey.Domain/"]
COPY ["src/IIASA.FieldSurvey.Domain.Shared/IIASA.FieldSurvey.Domain.Shared.csproj", "src/IIASA.FieldSurvey.Domain.Shared/"]
COPY ["src/IIASA.FieldSurvey.HttpApi/IIASA.FieldSurvey.HttpApi.csproj", "src/IIASA.FieldSurvey.HttpApi/"]
COPY ["src/IIASA.FieldSurvey.Application.Contracts/IIASA.FieldSurvey.Application.Contracts.csproj", "src/IIASA.FieldSurvey.Application.Contracts/"]
COPY ["src/IIASA.FieldSurvey.Application/IIASA.FieldSurvey.Application.csproj", "src/IIASA.FieldSurvey.Application/"]
RUN dotnet restore "src/IIASA.FieldSurvey.HttpApi.Host/IIASA.FieldSurvey.HttpApi.Host.csproj"
COPY . .
WORKDIR "/src/src/IIASA.FieldSurvey.HttpApi.Host"
RUN dotnet build "IIASA.FieldSurvey.HttpApi.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IIASA.FieldSurvey.HttpApi.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IIASA.FieldSurvey.HttpApi.Host.dll"]