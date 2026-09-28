using System.Text.Json.Nodes;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.DTOs.Reports;
using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.DTOs.Services;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ConferenceRoomBooking.Api.OpenApi;

public static class OpenApiConfiguration
{
    private const string LocalTimeDescription =
        "Local venue time without Z or a time zone offset; up to six fractional second digits.";

    private static readonly Dictionary<Type, string> Examples = new()
    {
        [typeof(CreateConferenceRoomRequest)] = """{"name":"Strategy room","capacity":50,"baseHourlyRate":2000,"serviceIds":[1,2]}""",
        [typeof(UpdateConferenceRoomRequest)] = """{"name":"Strategy room","capacity":60,"baseHourlyRate":2500,"serviceIds":[1,3]}""",
        [typeof(ConferenceRoomResponse)] = """{"id":4,"name":"Strategy room","capacity":50,"baseHourlyRate":2000,"isActive":true,"services":[{"id":1,"name":"Проєктор","price":500},{"id":2,"name":"Wi-Fi","price":300}]}""",
        [typeof(CreateBookingRequest)] = """{"roomId":1,"startDateTime":"2026-10-01T11:00:00","endDateTime":"2026-10-01T15:00:00","serviceIds":[1,2]}""",
        [typeof(BookingResponse)] = """{"bookingId":1,"room":{"id":1,"name":"Зал А"},"startDateTime":"2026-10-01T11:00:00","endDateTime":"2026-10-01T15:00:00","selectedServices":[{"id":1,"name":"Проєктор","price":500},{"id":2,"name":"Wi-Fi","price":300}],"rentalPrice":8600,"servicesPrice":800,"totalPrice":9400}""",
        [typeof(ServiceResponse)] = """{"id":1,"name":"Проєктор","price":500}""",
        [typeof(ConferenceRoomServiceResponse)] = """{"id":1,"name":"Проєктор","price":500}""",
        [typeof(RevenueReportResponse)] = """{"totalRevenue":9400,"bookingCount":1,"averageBookingValue":9400}""",
        [typeof(RoomReportResponse)] = """{"roomId":1,"roomName":"Зал А","bookingCount":1,"bookedHours":4,"revenue":9400}""",
        [typeof(ServiceReportResponse)] = """{"serviceId":1,"serviceName":"Проєктор","timesSelected":1,"revenue":500}"""
    };

    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Conference Room Booking API",
                Version = "v1",
                Description = "Manage rooms, find availability, create priced bookings and view business reports. " +
                    "All monetary amounts are in UAH. Booking prices are calculated by POST /api/bookings; " +
                    "there is no separate price-preview endpoint. Use IDs returned by the room and service endpoints."
            };
            return Task.CompletedTask;
        });
        options.AddSchemaTransformer((schema, context, _) =>
        {
            DescribeSchema(schema, context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, _, _) =>
        {
            DescribeParameters(operation);
            return Task.CompletedTask;
        });
    }

    private static void DescribeSchema(OpenApiSchema schema, Type type)
    {
        if (Examples.TryGetValue(type, out var example) && JsonNode.Parse(example) is { } value)
        {
            schema.Examples = [value];
        }

        if (type == typeof(CreateConferenceRoomRequest) || type == typeof(UpdateConferenceRoomRequest))
        {
            schema.Required = new HashSet<string> { "name", "capacity", "baseHourlyRate" };
        }
        else if (type == typeof(CreateBookingRequest))
        {
            schema.Required = new HashSet<string> { "roomId", "startDateTime", "endDateTime" };
        }

        if (type.Namespace?.StartsWith("ConferenceRoomBooking.Application.DTOs.", StringComparison.Ordinal) != true ||
            schema.Properties is null)
        {
            return;
        }

        foreach (var (name, propertySchema) in schema.Properties)
        {
            if (propertySchema is not OpenApiSchema property)
            {
                continue;
            }

            switch (name)
            {
                case "name":
                    property.Description = "Display name; required and not blank, with at most 200 characters.";
                    property.MaxLength = 200;
                    property.MinLength = 1;
                    break;
                case "capacity":
                    property.Description = "Number of guests; must be greater than zero.";
                    property.Minimum = "1";
                    break;
                case "baseHourlyRate":
                    property.Description = "Base price per hour in UAH; positive, with at most two decimal places.";
                    property.Minimum = "0.01";
                    property.Maximum = "9999999999999999.99";
                    property.MultipleOf = 0.01m;
                    break;
                case "serviceIds":
                    property.Description = "Unique positive IDs of active services. For bookings, services must also " +
                        "be available for the selected room. May be omitted or empty; null is invalid.";
                    property.UniqueItems = true;
                    break;
                case "startDateTime":
                case "endDateTime":
                    DescribeLocalTime(property);
                    property.Description = LocalTimeDescription + " Start must precede end, on the same day within 06:00–23:00.";
                    break;
                case "totalPrice":
                    property.Description = "Historical booking total in UAH, including rental and selected services.";
                    break;
                case "rentalPrice":
                    property.Description = "Rental in UAH, calculated across applicable time bands and rounded once to two decimals.";
                    break;
                case "servicesPrice":
                    property.Description = "Selected services in UAH, each charged once per booking.";
                    break;
                case "price":
                    property.Description = type == typeof(BookingServiceResponse)
                        ? "Service price in UAH saved when the booking was created."
                        : "Current service price in UAH, charged once per booking.";
                    break;
                case "bookedHours":
                    property.Description = "Total duration of included bookings in hours, including partial hours.";
                    break;
            }
        }
    }

    private static void DescribeParameters(OpenApiOperation operation)
    {
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            var (description, example) = parameter.Name?.ToLowerInvariant() switch
            {
                "id" => ("Conference room ID returned by GET /api/rooms.", (JsonNode?)JsonValue.Create(1)),
                "capacity" => ("Minimum number of guests; must be greater than zero.", JsonValue.Create(50)),
                "startdatetime" => (LocalTimeDescription + " Start of the requested period, at or after 06:00.", JsonValue.Create("2026-10-01T11:00:00")),
                "enddatetime" => (LocalTimeDescription + " After start, on the same day, at or before 23:00.", JsonValue.Create("2026-10-01T15:00:00")),
                "from" => (LocalTimeDescription + " Inclusive lower bound for booking start time.", JsonValue.Create("2026-10-01T00:00:00")),
                "to" => (LocalTimeDescription + " Exclusive upper bound for booking start time; must be later than From.", JsonValue.Create("2026-11-01T00:00:00")),
                _ => (null, null)
            };
            if (description is null)
            {
                continue;
            }

            parameter.Description = description;
            parameter.Example = example;
            parameter.Required = true;
            if (parameter.Schema is OpenApiSchema schema)
            {
                if (schema.Format == "date-time")
                {
                    DescribeLocalTime(schema);
                }
                else if (parameter.Name?.Equals("capacity", StringComparison.OrdinalIgnoreCase) == true)
                {
                    schema.Type = JsonSchemaType.Integer;
                    schema.Minimum = "1";
                }
                else if (parameter.Name?.Equals("id", StringComparison.OrdinalIgnoreCase) == true)
                {
                    schema.Type = JsonSchemaType.Integer;
                }
            }
        }
    }

    private static void DescribeLocalTime(OpenApiSchema schema)
    {
        schema.Format = "local-date-time";
        schema.Pattern = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,6})?)?$";
    }
}
