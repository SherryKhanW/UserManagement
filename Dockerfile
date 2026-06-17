#running the .NET SDK package
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build 
#creating new directory for the source code
WORKDIR /src   
# Copy only the project file to leverage Docker layer caching
COPY ["UserManagement/UserManagement.csproj", "UserManagement/"]
#running the NuGet packages for .NET 
RUN dotnet restore "UserManagement/UserManagement.csproj" 
#copying everything else in the folder
COPY . .
#running application now that dockerfile is setup
RUN dotnet publish "UserManagement/UserManagement.csproj" -c Release -o /app/publish

# now only keeping the application and discarding all the extra packages etc as if we keep on running that then everytime this docker file is 
# ran now only image of the application is made rathe than installing all the SDKs etc again
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
#port number 
EXPOSE 8080 
#ENTRYPOINT is used to tell Docker from where to start this application
ENTRYPOINT ["dotnet", "UserManagement.dll"]
