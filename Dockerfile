FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine as build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish BervProject.MergePDFOnline.Console -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine as runtime
COPY --from=build /app/publish /app/publish
WORKDIR /app/publish
CMD ["dotnet", "BervProject.MergePDFOnline.Console.dll"]
