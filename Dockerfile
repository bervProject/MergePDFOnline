FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish BervProject.MergePDFOnline.Console -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime
COPY --from=build /app/publish /app/publish
WORKDIR /app/publish
CMD ["dotnet", "BervProject.MergePDFOnline.Console.dll"]
