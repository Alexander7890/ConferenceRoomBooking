using ConferenceRoomBooking.Application.Services;

namespace ConferenceRoomBooking.Tests.Pricing;

public sealed class PricingCalculatorTests
{
    [Theory]
    [InlineData(6, 0, 7, 0, 1800)]
    [InlineData(9, 0, 10, 0, 2000)]
    [InlineData(12, 0, 13, 0, 2300)]
    [InlineData(18, 0, 19, 0, 1600)]
    [InlineData(11, 0, 15, 0, 8600)]
    [InlineData(8, 30, 9, 30, 1900)]
    [InlineData(17, 30, 18, 30, 1800)]
    [InlineData(10, 0, 10, 30, 1000)]
    public void Calculate_SplitsTimeAcrossPricePeriods(
        int startHour, int startMinute, int endHour, int endMinute, decimal expected)
    {
        var start = new DateTime(2026, 10, 1, startHour, startMinute, 0);
        var end = new DateTime(2026, 10, 1, endHour, endMinute, 0);

        var price = PricingCalculator.Calculate(2000m, start, end, []);

        Assert.Equal(expected, price.RentalPrice);
        Assert.Equal(0m, price.ServicesPrice);
        Assert.Equal(expected, price.TotalPrice);
    }

    [Fact]
    public void Calculate_ChargesOneServiceOnceForTheWholeBooking()
    {
        var price = PricingCalculator.Calculate(
            2000m, new DateTime(2026, 10, 1, 10, 0, 0), new DateTime(2026, 10, 1, 12, 0, 0), [500m]);

        Assert.Equal(4000m, price.RentalPrice);
        Assert.Equal(500m, price.ServicesPrice);
        Assert.Equal(4500m, price.TotalPrice);
    }

    [Fact]
    public void Calculate_ChargesMultipleServicesOnceForTheWholeBooking()
    {
        var price = PricingCalculator.Calculate(
            2000m, new DateTime(2026, 10, 1, 11, 0, 0), new DateTime(2026, 10, 1, 15, 0, 0), [500m, 300m, 700m]);

        Assert.Equal(8600m, price.RentalPrice);
        Assert.Equal(1500m, price.ServicesPrice);
        Assert.Equal(10100m, price.TotalPrice);
    }

    [Fact]
    public void Calculate_RoundsTheCombinedRentalPriceToCents()
    {
        var price = PricingCalculator.Calculate(
            1m, new DateTime(2026, 10, 1, 8, 59, 0), new DateTime(2026, 10, 1, 9, 1, 0), []);

        Assert.Equal(0.03m, price.RentalPrice);
    }
}
