FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ConferenceRoomBooking.Api/ConferenceRoomBooking.Api.csproj ConferenceRoomBooking.Api/
COPY ConferenceRoomBooking.Application/ConferenceRoomBooking.Application.csproj ConferenceRoomBooking.Application/
COPY ConferenceRoomBooking.Domain/ConferenceRoomBooking.Domain.csproj ConferenceRoomBooking.Domain/
COPY ConferenceRoomBooking.Infrastructure/ConferenceRoomBooking.Infrastructure.csproj ConferenceRoomBooking.Infrastructure/
RUN dotnet restore ConferenceRoomBooking.Api/ConferenceRoomBooking.Api.csproj

COPY . .
RUN dotnet publish ConferenceRoomBooking.Api/ConferenceRoomBooking.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ConferenceRoomBooking.Api.dll"]
